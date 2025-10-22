using System;

namespace EVServiceCenter.Application.Helpers
{
    /// <summary>
    /// Helper để xử lý thời gian với timezone
    /// </summary>
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        
        /// <summary>
        /// Lấy thời gian hiện tại theo múi giờ Việt Nam
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTimeZone);
        
        /// <summary>
        /// Chuyển đổi UTC sang giờ Việt Nam
        /// </summary>
        public static DateTime ToVietnamTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Unspecified)
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VietnamTimeZone);
        }
        
        /// <summary>
        /// Chuyển đổi giờ Việt Nam sang UTC
        /// </summary>
        public static DateTime ToUtc(DateTime vietnamDateTime)
        {
            if (vietnamDateTime.Kind == DateTimeKind.Unspecified)
                vietnamDateTime = DateTime.SpecifyKind(vietnamDateTime, DateTimeKind.Local);
            
            return TimeZoneInfo.ConvertTimeToUtc(vietnamDateTime, VietnamTimeZone);
        }
        
        /// <summary>
        /// Lấy thời gian hiện tại theo UTC
        /// </summary>
        public static DateTime UtcNow => DateTime.UtcNow;
    }
}
