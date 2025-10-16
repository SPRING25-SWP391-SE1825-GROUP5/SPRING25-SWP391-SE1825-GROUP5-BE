using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class OrderHistoryStatsResponse
    {
        public int TotalOrders { get; set; }
        public OrderStatusBreakdown StatusBreakdown { get; set; } = null!;
        public decimal TotalSpent { get; set; }
        public decimal AverageOrderValue { get; set; }
        public FavoriteProduct FavoriteProduct { get; set; } = null!;
        public OrderRecentActivity RecentActivity { get; set; } = null!;
        public required string Period { get; set; } = null!;
    }

    public class OrderStatusBreakdown
    {
        public int Delivered { get; set; }
        public int Cancelled { get; set; }
        public int Pending { get; set; }
        public int Shipped { get; set; }
        public int Confirmed { get; set; }
        public int Returned { get; set; }
    }

    public class FavoriteProduct
    {
        public int ProductId { get; set; }
        public required string ProductName { get; set; } = null!;
        public int Count { get; set; }
    }

    public class OrderRecentActivity
    {
        public DateTime? LastOrderDate { get; set; }
        public string? LastProduct { get; set; }
        public int? DaysSinceLastOrder { get; set; }
    }
}
