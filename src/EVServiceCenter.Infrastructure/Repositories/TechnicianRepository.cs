using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class TechnicianRepository : ITechnicianRepository
    {
        private readonly EVDbContext _context;

        public TechnicianRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<Technician>> GetAllTechniciansAsync()
        {
            return await _context.Technicians
                .Include(t => t.User)
                .Include(t => t.Center)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<Technician> GetTechnicianByIdAsync(int technicianId)
        {
            return await _context.Technicians
                .Include(t => t.User)
                .Include(t => t.Center)
                .FirstOrDefaultAsync(t => t.TechnicianId == technicianId);
        }

        public async Task<List<TechnicianTimeSlot>> GetTechnicianAvailabilityAsync(int technicianId, DateOnly date)
        {
            return await _context.TechnicianTimeSlots
                .Include(tts => tts.Slot)
                .Where(tts => tts.TechnicianId == technicianId && tts.WorkDate == date)
                .OrderBy(tts => tts.Slot.SlotTime)
                .ToListAsync();
        }

        public async Task UpdateTechnicianAvailabilityAsync(List<TechnicianTimeSlot> timeSlots)
        {
            foreach (var timeSlot in timeSlots)
            {
                var existing = await _context.TechnicianTimeSlots
                    .FirstOrDefaultAsync(tts => tts.TechnicianId == timeSlot.TechnicianId 
                                            && tts.WorkDate == timeSlot.WorkDate 
                                            && tts.SlotId == timeSlot.SlotId);

                if (existing != null)
                {
                    existing.IsAvailable = timeSlot.IsAvailable;
                    existing.Notes = timeSlot.Notes;
                    _context.TechnicianTimeSlots.Update(existing);
                }
                else
                {
                    _context.TechnicianTimeSlots.Add(timeSlot);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<TechnicianTimeSlot> GetTechnicianTimeSlotAsync(int technicianId, DateOnly date, int slotId)
        {
            return await _context.TechnicianTimeSlots
                .FirstOrDefaultAsync(tts => tts.TechnicianId == technicianId 
                                        && tts.WorkDate == date 
                                        && tts.SlotId == slotId);
        }
    }
}
