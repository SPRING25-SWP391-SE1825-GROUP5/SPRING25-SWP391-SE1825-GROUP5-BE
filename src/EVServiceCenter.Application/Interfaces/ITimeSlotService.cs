using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface ITimeSlotService
    {
        Task<List<TimeSlotResponse>> GetAllTimeSlotsAsync();
        Task<List<TimeSlotResponse>> GetActiveTimeSlotsAsync();
        Task<TimeSlotResponse> CreateTimeSlotAsync(CreateTimeSlotRequest request);
    }
}
