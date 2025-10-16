using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class InventoryListResponse
    {
        public required List<InventoryResponse> Inventories { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
