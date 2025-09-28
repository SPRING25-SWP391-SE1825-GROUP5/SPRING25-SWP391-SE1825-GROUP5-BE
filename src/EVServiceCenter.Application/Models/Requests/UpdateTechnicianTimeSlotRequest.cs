namespace EVServiceCenter.Application.Models.Requests
{
    public class UpdateTechnicianTimeSlotRequest
    {
        public bool IsAvailable { get; set; }
        public string? Notes { get; set; }
    }
}