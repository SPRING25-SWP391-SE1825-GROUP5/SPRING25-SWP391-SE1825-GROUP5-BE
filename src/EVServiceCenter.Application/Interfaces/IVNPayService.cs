using System.Threading.Tasks;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IVNPayService
    {
        /// <summary>
        /// Tạo payment URL cho VNPay
        /// </summary>
        Task<string> CreatePaymentUrlAsync(int bookingId, decimal amount, string description, string? customerName = null);

        /// <summary>
        /// Verify payment response từ VNPay
        /// </summary>
        bool VerifyPaymentResponse(Dictionary<string, string> vnpayData, string vnp_SecureHash);

        /// <summary>
        /// Lấy bookingId từ VNPay response
        /// </summary>
        int? GetBookingIdFromResponse(Dictionary<string, string> vnpayData);
    }
}

