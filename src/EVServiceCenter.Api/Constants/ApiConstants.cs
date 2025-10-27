namespace EVServiceCenter.Api.Constants;

public static class ApiConstants
{
    public static class Pagination
    {
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 100;
        public const int MinPageSize = 1;
    }

    public static class FileUpload
    {
        public const long MaxSizeBytes = 10 * 1024 * 1024;
        public const int MaxFilesPerUpload = 5;
        public static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx" };
    }

    public static class Booking
    {
        public const int MaxAdvanceBookingDays = 30;
        public const int MinAdvanceBookingHours = 2;
        public const int MaxBookingDurationHours = 8;
        public const int AllowCancellationHours = 24;
    }

    public static class Validation
    {
        public const int MinPasswordLength = 8;
        public const int MaxPasswordLength = 128;
        public const int MaxFeedbackLength = 1000;
        public const string PhoneNumberPattern = @"^[0-9]{10,11}$";
        public const string EmailPattern = @"^[^\s@]+@[^\s@]+\.[^\s@]+$";
        public const string LicensePlatePattern = @"^[0-9]{2}[A-Z]{1,2}[0-9]{4,5}$";
    }

    public static class Limits
    {
        public const int MaxVehiclesPerCustomer = 5;
        public const int MaxBookingsPerDay = 3;
        public const int MaxPromotionsPerCustomer = 10;
    }

    public static class Timeouts
    {
        public const int SessionTimeoutMinutes = 30;
        public const int GuestSessionTimeoutMinutes = 30;
        public const int OtpExpiryMinutes = 5;
        public const int LockoutDurationMinutes = 15;
    }

    public static class Roles
    {
        public const string Admin = "ADMIN";
        public const string Technician = "TECHNICIAN";
        public const string Customer = "CUSTOMER";
        public const string Staff = "STAFF";
    }

    public static class BookingStatus
    {
        public const string Pending = "PENDING";
        public const string Confirmed = "CONFIRMED";
        public const string InProgress = "IN_PROGRESS";
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELLED";
    }

    public static class PaymentStatus
    {
        public const string Pending = "PENDING";
        public const string Paid = "PAID";
        public const string Failed = "FAILED";
        public const string Refunded = "REFUNDED";
    }

    public static class ServiceType
    {
        public const string Maintenance = "MAINTENANCE";
        public const string Repair = "REPAIR";
        public const string Inspection = "INSPECTION";
    }

    public static class DiscountType
    {
        public const string Percent = "PERCENT";
        public const string Fixed = "FIXED";
    }

    public static class PromotionStatus
    {
        public const string Active = "ACTIVE";
        public const string Expired = "EXPIRED";
        public const string Cancelled = "CANCELLED";
    }

    public static class ErrorMessages
    {
        public const string InvalidData = "Dữ liệu không hợp lệ";
        public const string NotFound = "Không tìm thấy dữ liệu";
        public const string Unauthorized = "Không có quyền truy cập";
        public const string Forbidden = "Bị cấm truy cập";
        public const string InternalServerError = "Lỗi hệ thống";
        public const string Conflict = "Xung đột dữ liệu";
        public const string ValidationFailed = "Xác thực dữ liệu thất bại";
        public const string EmailSendingFailed = "Gửi email thất bại";
        public const string FileUploadFailed = "Tải lên file thất bại";
        public const string PaymentFailed = "Thanh toán thất bại";
    }

    public static class SuccessMessages
    {
        public const string Created = "Tạo thành công";
        public const string Updated = "Cập nhật thành công";
        public const string Deleted = "Xóa thành công";
        public const string Retrieved = "Lấy dữ liệu thành công";
        public const string OperationSuccess = "Thao tác thành công";
        public const string EmailSent = "Gửi email thành công";
        public const string PaymentSuccess = "Thanh toán thành công";
        public const string BookingCreated = "Đặt lịch thành công";
        public const string BookingCancelled = "Hủy đặt lịch thành công";
    }

    public static class Endpoints
    {
        public const string Auth = "/api/auth";
        public const string Booking = "/api/booking";
        public const string Services = "/api/services";
        public const string Vehicles = "/api/vehicles";
        public const string Promotions = "/api/promotions";
        public const string Feedback = "/api/feedback";
        public const string Swagger = "/swagger";
        public const string Health = "/health";
    }

    public static class Support
    {
        public const string Email = "support@evservicecenter.com";
        public const string Phone = "1900-1234";
        public const string WorkingHours = "8:00 - 17:00 (Thứ 2 - Thứ 6)";
    }
}
