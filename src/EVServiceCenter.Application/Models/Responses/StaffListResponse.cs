using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class StaffListResponse
    {
        public required List<StaffResponse> Staff { get; set; } = new List<StaffResponse>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
