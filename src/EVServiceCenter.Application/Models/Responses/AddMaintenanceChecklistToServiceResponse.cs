using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class AddMaintenanceChecklistToServiceResponse
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public int AddedItemsCount { get; set; }
        public List<MaintenanceChecklistItemResponse> AddedItems { get; set; } = new List<MaintenanceChecklistItemResponse>();
    }
}





