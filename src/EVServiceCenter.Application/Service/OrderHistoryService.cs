using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service
{
    public class OrderHistoryService : IOrderHistoryService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICustomerRepository _customerRepository;

        public OrderHistoryService(
            IOrderRepository orderRepository,
            ICustomerRepository customerRepository)
        {
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
        }

        public async Task<OrderHistoryListResponse> GetOrderHistoryAsync(int customerId, int page = 1, int pageSize = 10, 
            string? status = null, DateTime? fromDate = null, DateTime? toDate = null, 
            string sortBy = "orderDate", string sortOrder = "desc")
        {
            // Validate customer exists
            var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with ID {customerId} not found.");
            }

            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Min(Math.Max(1, pageSize), 50);

            // Get orders with pagination
            var orders = await _orderRepository.GetOrdersByCustomerIdAsync(
                customerId, page, pageSize, status, fromDate, toDate, sortBy, sortOrder);

            // Get total count for pagination
            var totalItems = await _orderRepository.CountOrdersByCustomerIdAsync(
                customerId, status, fromDate, toDate);

            // Map to summary DTOs
            var orderSummaries = orders.Select(MapToOrderHistorySummary).ToList();

            // Calculate pagination info
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var pagination = new OrderPaginationInfo
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };

            var filters = new OrderFilterInfo
            {
                Status = status,
                FromDate = fromDate,
                ToDate = toDate,
                SortBy = sortBy,
                SortOrder = sortOrder
            };

            return new OrderHistoryListResponse
            {
                Orders = orderSummaries,
                Pagination = pagination,
                Filters = filters
            };
        }

        public async Task<OrderHistoryResponse> GetOrderHistoryByIdAsync(int customerId, int orderId)
        {
            // Validate customer exists
            var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with ID {customerId} not found.");
            }

            // Get order with full details
            var order = await _orderRepository.GetOrderWithDetailsByIdAsync(orderId);
            if (order == null)
            {
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");
            }

            // Verify order belongs to customer
            if (order.CustomerId != customerId)
            {
                throw new UnauthorizedAccessException("You can only view your own order history.");
            }

            return await MapToOrderHistoryResponse(order);
        }

        public async Task<OrderHistoryStatsResponse> GetOrderHistoryStatsAsync(int customerId, string period = "all")
        {
            // Validate customer exists
            var customer = await _customerRepository.GetCustomerByIdAsync(customerId);
            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with ID {customerId} not found.");
            }

            // Calculate date range based on period
            DateTime? fromDate = null;
            switch (period.ToLower())
            {
                case "7days":
                    fromDate = DateTime.Now.AddDays(-7);
                    break;
                case "30days":
                    fromDate = DateTime.Now.AddDays(-30);
                    break;
                case "90days":
                    fromDate = DateTime.Now.AddDays(-90);
                    break;
                case "1year":
                    fromDate = DateTime.Now.AddYears(-1);
                    break;
                case "all":
                default:
                    fromDate = null;
                    break;
            }

            // Get all orders for the period
            var allOrders = await _orderRepository.GetOrdersByCustomerIdAsync(
                customerId, 1, int.MaxValue, null, fromDate, null, "orderDate", "desc");

            // Calculate statistics
            var totalOrders = allOrders.Count;
            var statusBreakdown = CalculateStatusBreakdown(allOrders);
            var totalSpent = allOrders.Where(o => o.Status == "DELIVERED").Sum(o => o.OrderItems?.Sum(oi => oi.Quantity * oi.UnitPrice) ?? 0m);
            var averageOrderValue = totalOrders > 0 ? totalSpent / totalOrders : 0;

            var favoriteProduct = CalculateFavoriteProduct(allOrders);
            var recentActivity = CalculateRecentActivity(allOrders);

            return new OrderHistoryStatsResponse
            {
                TotalOrders = totalOrders,
                StatusBreakdown = statusBreakdown,
                TotalSpent = totalSpent,
                AverageOrderValue = averageOrderValue,
                FavoriteProduct = favoriteProduct,
                RecentActivity = recentActivity,
                Period = period
            };
        }

        private OrderHistorySummary MapToOrderHistorySummary(Order order)
        {
            return new OrderHistorySummary
            {
                OrderId = order.OrderId,
                OrderNumber = $"ORD-#{order.OrderId}",
                OrderDate = order.CreatedAt,
                Status = order.Status,
                TotalAmount = order.OrderItems?.Sum(oi => oi.Quantity * oi.UnitPrice) ?? 0m,
                ItemCount = order.OrderItems?.Count ?? 0,
                CreatedAt = order.CreatedAt
            };
        }

        private async Task<OrderHistoryResponse> MapToOrderHistoryResponse(Order order)
        {
            var response = new OrderHistoryResponse
            {
                OrderId = order.OrderId,
                OrderNumber = $"ORD-#{order.OrderId}",
                OrderDate = order.CreatedAt,
                Status = order.Status,
                TotalAmount = order.OrderItems?.Sum(oi => oi.Quantity * oi.UnitPrice) ?? 0m,
                Notes = order.Notes,
                ShippingAddress = new ShippingAddressInfo
                {
                    FullName = "Customer", // This would need to be extracted from shipping address
                    PhoneNumber = order.Customer?.User?.PhoneNumber,
                    Address = order.Customer?.User?.Address
                },
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt
            };

            // Add order items
            if (order.OrderItems != null)
            {
                response.Items = order.OrderItems.Select(oi => new OrderItemInfo
                {
                    OrderItemId = oi.OrderItemId,
                    PartId = oi.PartId,
                    ProductName = oi.Part?.PartName ?? "Unknown Product",
                    PartNumber = oi.Part?.PartNumber,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.Quantity * oi.UnitPrice
                }).ToList();
            }

            // Get payment info from invoice
            var payment = order.Invoices?.FirstOrDefault()?.Payments?.FirstOrDefault();
            if (payment != null)
            {
                response.PaymentInfo = new OrderPaymentInfo
                {
                    PaymentId = payment.PaymentId,
                    PaymentMethod = payment.PaymentMethod ?? "Unknown",
                    PaymentStatus = payment.Status ?? "Unknown",
                    PaidAt = payment.PaidAt,
                    Amount = payment.Amount
                };
            }

            // Generate timeline from status history
            response.Timeline = GenerateStatusTimeline(order);

            return response;
        }

        private List<OrderStatusTimelineInfo> GenerateStatusTimeline(Order order)
        {
            var timeline = new List<OrderStatusTimelineInfo>();

            // Add order creation
            timeline.Add(new OrderStatusTimelineInfo
            {
                Status = "PENDING",
                Timestamp = order.CreatedAt,
                Note = "Đặt hàng thành công"
            });

            // Add status changes based on order status
            if (order.Status == "CONFIRMED" || order.Status == "SHIPPED" || order.Status == "DELIVERED")
            {
                timeline.Add(new OrderStatusTimelineInfo
                {
                    Status = "CONFIRMED",
                    Timestamp = order.UpdatedAt,
                    Note = "Xác nhận đơn hàng"
                });
            }

            if (order.Status == "SHIPPED" || order.Status == "DELIVERED")
            {
                timeline.Add(new OrderStatusTimelineInfo
                {
                    Status = "SHIPPED",
                    Timestamp = order.UpdatedAt,
                    Note = "Đã giao cho đơn vị vận chuyển"
                });
            }

            if (order.Status == "DELIVERED")
            {
                timeline.Add(new OrderStatusTimelineInfo
                {
                    Status = "DELIVERED",
                    Timestamp = order.UpdatedAt,
                    Note = "Giao hàng thành công"
                });
            }

            if (order.Status == "CANCELLED")
            {
                timeline.Add(new OrderStatusTimelineInfo
                {
                    Status = "CANCELLED",
                    Timestamp = order.UpdatedAt,
                    Note = "Hủy đơn hàng"
                });
            }

            if (order.Status == "RETURNED")
            {
                timeline.Add(new OrderStatusTimelineInfo
                {
                    Status = "RETURNED",
                    Timestamp = order.UpdatedAt,
                    Note = "Trả hàng"
                });
            }

            return timeline.OrderBy(t => t.Timestamp).ToList();
        }

        private OrderStatusBreakdown CalculateStatusBreakdown(List<Order> orders)
        {
            return new OrderStatusBreakdown
            {
                Delivered = orders.Count(o => o.Status == "DELIVERED"),
                Cancelled = orders.Count(o => o.Status == "CANCELLED"),
                Pending = orders.Count(o => o.Status == "PENDING"),
                Shipped = orders.Count(o => o.Status == "SHIPPED"),
                Confirmed = orders.Count(o => o.Status == "CONFIRMED"),
                Returned = orders.Count(o => o.Status == "RETURNED")
            };
        }

        private FavoriteProduct CalculateFavoriteProduct(List<Order> orders)
        {
            var productGroups = orders
                .SelectMany(o => o.OrderItems ?? new List<OrderItem>())
                .Where(oi => oi.Part != null)
                .GroupBy(oi => new { oi.PartId, oi.Part.PartName })
                .OrderByDescending(g => g.Sum(oi => oi.Quantity))
                .FirstOrDefault();

            if (productGroups != null)
            {
                return new FavoriteProduct
                {
                    ProductId = productGroups.Key.PartId,
                    ProductName = productGroups.Key.PartName,
                    Count = productGroups.Sum(oi => oi.Quantity)
                };
            }

            return new FavoriteProduct { ProductId = 0, ProductName = "Không có", Count = 0 };
        }

        private OrderRecentActivity CalculateRecentActivity(List<Order> orders)
        {
            var deliveredOrders = orders.Where(o => o.Status == "DELIVERED").OrderByDescending(o => o.CreatedAt);
            var lastOrder = deliveredOrders.FirstOrDefault();

            var lastProduct = lastOrder?.OrderItems?.FirstOrDefault()?.Part?.PartName;

            return new OrderRecentActivity
            {
                LastOrderDate = lastOrder?.CreatedAt,
                LastProduct = lastProduct,
                DaysSinceLastOrder = lastOrder != null ? (DateTime.Now - lastOrder.CreatedAt).Days : null
            };
        }
    }
}
