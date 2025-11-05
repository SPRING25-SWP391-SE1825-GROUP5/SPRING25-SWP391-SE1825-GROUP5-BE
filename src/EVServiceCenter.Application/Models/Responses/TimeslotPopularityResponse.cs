using System;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    /// <summary>
    /// Response cho Timeslot Popularity API - Đánh giá số lượng booking của từng timeslot
    /// </summary>
    public class TimeslotPopularityResponse
    {
        public bool Success { get; set; } = true;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<TimeslotPopularityData> Timeslots { get; set; } = new List<TimeslotPopularityData>();
        public int TotalBookings { get; set; }
    }

    /// <summary>
    /// Dữ liệu popularity của một timeslot
    /// </summary>
    public class TimeslotPopularityData
    {
        /// <summary>
        /// ID của timeslot
        /// </summary>
        public int SlotId { get; set; }

        /// <summary>
        /// Thời gian của slot (ví dụ: "08:00:00")
        /// </summary>
        public TimeOnly SlotTime { get; set; }

        /// <summary>
        /// Label của slot (ví dụ: "08:00 - 09:00")
        /// </summary>
        public string SlotLabel { get; set; } = string.Empty;

        /// <summary>
        /// Số lượng booking đã được đặt cho timeslot này (toàn hệ thống)
        /// </summary>
        public int BookingCount { get; set; }

        /// <summary>
        /// Trạng thái active của timeslot
        /// </summary>
        public bool IsActive { get; set; }
    }
}

