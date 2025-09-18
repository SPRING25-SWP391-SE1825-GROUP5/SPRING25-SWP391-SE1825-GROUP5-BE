using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class VehicleListResponse
    {
        public List<VehicleResponse> Vehicles { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
