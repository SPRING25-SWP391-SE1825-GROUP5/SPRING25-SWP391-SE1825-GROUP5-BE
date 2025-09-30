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
    private readonly IOrderStatusHistoryRepository _orderStatusHistoryRepository;
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPartRepository _partRepository;

    public OrderService(
        IOrderRepository orderRepository,
        IOrderStatusHistoryRepository orderStatusHistoryRepository,
        IShoppingCartRepository shoppingCartRepository,
        ICustomerRepository customerRepository,
        IPartRepository partRepository)
    {
        _orderRepository = orderRepository;
        _orderStatusHistoryRepository = orderStatusHistoryRepository;
        _shoppingCartRepository = shoppingCartRepository;
        _customerRepository = customerRepository;
        _partRepository = partRepository;
    }

    public async Task<List<OrderResponse>> GetByCustomerIdAsync(int customerId)
    {
        var orders = await _orderRepository.GetByCustomerIdAsync(customerId);
        return orders.Select(MapToResponse).ToList();
    }

    public async Task<OrderResponse?> GetByIdAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        return order != null ? MapToResponse(order) : null;
    }

    public async Task<List<OrderResponse>> GetAllAsync()
    {
        var orders = await _orderRepository.GetAllAsync();
        return orders.Select(MapToResponse).ToList();
    }

    public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        // Kiểm tra khách hàng có tồn tại không
        var customer = await _customerRepository.GetCustomerByIdAsync(request.CustomerId);
        if (customer == null)
            throw new ArgumentException("Khách hàng không tồn tại");

        // Lấy giỏ hàng của khách hàng
        var cartItems = await _shoppingCartRepository.GetByCustomerIdAsync(request.CustomerId);
        if (!cartItems.Any())
            throw new ArgumentException("Giỏ hàng trống");

        // Tạo đơn hàng
        var orderNumber = await _orderRepository.GenerateOrderNumberAsync();
        var totalAmount = cartItems.Sum(item => item.Quantity * item.UnitPrice);

        var order = new Order
        {
            CustomerId = request.CustomerId,
            OrderNumber = orderNumber,
            TotalAmount = totalAmount,
            Status = "PENDING",
            Notes = request.Notes,
            ShippingAddress = !string.IsNullOrWhiteSpace(request.ShippingAddress)
                ? request.ShippingAddress
                : (customer?.User?.Address ?? customer?.Email ?? "UNKNOWN"),
            ShippingPhone = !string.IsNullOrWhiteSpace(request.ShippingPhone)
                ? request.ShippingPhone
                : (customer?.User?.PhoneNumber ?? customer?.NormalizedPhone ?? "UNKNOWN"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OrderItems = cartItems.Select(cartItem => new OrderItem
            {
                PartId = cartItem.PartId,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice,
                LineTotal = cartItem.Quantity * cartItem.UnitPrice
            }).ToList()
        };

        var createdOrder = await _orderRepository.AddAsync(order);

        // Tạo lịch sử trạng thái
        var statusHistory = new OrderStatusHistory
        {
            OrderId = createdOrder.OrderId,
            Status = "PENDING",
            Notes = "Đơn hàng được tạo",
            SystemGenerated = true,
            CreatedAt = DateTime.UtcNow
        };

        await _orderStatusHistoryRepository.AddAsync(statusHistory);

        // Xóa giỏ hàng
        await _shoppingCartRepository.DeleteByCustomerIdAsync(request.CustomerId);

        return MapToResponse(createdOrder);
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

            var unitPrice = part.UnitPrice;
            var lineTotal = unitPrice * item.Quantity;
            total += lineTotal;

            orderItems.Add(new OrderItem
            {
                PartId = part.PartId,
                Quantity = item.Quantity,
                UnitPrice = unitPrice,
                LineTotal = lineTotal
            });
        }

        var orderNumber = await _orderRepository.GenerateOrderNumberAsync();
        var order = new Order
        {
            CustomerId = request.CustomerId,
            OrderNumber = orderNumber,
            TotalAmount = total,
            Status = "PENDING",
            Notes = request.Notes,
            ShippingAddress = !string.IsNullOrWhiteSpace(request.ShippingAddress)
                ? request.ShippingAddress
                : (customer?.User?.Address ?? customer?.Email ?? "UNKNOWN"),
            ShippingPhone = !string.IsNullOrWhiteSpace(request.ShippingPhone)
                ? request.ShippingPhone
                : (customer?.User?.PhoneNumber ?? customer?.NormalizedPhone ?? "UNKNOWN"),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OrderItems = orderItems
        };

        var created = await _orderRepository.AddAsync(order);

        var statusHistory = new OrderStatusHistory
        {
            OrderId = created.OrderId,
            Status = "PENDING",
            Notes = "Đơn hàng mua ngay được tạo",
            SystemGenerated = true,
            CreatedAt = DateTime.UtcNow
        };
        await _orderStatusHistoryRepository.AddAsync(statusHistory);

        return MapToResponse(created);
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
                UnitPrice = oi.UnitPrice,
                Quantity = oi.Quantity,
                Subtotal = oi.LineTotal
            }).ToList();
    }

    public async Task<List<OrderStatusHistoryResponse>> GetStatusHistoryAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null) throw new ArgumentException("Đơn hàng không tồn tại.");
        var list = order.OrderStatusHistories ?? new List<OrderStatusHistory>();
        return list.Select(h => new OrderStatusHistoryResponse
        {
            HistoryId = h.HistoryId,
            Status = h.Status,
            Notes = h.Notes,
            CreatedBy = h.CreatedByUser?.FullName,
            SystemGenerated = h.SystemGenerated,
            CreatedAt = h.CreatedAt
        }).ToList();
    }

    public async Task<OrderResponse> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusRequest request)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            throw new ArgumentException("Đơn hàng không tồn tại");

        var oldStatus = order.Status;
        order.Status = request.Status;
        order.UpdatedAt = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateAsync(order);

        // Tạo lịch sử trạng thái
        var statusHistory = new OrderStatusHistory
        {
            OrderId = orderId,
            Status = request.Status,
            Notes = request.Notes ?? $"Trạng thái thay đổi từ {oldStatus} thành {request.Status}",
            CreatedBy = request.CreatedBy,
            SystemGenerated = false,
            CreatedAt = DateTime.UtcNow
        };

        await _orderStatusHistoryRepository.AddAsync(statusHistory);

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
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.User?.FullName ?? "Khách hàng",
            CustomerPhone = order.Customer?.User?.PhoneNumber ?? "",
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            Notes = order.Notes,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            OrderItems = order.OrderItems.Select(oi => new OrderItemResponse
            {
                OrderItemId = oi.OrderItemId,
                PartId = oi.PartId,
                PartName = oi.Part?.PartName ?? "",
                PartNumber = oi.Part?.PartNumber ?? "",
                Brand = oi.Part?.Brand ?? "",
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                LineTotal = oi.LineTotal
            }).ToList(),
            StatusHistory = order.OrderStatusHistories.Select(osh => new OrderStatusHistoryResponse
            {
                HistoryId = osh.HistoryId,
                Status = osh.Status,
                Notes = osh.Notes,
                CreatedBy = osh.CreatedByUser?.FullName ?? "Hệ thống",
                SystemGenerated = osh.SystemGenerated,
                CreatedAt = osh.CreatedAt
            }).ToList()
        };
    }
}
