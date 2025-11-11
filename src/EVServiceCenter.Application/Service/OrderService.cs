using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPartRepository _partRepository;
    private readonly ICartService _cartService;
    private readonly ICenterRepository _centerRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public OrderService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IPartRepository partRepository,
        ICartService cartService,
        ICenterRepository centerRepository,
        IInventoryRepository inventoryRepository)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _partRepository = partRepository;
        _cartService = cartService;
        _centerRepository = centerRepository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task<List<OrderResponse>> GetByCustomerIdAsync(int customerId)
    {
        var orders = await _orderRepository.GetByCustomerIdAsync(customerId);
        var result = new List<OrderResponse>();
        foreach (var o in orders)
        {
            result.Add(await MapToResponseWithCustomerAsync(o));
        }
        return result;
    }

    public async Task<OrderResponse?> GetByIdAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        return order != null ? await MapToResponseWithCustomerAsync(order) : null;
    }

    public async Task<List<OrderResponse>> GetAllAsync()
    {
        var orders = await _orderRepository.GetAllAsync();
        var result = new List<OrderResponse>();
        foreach (var o in orders)
        {
            result.Add(await MapToResponseWithCustomerAsync(o));
        }
        return result;
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        // Kiểm tra khách hàng có tồn tại không
        var customer = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
        if (customer == null)
            throw new ArgumentException("Khách hàng không tồn tại");

        // Không còn giỏ hàng: buộc phải gửi items trong request
        if (request.Items == null || !request.Items.Any())
            throw new ArgumentException("Danh sách sản phẩm không được rỗng");

        // Validate parts và tạo OrderItems từ request
        var orderItems = new List<OrderItem>();
        foreach (var item in request.Items)
        {
            if (item.PartId <= 0 || item.Quantity <= 0)
                throw new ArgumentException("Sản phẩm hoặc số lượng không hợp lệ");

            var part = await _partRepository.GetPartByIdAsync(item.PartId);
            if (part == null)
                throw new ArgumentException($"Sản phẩm {item.PartId} không tồn tại");
            if (!part.IsActive)
                throw new ArgumentException($"Sản phẩm {part.PartName} đã ngưng hoạt động");

            orderItems.Add(new OrderItem
            {
                PartId = part.PartId,
                Quantity = item.Quantity,
                UnitPrice = part.Price
            });
        }

        // Validate và lưu FulfillmentCenterId nếu được cung cấp từ FE
        int? fulfillmentCenterId = null;
        if (request.FulfillmentCenterId.HasValue)
        {
            // Validate center tồn tại, active và có đủ stock
            await ValidateCenterStockAsync(request.FulfillmentCenterId.Value, orderItems);
            fulfillmentCenterId = request.FulfillmentCenterId.Value;
        }

        var order = new Order
        {
            CustomerId = request.CustomerId,
            Status = "PENDING",
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            FulfillmentCenterId = fulfillmentCenterId, // Lưu center được chọn từ FE
            OrderItems = orderItems
        };

        var createdOrder = await _orderRepository.AddAsync(order);

        // Gợi ý center fulfill gần nhất dựa vào toạ độ + items (chỉ khi chưa có FulfillmentCenterId)
        var suggestion = fulfillmentCenterId == null
            ? await SuggestCenterAsync(request.Latitude, request.Longitude, orderItems)
            : null;

        var resp = MapToResponse(createdOrder);
        if (suggestion != null)
        {
            resp.SuggestedFulfillmentCenterId = suggestion.Value.centerId;
            resp.SuggestedFulfillmentDistanceKm = suggestion.Value.distanceKm;
        }
        return resp;
    }

    public async Task<OrderResponse> CreateQuickOrderAsync(QuickOrderRequest request)
    {
        // Validate customer exists
        var customer = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
        if (customer == null)
            throw new ArgumentException("Khách hàng không tồn tại");

        if (request.Items == null || !request.Items.Any())
            throw new ArgumentException("Danh sách sản phẩm không được rỗng");

        // Validate parts, active, and compute total
        var orderItems = new List<OrderItem>();
        decimal total = 0m;
        foreach (var item in request.Items)
        {
            if (item.PartId <= 0 || item.Quantity <= 0)
                throw new ArgumentException("Sản phẩm hoặc số lượng không hợp lệ");

            var part = await _partRepository.GetPartByIdAsync(item.PartId);
            if (part == null)
                throw new ArgumentException($"Sản phẩm {item.PartId} không tồn tại");
            if (!part.IsActive)
                throw new ArgumentException($"Sản phẩm {part.PartName} đã ngưng hoạt động");

            var unitPrice = part.Price;
            var lineTotal = unitPrice * item.Quantity;
            total += lineTotal;

            orderItems.Add(new OrderItem
            {
                PartId = part.PartId,
                Quantity = item.Quantity,
                UnitPrice = unitPrice
            });
        }

        // Validate và lưu FulfillmentCenterId nếu được cung cấp từ FE
        int? fulfillmentCenterId = null;
        if (request.FulfillmentCenterId.HasValue)
        {
            // Validate center tồn tại, active và có đủ stock
            await ValidateCenterStockAsync(request.FulfillmentCenterId.Value, orderItems);
            fulfillmentCenterId = request.FulfillmentCenterId.Value;
        }

        var order = new Order
        {
            CustomerId = request.CustomerId,
            Status = "PENDING",
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            FulfillmentCenterId = fulfillmentCenterId, // Lưu center được chọn từ FE
            OrderItems = orderItems
        };

        var created = await _orderRepository.AddAsync(order);

        // Gợi ý center fulfill gần nhất dựa vào toạ độ + items (chỉ khi chưa có FulfillmentCenterId)
        var suggestion = fulfillmentCenterId == null
            ? await SuggestCenterAsync(request.Latitude, request.Longitude, orderItems)
            : null;

        var resp = MapToResponse(created);
        if (suggestion != null)
        {
            resp.SuggestedFulfillmentCenterId = suggestion.Value.centerId;
            resp.SuggestedFulfillmentDistanceKm = suggestion.Value.distanceKm;
        }
        return resp;
    }

    public async Task<List<OrderItemSimpleResponse>> GetItemsAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null) throw new ArgumentException("Đơn hàng không tồn tại.");
        return order.OrderItems
            .Select(oi => new OrderItemSimpleResponse
            {
                OrderItemId = oi.OrderItemId,
                PartId = oi.PartId,
                PartName = oi.Part?.PartName ?? string.Empty,
                PartNumber = oi.Part?.PartNumber,
                Brand = oi.Part?.Brand,
                ImageUrl = oi.Part?.ImageUrl,
                Rating = oi.Part?.Rating,
                UnitPrice = oi.UnitPrice,
                Quantity = oi.Quantity,
                ConsumedQty = oi.ConsumedQty, // Thêm ConsumedQty
                Subtotal = oi.Quantity * oi.UnitPrice
            }).ToList();
    }

    // Lịch sử trạng thái đã bỏ theo yêu cầu

    public async Task<OrderResponse> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            throw new ArgumentException("Đơn hàng không tồn tại");

        var oldStatus = order.Status;
        order.Status = request.Status;
        order.UpdatedAt = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order);

        // Lịch sử trạng thái đã bỏ theo yêu cầu

        return MapToResponse(updatedOrder);
    }

    public async Task<OrderResponse> UpdateFulfillmentCenterAsync(int orderId, UpdateFulfillmentCenterRequest request)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            throw new ArgumentException("Đơn hàng không tồn tại");

        // Validate order status - chỉ cho phép cập nhật khi order chưa thanh toán
        if (order.Status == "PAID" || order.Status == "COMPLETED")
            throw new InvalidOperationException("Không thể cập nhật chi nhánh cho đơn hàng đã thanh toán");

        // Validate fulfillmentCenterId nếu có
        if (request.FulfillmentCenterId.HasValue)
        {
            await ValidateCenterStockAsync(request.FulfillmentCenterId.Value, order.OrderItems);
        }

        order.FulfillmentCenterId = request.FulfillmentCenterId;
        order.UpdatedAt = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order);

        return MapToResponse(updatedOrder);
    }

    public async Task DeleteOrderAsync(int orderId)
    {
        if (!await _orderRepository.ExistsAsync(orderId))
            throw new ArgumentException("Đơn hàng không tồn tại");

        await _orderRepository.DeleteAsync(orderId);
    }

    public async Task<bool> ExistsAsync(int orderId)
    {
        return await _orderRepository.ExistsAsync(orderId);
    }

    private OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            OrderId = order.OrderId,
            OrderNumber = $"ORD-#{order.OrderId}",
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.User?.FullName ?? "Khách hàng",
            CustomerPhone = order.Customer?.User?.PhoneNumber ?? "",
            TotalAmount = order.OrderItems?.Sum(oi => oi.Quantity * oi.UnitPrice) ?? 0m,
            Status = order.Status,
            Notes = order.Notes,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            FulfillmentCenterId = order.FulfillmentCenterId,
            FulfillmentCenterName = order.FulfillmentCenter?.CenterName,
            OrderItems = (order.OrderItems ?? new List<OrderItem>()).Select(oi => new OrderItemResponse
            {
                OrderItemId = oi.OrderItemId,
                PartId = oi.PartId,
                PartName = oi.Part?.PartName ?? "",
                PartNumber = oi.Part?.PartNumber ?? "",
                Brand = oi.Part?.Brand ?? "",
                ImageUrl = oi.Part?.ImageUrl,
                Rating = oi.Part?.Rating,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                LineTotal = oi.Quantity * oi.UnitPrice
            }).ToList()
        };
    }

    private async Task<OrderResponse> MapToResponseWithCustomerAsync(Order order)
    {
        var resp = MapToResponse(order);
        var missingName = string.IsNullOrWhiteSpace(resp.CustomerName) || resp.CustomerName == "Khách hàng";
        var missingPhone = string.IsNullOrWhiteSpace(resp.CustomerPhone);
        if (missingName || missingPhone)
        {
            var customer = await _customerRepository.GetCustomerByIdAsync(order.CustomerId);
            if (customer != null)
            {
                if (missingName)
                    resp.CustomerName = customer.User?.FullName ?? resp.CustomerName;
                if (missingPhone)
                    resp.CustomerPhone = customer.User?.PhoneNumber ?? resp.CustomerPhone;
            }
        }

        // Bổ sung tên chi nhánh nếu đã có FulfillmentCenterId nhưng thiếu tên
        if (resp.FulfillmentCenterId.HasValue && string.IsNullOrWhiteSpace(resp.FulfillmentCenterName))
        {
            var center = await _centerRepository.GetCenterByIdAsync(resp.FulfillmentCenterId.Value);
            if (center != null)
            {
                resp.FulfillmentCenterName = center.CenterName;
            }
        }
        return resp;
    }

    public async Task<OrderResponse> GetOrCreateCartAsync(int customerId)
    {
        var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
        if (customer == null) throw new ArgumentException("Khách hàng không tồn tại");

        var existing = await _orderRepository.GetCartByCustomerIdAsync(customerId);
        if (existing != null) return MapToResponse(existing);

        var cart = new Order
        {
            CustomerId = customerId,
            Status = "CART",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Notes = null,
            OrderItems = new List<OrderItem>()
        };
        var created = await _orderRepository.AddAsync(cart);
        return MapToResponse(created);
    }

    public async Task<List<OrderItemSimpleResponse>> GetCartItemsAsync(int cartOrderId)
    {
        var order = await _orderRepository.GetByIdAsync(cartOrderId);
        if (order == null || order.Status != "CART")
            throw new ArgumentException("Cart không tồn tại");
        return order.OrderItems.Select(oi => new OrderItemSimpleResponse
        {
            OrderItemId = oi.OrderItemId,
            PartId = oi.PartId,
            PartName = oi.Part?.PartName ?? string.Empty,
            UnitPrice = oi.UnitPrice,
            Quantity = oi.Quantity,
            Subtotal = oi.Quantity * oi.UnitPrice
        }).ToList();
    }

    public async Task<OrderResponse> AddItemToCartAsync(int cartOrderId, int partId, int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Số lượng phải lớn hơn 0");
        var order = await _orderRepository.GetByIdAsync(cartOrderId);
        if (order == null || order.Status != "CART") throw new ArgumentException("Cart không tồn tại");

        var part = await _partRepository.GetPartByIdAsync(partId);
        if (part == null) throw new ArgumentException("Phụ tùng không tồn tại");
        if (!part.IsActive) throw new ArgumentException($"Sản phẩm {part.PartName} đã ngưng hoạt động");

        var existingItem = order.OrderItems.FirstOrDefault(i => i.PartId == partId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            order.OrderItems.Add(new OrderItem
            {
                PartId = part.PartId,
                Quantity = quantity,
                UnitPrice = part.Price
            });
        }
        order.UpdatedAt = DateTime.UtcNow;
        var updated = await _orderRepository.UpdateAsync(order);
        return MapToResponse(updated);
    }

    public async Task<OrderResponse> UpdateCartItemQuantityAsync(int cartOrderId, int orderItemId, int quantity)
    {
        if (quantity < 0) throw new ArgumentException("Số lượng không hợp lệ");
        var order = await _orderRepository.GetByIdAsync(cartOrderId);
        if (order == null || order.Status != "CART") throw new ArgumentException("Cart không tồn tại");
        var item = order.OrderItems.FirstOrDefault(i => i.OrderItemId == orderItemId);
        if (item == null) throw new ArgumentException("Item không tồn tại trong cart");

        if (quantity == 0)
        {
            order.OrderItems.Remove(item);
        }
        else
        {
            item.Quantity = quantity;
        }
        order.UpdatedAt = DateTime.UtcNow;
        var updated = await _orderRepository.UpdateAsync(order);
        return MapToResponse(updated);
    }

    public async Task<OrderResponse> RemoveCartItemAsync(int cartOrderId, int orderItemId)
    {
        var order = await _orderRepository.GetByIdAsync(cartOrderId);
        if (order == null || order.Status != "CART") throw new ArgumentException("Cart không tồn tại");
        var item = order.OrderItems.FirstOrDefault(i => i.OrderItemId == orderItemId);
        if (item == null) throw new ArgumentException("Item không tồn tại trong cart");
        order.OrderItems.Remove(item);
        order.UpdatedAt = DateTime.UtcNow;
        var updated = await _orderRepository.UpdateAsync(order);
        return MapToResponse(updated);
    }

    public async Task<OrderResponse> ClearCartAsync(int cartOrderId)
    {
        var order = await _orderRepository.GetByIdAsync(cartOrderId);
        if (order == null || order.Status != "CART") throw new ArgumentException("Cart không tồn tại");
        order.OrderItems.Clear();
        order.UpdatedAt = DateTime.UtcNow;
        var updated = await _orderRepository.UpdateAsync(order);
        return MapToResponse(updated);
    }

    public async Task<OrderResponse> CheckoutCartAsync(int cartOrderId)
    {
        var order = await _orderRepository.GetByIdAsync(cartOrderId);
        if (order == null || order.Status != "CART") throw new ArgumentException("Cart không tồn tại");
        if (!order.OrderItems.Any()) throw new ArgumentException("Cart đang trống");
        order.Status = "PENDING";
        order.UpdatedAt = DateTime.UtcNow;
        var updated = await _orderRepository.UpdateAsync(order);
        return MapToResponse(updated);
    }

    public async Task<OrderResponse> CheckoutCartFromRedisAsync(int customerId)
    {
        var cart = await _cartService.GetCartAsync(customerId);
        if (cart == null || cart.Items.Count == 0)
            throw new ArgumentException("Cart không tồn tại hoặc đang trống");

        var order = new Order
        {
            CustomerId = customerId,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Notes = null,
            FulfillmentCenterId = cart.FulfillmentCenterId, // Lấy fulfillmentCenterId từ Cart
            OrderItems = cart.Items.Select(item => new OrderItem
            {
                PartId = item.PartId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };

        var created = await _orderRepository.AddAsync(order);
        await _cartService.ClearCartAsync(customerId);

        return MapToResponse(created);
    }

    private async Task<(int centerId, double distanceKm)?> SuggestCenterAsync(double? lat, double? lng, IEnumerable<OrderItem> items)
    {
        if (lat == null || lng == null) return null;
        var centers = await _centerRepository.GetActiveCentersAsync();
        var centersWithGeo = centers
            .Where(c => c.Latitude.HasValue && c.Longitude.HasValue)
            .Select(c => new { c.CenterId, Lat = (double)c.Latitude!.Value, Lng = (double)c.Longitude!.Value })
            .ToList();
        if (centersWithGeo.Count == 0) return null;

        (int centerId, double distanceKm)? best = null;
        foreach (var c in centersWithGeo)
        {
            var inv = await _inventoryRepository.GetInventoryByCenterIdAsync(c.CenterId);
            var parts = inv?.InventoryParts ?? new List<InventoryPart>();
            bool ok = items.All(oi => parts.Any(p => p.PartId == oi.PartId && p.CurrentStock >= oi.Quantity));
            if (!ok) continue;

            var d = HaversineKm(lat.Value, lng.Value, c.Lat, c.Lng);
            if (best == null || d < best.Value.distanceKm) best = (c.CenterId, d);
        }
        return best;
    }

    /// <summary>
    /// Validate center tồn tại, active và có đủ stock cho tất cả order items
    /// Throw ArgumentException nếu validation fail
    /// </summary>
    private async Task ValidateCenterStockAsync(int centerId, IEnumerable<OrderItem> orderItems)
    {
        // Validate center tồn tại và active
        var center = await _centerRepository.GetCenterByIdAsync(centerId);
        if (center == null)
            throw new ArgumentException($"Chi nhánh ID {centerId} không tồn tại");

        if (!center.IsActive)
            throw new ArgumentException($"Chi nhánh ID {centerId} đã ngưng hoạt động");

        // Validate inventory tồn tại
        var inventory = await _inventoryRepository.GetInventoryByCenterIdAsync(centerId);
        if (inventory == null)
            throw new ArgumentException($"Không tìm thấy kho của chi nhánh ID {centerId}");

        var inventoryParts = inventory.InventoryParts ?? new List<InventoryPart>();

        // Validate từng item có đủ stock
        // Sử dụng AvailableQty (CurrentStock - ReservedQty) thay vì CurrentStock
        foreach (var orderItem in orderItems)
        {
            var inventoryPart = inventoryParts.FirstOrDefault(ip => ip.PartId == orderItem.PartId);

            if (inventoryPart == null)
                throw new ArgumentException($"Phụ tùng ID {orderItem.PartId} không có trong kho của chi nhánh ID {centerId}");

            var availableQty = inventoryPart.CurrentStock - inventoryPart.ReservedQty;
            if (availableQty < orderItem.Quantity)
            {
                var partName = orderItem.Part?.PartName ?? $"Part {orderItem.PartId}";
                throw new ArgumentException(
                    $"Không đủ hàng cho phụ tùng '{partName}' tại chi nhánh ID {centerId}. " +
                    $"Hiện có: {availableQty} (CurrentStock: {inventoryPart.CurrentStock}, ReservedQty: {inventoryPart.ReservedQty}), cần: {orderItem.Quantity}");
            }
        }
    }

    private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371d;
        double dLat = (lat2 - lat1) * Math.PI / 180d;
        double dLng = (lng2 - lng1) * Math.PI / 180d;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180d) * Math.Cos(lat2 * Math.PI / 180d) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        return R * c;
    }
}
