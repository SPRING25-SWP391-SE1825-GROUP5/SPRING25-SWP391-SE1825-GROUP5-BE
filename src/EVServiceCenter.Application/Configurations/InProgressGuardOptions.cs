namespace EVServiceCenter.Application.Configurations
{
    public class InProgressGuardOptions
    {
        public const string SectionName = "InProgressGuard";
        public bool Enabled { get; set; } = true;
        public int GraceBeforeMinutes { get; set; } = 15; 
        public int GraceAfterMinutes { get; set; } = 60;  
    }
}


