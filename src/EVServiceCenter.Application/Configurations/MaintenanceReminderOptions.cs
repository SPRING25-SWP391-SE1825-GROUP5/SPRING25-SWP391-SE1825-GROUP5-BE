namespace EVServiceCenter.Application.Configurations
{
    public class MaintenanceReminderOptions
    {
        public int UpcomingDays { get; set; } = 7;
        public int DispatchHourLocal { get; set; } = 9;
        public string TimeZoneId { get; set; } = "SE Asia Standard Time";
        public int AppointmentReminderHours { get; set; } = 24;

        // Fallback values khi ServiceChecklistTemplate không có MinKm/MaxDate (Phương án 2)
        public int? DefaultIntervalDays { get; set; } = 90;        // Mặc định 90 ngày
        public int? DefaultIntervalMileage { get; set; } = 10000;  // Mặc định 10000 km
    }
}


