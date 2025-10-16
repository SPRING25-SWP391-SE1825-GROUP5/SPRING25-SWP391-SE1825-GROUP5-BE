using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class BookingHistoryListResponse
    {
        public required List<BookingHistorySummary> Bookings { get; set; } = new List<BookingHistorySummary>();
        public PaginationInfo Pagination { get; set; } = null!;
        public FilterInfo Filters { get; set; } = null!;
    }

    public class BookingHistorySummary
    {
        public int BookingId { get; set; }
        public required string BookingCode { get; set; } = null!;
        public DateOnly BookingDate { get; set; }
        public required string Status { get; set; } = null!;
        public required string CenterName { get; set; } = null!;
        public VehicleSummary VehicleInfo { get; set; } = null!;
        public required string ServiceName { get; set; } = null!;
        public string? TechnicianName { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class VehicleSummary
    {
        public required string LicensePlate { get; set; } = null!;
        public string? ModelName { get; set; }
    }

    public class PaginationInfo
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class FilterInfo
    {
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public required string SortBy { get; set; } = null!;
        public required string SortOrder { get; set; } = null!;
    }
}
