namespace EVServiceCenter.Application.Configurations
{
    public class MaintenanceReminderOptions
    {
        public int UpcomingDays { get; set; } = 7;
        public int DispatchHourLocal { get; set; } = 9;
        public string TimeZoneId { get; set; } = "SE Asia Standard Time";
        public int AppointmentReminderHours { get; set; } = 24;
    }
}


