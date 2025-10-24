using System.Collections.Generic;
using System.Threading.Tasks;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IPayOSService
    {
        Task<string> CreatePaymentLinkAsync(int bookingId, decimal amount, string description, string? customerName = null);
        Task<PayOSPaymentInfo?> GetPaymentInfoAsync(int orderCode);
        Task<bool> CancelPaymentLinkAsync(int orderCode);
        bool VerifyPaymentWebhook(string webhookData);
        Task<bool> HandlePaymentCallbackAsync(string orderCode);
    }

    public class PayOSPaymentInfo
    {
        public string Id { get; set; } = string.Empty;
        public int OrderCode { get; set; }
        public int Amount { get; set; }
        public int AmountPaid { get; set; }
        public int AmountRemaining { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
        public string CanceledAt { get; set; } = string.Empty;
        public string CancellationReason { get; set; } = string.Empty;
        public List<PayOSTransaction> Transactions { get; set; } = new List<PayOSTransaction>();
        
        // Legacy properties for backward compatibility
        public string CheckoutUrl { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class PayOSTransaction
    {
        public string AccountNumber { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string CounterAccountBankId { get; set; } = string.Empty;
        public string? CounterAccountBankName { get; set; }
        public string CounterAccountName { get; set; } = string.Empty;
        public string CounterAccountNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string TransactionDateTime { get; set; } = string.Empty;
        public string? VirtualAccountName { get; set; }
        public string? VirtualAccountNumber { get; set; }
    }
}
