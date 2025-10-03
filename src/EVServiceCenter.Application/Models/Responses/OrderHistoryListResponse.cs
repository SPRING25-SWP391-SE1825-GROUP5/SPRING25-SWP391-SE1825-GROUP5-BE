using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class OrderHistoryListResponse
    {
        public List<OrderHistorySummary> Orders { get; set; } = new List<OrderHistorySummary>();
        public OrderPaginationInfo Pagination { get; set; } = null!;
        public OrderFilterInfo Filters { get; set; } = null!;
    }

    public class OrderHistorySummary
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrderPaginationInfo
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class OrderFilterInfo
    {
        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string SortBy { get; set; } = null!;
        public string SortOrder { get; set; } = null!;
    }
}
