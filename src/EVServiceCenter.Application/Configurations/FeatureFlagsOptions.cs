namespace EVServiceCenter.Application.Configurations
{
    public class FeatureFlagsOptions
    {
        public bool EnableMaintenanceReminder { get; set; } = true;
        public bool EnableSoftWarning { get; set; } = true;
        public bool EnableGuestBooking { get; set; } = true;
        public bool EnableRealTimeBooking { get; set; } = true;
        public bool EnablePromotions { get; set; } = true;
        public bool EnableFeedback { get; set; } = true;
        public bool EnableNotifications { get; set; } = true;
        public bool EnableFileUpload { get; set; } = true;
        public bool EnableMultiplePaymentMethods { get; set; } = true;
        public bool EnableBookingHistory { get; set; } = true;
        public bool EnableOrderHistory { get; set; } = true;
        public bool EnableVehicleManagement { get; set; } = true;
        public bool EnableTechnicianAssignment { get; set; } = true;
        public bool EnableInventoryManagement { get; set; } = true;
        public bool EnableReports { get; set; } = true;
    }
}
