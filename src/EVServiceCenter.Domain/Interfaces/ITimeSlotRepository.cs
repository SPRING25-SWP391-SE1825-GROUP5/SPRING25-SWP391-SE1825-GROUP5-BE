using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface ITimeSlotRepository
    {
        Task<List<TimeSlot>> GetAllTimeSlotsAsync();
        Task<List<TimeSlot>> GetActiveTimeSlotsAsync();
        Task<TimeSlot> CreateTimeSlotAsync(TimeSlot timeSlot);
    }
}
