namespace EVServiceCenter.Application.Configurations
{
    public class MaintenanceReminderSchedulerOptions
    {
        public const string SectionName = "MaintenanceReminderScheduler";
        public bool Enabled { get; set; } = true;
        public int IntervalMinutes { get; set; } = 60;
    }
}


