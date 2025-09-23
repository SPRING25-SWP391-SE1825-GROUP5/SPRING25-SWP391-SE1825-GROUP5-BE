using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EVServiceCenter.Domain.Entities
{
    [Table("CenterSchedule", Schema = "dbo")]
    public class CenterSchedule
    {
        [Key]
        [Column("CenterScheduleID")]
        public int CenterScheduleId { get; set; }

        [Required]
        [Column("CenterID")]
        public int CenterId { get; set; }

        [Required]
        [Column("DayOfWeek")]
        [Range(0, 6, ErrorMessage = "DayOfWeek must be between 0 and 6 (Sunday to Saturday)")]
        public byte DayOfWeek { get; set; }

        [Required]
        [Column("StartTime")]
        public TimeOnly StartTime { get; set; }

        [Required]
        [Column("EndTime")]
        public TimeOnly EndTime { get; set; }

        // SlotLength removed: system uses fixed 30-minute slots

        [Required]
        [Column("EffectiveFrom")]
        public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

        [Column("EffectiveTo")]
        public DateOnly? EffectiveTo { get; set; }

        [Required]
        [Column("CapacityTotal")]
        [Range(1, int.MaxValue, ErrorMessage = "CapacityTotal must be greater than 0")]
        public int CapacityTotal { get; set; }

        [Required]
        [Column("CapacityLeft")]
        [Range(0, int.MaxValue, ErrorMessage = "CapacityLeft must be greater than or equal to 0")]
        public int CapacityLeft { get; set; }

        [Required]
        [Column("IsActive")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("CenterId")]
        public virtual ServiceCenter? Center { get; set; }
    }
}
