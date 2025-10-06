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

    public OrderService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IPartRepository partRepository)
    {
        _orderRepository = orderRepository;
    
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

        var order = new Order
        {
            CustomerId = request.CustomerId,
            Status = "PENDING",
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OrderItems = orderItems
        };

        var createdOrder = await _orderRepository.AddAsync(order);

        // Lịch sử trạng thái đã bỏ theo yêu cầu

        // Không cần xóa giỏ hàng nữa

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

        var order = new Order
        {
            CustomerId = request.CustomerId,
            Status = "PENDING",
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OrderItems = orderItems
        };

        var created = await _orderRepository.AddAsync(order);

        // Lịch sử trạng thái đã bỏ theo yêu cầu

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
            OrderItems = order.OrderItems.Select(oi => new OrderItemResponse
            {
                OrderItemId = oi.OrderItemId,
                PartId = oi.PartId,
                PartName = oi.Part?.PartName ?? "",
                PartNumber = oi.Part?.PartNumber ?? "",
                Brand = oi.Part?.Brand ?? "",
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                LineTotal = oi.Quantity * oi.UnitPrice
            }).ToList()
        };
    }
}
