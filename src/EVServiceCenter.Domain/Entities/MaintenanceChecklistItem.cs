using System;

namespace EVServiceCenter.Domain.Entities
{
    public class MaintenanceChecklistItem
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
