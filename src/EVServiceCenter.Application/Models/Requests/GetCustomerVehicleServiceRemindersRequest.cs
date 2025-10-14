using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class GetCustomerVehicleServiceRemindersRequest
    {
        [Required]
        public int CustomerId { get; set; }

        public int? VehicleId { get; set; }

        public bool? IsCompleted { get; set; }

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public int? ServiceId { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public string SortBy { get; set; } = "DueDate";

        public string SortDirection { get; set; } = "asc"; // asc, desc
    }
}
