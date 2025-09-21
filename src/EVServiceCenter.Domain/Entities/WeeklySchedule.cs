using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVServiceCenter.Domain.Entities
{
    [Table("WeeklySchedule", Schema = "dbo")]
    public class WeeklySchedule
    {
        [Key]
        [Column("WeeklyScheduleID")]
        public int WeeklyScheduleId { get; set; }

        [Required]
        [Column("LocationID")]
        public int LocationId { get; set; }

        [Column("TechnicianID")]
        public int? TechnicianId { get; set; }

        [Required]
        [Column("DayOfWeek")]
        [Range(0, 6, ErrorMessage = "DayOfWeek must be between 0 and 6 (Sunday to Saturday)")]
        public byte DayOfWeek { get; set; }

        [Required]
        [Column("IsOpen")]
        public bool IsOpen { get; set; } = true;

        [Column("StartTime")]
        public TimeOnly? StartTime { get; set; }

        [Column("EndTime")]
        public TimeOnly? EndTime { get; set; }

        [Column("BreakStart")]
        public TimeOnly? BreakStart { get; set; }

        [Column("BreakEnd")]
        public TimeOnly? BreakEnd { get; set; }

        [Required]
        [Column("BufferMinutes")]
        [Range(0, 255, ErrorMessage = "BufferMinutes must be between 0 and 255")]
        public byte BufferMinutes { get; set; } = 10;

        [Required]
        [Column("StepMinutes")]
        [Range(1, 255, ErrorMessage = "StepMinutes must be between 1 and 255")]
        public byte StepMinutes { get; set; } = 30;

        [Required]
        [Column("EffectiveFrom")]
        public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        [Column("EffectiveTo")]
        public DateOnly? EffectiveTo { get; set; }

        [Required]
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        [Column("Notes")]
        [StringLength(300, ErrorMessage = "Notes cannot exceed 300 characters")]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("LocationId")]
        public virtual ServiceCenter? Location { get; set; }

        [ForeignKey("TechnicianId")]
        public virtual Technician? Technician { get; set; }
    }
}

