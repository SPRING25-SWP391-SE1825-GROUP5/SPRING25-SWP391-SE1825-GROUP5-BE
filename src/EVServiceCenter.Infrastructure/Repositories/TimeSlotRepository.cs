using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class TimeSlotRepository : ITimeSlotRepository
    {
        private readonly EVDbContext _context;

        public TimeSlotRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<TimeSlot>> GetAllTimeSlotsAsync()
        {
            return await _context.TimeSlots
                .OrderBy(ts => ts.SlotTime)
                .ToListAsync();
        }

        public async Task<List<TimeSlot>> GetActiveTimeSlotsAsync()
        {
            return await _context.TimeSlots
                .Where(ts => ts.IsActive)
                .OrderBy(ts => ts.SlotTime)
                .ToListAsync();
        }

        public async Task<TimeSlot> GetByIdAsync(int slotId)
        {
            return await _context.TimeSlots.FirstOrDefaultAsync(ts => ts.SlotId == slotId);
        }

        public async Task<TimeSlot> CreateTimeSlotAsync(TimeSlot timeSlot)
        {
            _context.TimeSlots.Add(timeSlot);
            await _context.SaveChangesAsync();
            return timeSlot;
        }
    }
}
