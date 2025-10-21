using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class UserListResponse
    {
        public required List<UserResponse> Users { get; set; } = new List<UserResponse>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
