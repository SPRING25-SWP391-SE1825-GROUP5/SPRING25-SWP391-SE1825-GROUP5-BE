using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface ITimeSlotRepository
    {
        Task<List<TimeSlot>> GetAllTimeSlotsAsync();
        Task<List<TimeSlot>> GetActiveTimeSlotsAsync();
        Task<TimeSlot> GetByIdAsync(int slotId);
        Task<TimeSlot> CreateTimeSlotAsync(TimeSlot timeSlot);
        Task<TimeSlot> UpdateAsync(TimeSlot timeSlot);
        Task<bool> DeleteAsync(int slotId);
    }
}
