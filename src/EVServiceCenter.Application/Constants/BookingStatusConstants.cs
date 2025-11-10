namespace EVServiceCenter.Application.Constants;

/// <summary>
/// Constants cho Booking Status
/// </summary>
public static class BookingStatusConstants
{
    public const string Pending = "PENDING";
    public const string Confirmed = "CONFIRMED";
    public const string CheckedIn = "CHECKED_IN";
    public const string InProgress = "IN_PROGRESS";
    public const string Completed = "COMPLETED";
    public const string Paid = "PAID";
    public const string Cancelled = "CANCELLED";
    public const string Canceled = "CANCELED"; // Alternative spelling

    /// <summary>
    /// All valid booking statuses
    /// </summary>
    public static readonly string[] AllStatuses = new[]
    {
        Pending,
        Confirmed,
        CheckedIn,
        InProgress,
        Completed,
        Paid,
        Cancelled
    };

    /// <summary>
    /// Statuses that indicate booking is finished
    /// </summary>
    public static readonly string[] FinishedStatuses = new[]
    {
        Completed,
        Paid,
        Cancelled
    };

    /// <summary>
    /// Statuses that allow payment
    /// </summary>
    public static readonly string[] PaymentAllowedStatuses = new[]
    {
        Completed
    };
}

