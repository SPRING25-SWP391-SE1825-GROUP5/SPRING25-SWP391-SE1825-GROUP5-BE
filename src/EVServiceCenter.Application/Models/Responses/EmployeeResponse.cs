using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EVServiceCenter.Application.Models.Responses
{
    public class EmployeeResponse
    {
        public string Type { get; set; } = string.Empty; // "STAFF" hoặc "TECHNICIAN"
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? StaffId { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? TechnicianId { get; set; }
        
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // STAFF hoặc TECHNICIAN
        public bool IsActive { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int CenterId { get; set; }
        
        public string CenterName { get; set; } = string.Empty;
        
        // Các trường riêng của Technician
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Position { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? Rating { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
    
    public class EmployeeListResponse
    {
        public List<EmployeeResponse> Employees { get; set; } = new List<EmployeeResponse>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}

