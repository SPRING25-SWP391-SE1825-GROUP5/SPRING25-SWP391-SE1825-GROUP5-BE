using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class MaintenanceChecklistItemListResponse
    {
        public List<MaintenanceChecklistItemResponse> Items { get; set; } = new List<MaintenanceChecklistItemResponse>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}

