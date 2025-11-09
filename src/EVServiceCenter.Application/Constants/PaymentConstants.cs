namespace EVServiceCenter.Application.Constants;

/// <summary>
/// Constants cho Payment Service
/// Đặt trong Application layer để Service layer có thể sử dụng
/// </summary>
public static class PaymentConstants
{
    /// <summary>
    /// Order Status constants
    /// </summary>
    public static class OrderStatus
    {
        public const string Pending = "PENDING";
        public const string Paid = "PAID";
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELLED";
        public const string Canceled = "CANCELED";
    }

    /// <summary>
    /// Invoice Status constants
    /// </summary>
    public static class InvoiceStatus
    {
        public const string Pending = "PENDING";
        public const string Paid = "PAID";
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELLED";
    }

    /// <summary>
    /// PayOS Error Codes
    /// </summary>
    public static class PayOSErrorCodes
    {
        public const string PaymentExists = "231";
    }

    /// <summary>
    /// Payment status constants
    /// </summary>
    public static class PaymentStatus
    {
        public const string Pending = "PENDING";
        public const string Paid = "PAID";
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELLED";
    }
}

