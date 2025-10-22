using System.Threading.Tasks;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IPayOSService
    {
        Task<string> CreatePaymentLinkAsync(int bookingId, decimal amount, string description, string? customerName = null);
        Task<PayOSPaymentInfo?> GetPaymentInfoAsync(int orderCode);
        Task<bool> CancelPaymentLinkAsync(int orderCode);
        bool VerifyPaymentWebhook(string webhookData);
    }

    public class PayOSPaymentInfo
    {
        public string CheckoutUrl { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
        public int OrderCode { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
