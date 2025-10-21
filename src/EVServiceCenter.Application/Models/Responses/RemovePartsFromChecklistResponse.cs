using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class RemovePartsFromChecklistResponse
    {
        public int ServiceId { get; set; }
        public required string ServiceName { get; set; }
        public int RemovedPartsCount { get; set; }
        public required List<int> RemovedPartIds { get; set; } = new List<int>();
    }
}
