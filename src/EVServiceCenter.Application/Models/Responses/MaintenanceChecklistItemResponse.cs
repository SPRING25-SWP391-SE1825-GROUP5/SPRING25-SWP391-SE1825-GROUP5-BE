using System;

namespace EVServiceCenter.Application.Models.Responses
{
    public class MaintenanceChecklistItemResponse
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

