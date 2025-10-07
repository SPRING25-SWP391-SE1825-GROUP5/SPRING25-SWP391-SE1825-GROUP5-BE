using System.Collections.Generic;

namespace EVServiceCenter.Application.Models.Responses
{
    public class RemovePartsFromChecklistResponse
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public int RemovedPartsCount { get; set; }
        public List<int> RemovedPartIds { get; set; } = new List<int>();
    }
}
