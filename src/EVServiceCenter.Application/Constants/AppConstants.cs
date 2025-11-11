namespace EVServiceCenter.Application.Constants;

/// <summary>
/// Application-wide constants
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Payment amount constants
    /// </summary>
    public static class PaymentAmounts
    {
        /// <summary>
        /// Minimum payment amount in VND
        /// </summary>
        public const int MinAmountVnd = 1000;
    }

    /// <summary>
    /// Pagination constants
    /// </summary>
    public static class Pagination
    {
        public const int DefaultPage = 1;
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 100;
        public const int ReportsPageSize = 20;
    }

    /// <summary>
    /// Payment response codes
    /// </summary>
    public static class PaymentResponseCodes
    {
        public const string Success = "00";
        public const string InvalidSignature = "97";
        public const string CannotExtractBookingId = "99";
        public const string ConfirmFailed = "99";
        public const string InternalError = "99";
        public const string PaymentNotSuccessful = "00";
    }

    /// <summary>
    /// Transaction content format
    /// </summary>
    public static class TransactionContent
    {
        public const string Format = "Pay{0}ment";
    }

    /// <summary>
    /// Default IP address (fallback)
    /// </summary>
    public static class DefaultIpAddress
    {
        public const string Localhost = "127.0.0.1";
    }

    /// <summary>
    /// Customer ID constants
    /// </summary>
    public static class CustomerId
    {
        public const int Guest = 0;
    }
}

