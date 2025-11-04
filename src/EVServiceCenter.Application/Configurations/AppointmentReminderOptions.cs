namespace EVServiceCenter.Application.Configurations
{
    public class AppointmentReminderOptions
    {
        public const string SectionName = "AppointmentReminder";
        public bool Enabled { get; set; } = true;
        public int IntervalMinutes { get; set; } = 15;
        public int? WindowHours { get; set; }
    }
}


