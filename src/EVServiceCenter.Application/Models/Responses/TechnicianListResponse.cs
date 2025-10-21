using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class TechnicianListResponse
    {
        public required List<TechnicianResponse> Technicians { get; set; } = new List<TechnicianResponse>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}