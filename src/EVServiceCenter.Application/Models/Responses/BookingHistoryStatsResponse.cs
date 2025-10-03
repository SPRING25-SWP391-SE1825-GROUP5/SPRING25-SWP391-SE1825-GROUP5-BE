using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class BookingHistoryStatsResponse
    {
        public int TotalBookings { get; set; }
        public StatusBreakdown StatusBreakdown { get; set; } = null!;
        public decimal TotalSpent { get; set; }
        public decimal AverageCost { get; set; }
        public FavoriteService FavoriteService { get; set; } = null!;
        public FavoriteCenter FavoriteCenter { get; set; } = null!;
        public RecentActivity RecentActivity { get; set; } = null!;
        public string Period { get; set; } = null!;
    }

    public class StatusBreakdown
    {
        public int Completed { get; set; }
        public int Cancelled { get; set; }
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int Confirmed { get; set; }
    }

    public class FavoriteService
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; } = null!;
        public int Count { get; set; }
    }

    public class FavoriteCenter
    {
        public int CenterId { get; set; }
        public string CenterName { get; set; } = null!;
        public int Count { get; set; }
    }

    public class RecentActivity
    {
        public DateTime? LastBookingDate { get; set; }
        public string? LastService { get; set; }
        public int? DaysSinceLastVisit { get; set; }
    }
}
