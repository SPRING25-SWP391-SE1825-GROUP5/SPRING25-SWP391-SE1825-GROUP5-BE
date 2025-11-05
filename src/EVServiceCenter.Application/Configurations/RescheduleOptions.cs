namespace EVServiceCenter.Application.Configurations
{
    public class RescheduleOptions
    {
        public const string SectionName = "RescheduleOptions";
        public int CutoffHours { get; set; } = 2;
        public bool AllowCrossTechnician { get; set; } = true;
        public bool NotifyStaffOnReschedule { get; set; } = true;
    }
}


