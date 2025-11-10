using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    /// <summary>
    /// Request để update phụ tùng khách cung cấp cho booking
    /// </summary>
    public class UpdateBookingCustomerPartsRequest
    {
        /// <summary>
        /// Danh sách phụ tùng đã mua muốn sử dụng cho booking này
        /// Nếu null hoặc empty, sẽ xóa tất cả phụ tùng khách cung cấp
        /// </summary>
        public List<OrderItemUsageRequest>? OrderItemUsages { get; set; }
    }
}

