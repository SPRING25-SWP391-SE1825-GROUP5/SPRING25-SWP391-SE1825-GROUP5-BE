using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests
{
    public class SendVehicleMaintenanceAlertsRequest
    {
        /// <summary>
        /// ID xe cụ thể (tùy chọn)
        /// </summary>
        public int? VehicleId { get; set; }

        /// <summary>
        /// ID khách hàng cụ thể (tùy chọn)
        /// </summary>
        public int? CustomerId { get; set; }

        /// <summary>
        /// Số ngày sắp tới để lọc reminders (tùy chọn, mặc định từ config)
        /// </summary>
        [Range(1, 365, ErrorMessage = "Số ngày sắp tới phải từ 1 đến 365")]
        public int? UpcomingDays { get; set; }

        /// <summary>
        /// Có gửi email hay không (mặc định: true)
        /// </summary>
        public bool SendEmail { get; set; } = true;

        /// <summary>
        /// Có gửi SMS hay không (mặc định: false)
        /// </summary>
        public bool SendSms { get; set; } = false;
    }
}
