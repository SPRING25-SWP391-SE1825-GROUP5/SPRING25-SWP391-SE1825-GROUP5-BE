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
    public class TechnicianTimeSlotRepository : ITechnicianTimeSlotRepository
    {
        private readonly EVDbContext _context;

        public TechnicianTimeSlotRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<TechnicianTimeSlot>> GetAllTechnicianTimeSlotsAsync()
        {
            return await _context.TechnicianTimeSlots
                .Include(tts => tts.Technician)
                .Include(tts => tts.Slot)
                .OrderBy(tts => tts.WorkDate)
                .ThenBy(tts => tts.Slot.SlotTime)
                .ToListAsync();
        }

        public async Task<TechnicianTimeSlot> GetTechnicianTimeSlotByIdAsync(int technicianSlotId)
        {
            return await _context.TechnicianTimeSlots
                .Include(tts => tts.Technician)
                .Include(tts => tts.Slot)
                .FirstOrDefaultAsync(tts => tts.TechnicianSlotId == technicianSlotId);
        }

        public async Task<List<TechnicianTimeSlot>> GetTechnicianTimeSlotsByTechnicianAsync(int technicianId)
        {
            return await _context.TechnicianTimeSlots
                .Include(tts => tts.Technician)
                .Include(tts => tts.Slot)
                .Where(tts => tts.TechnicianId == technicianId)
                .OrderBy(tts => tts.WorkDate)
                .ThenBy(tts => tts.Slot.SlotTime)
                .ToListAsync();
        }

        public async Task<List<TechnicianTimeSlot>> GetTechnicianTimeSlotsByDateAsync(DateOnly date)
        {
            return await _context.TechnicianTimeSlots
                .Include(tts => tts.Technician)
                .Include(tts => tts.Slot)
                .Where(tts => tts.WorkDate == date)
                .OrderBy(tts => tts.Slot.SlotTime)
                .ToListAsync();
        }

        public async Task<List<TechnicianTimeSlot>> GetTechnicianTimeSlotsByTechnicianAndDateAsync(int technicianId, DateOnly date)
        {
            return await _context.TechnicianTimeSlots
                .Include(tts => tts.Technician)
                .Include(tts => tts.Slot)
                .Where(tts => tts.TechnicianId == technicianId && tts.WorkDate == date)
                .OrderBy(tts => tts.Slot.SlotTime)
                .ToListAsync();
        }

        public async Task<List<TechnicianTimeSlot>> GetAvailableTechnicianTimeSlotsAsync(int technicianId, DateOnly date)
        {
            return await _context.TechnicianTimeSlots
                .Include(tts => tts.Technician)
                .Include(tts => tts.Slot)
                .Where(tts => tts.TechnicianId == technicianId && 
                            tts.WorkDate == date && 
                            tts.IsAvailable && 
                            !tts.IsBooked)
                .OrderBy(tts => tts.Slot.SlotTime)
                .ToListAsync();
        }

        public async Task<TechnicianTimeSlot> CreateTechnicianTimeSlotAsync(TechnicianTimeSlot technicianTimeSlot)
        {
            _context.TechnicianTimeSlots.Add(technicianTimeSlot);
            await _context.SaveChangesAsync();
            return technicianTimeSlot;
        }

        public async Task<TechnicianTimeSlot> UpdateTechnicianTimeSlotAsync(TechnicianTimeSlot technicianTimeSlot)
        {
            _context.TechnicianTimeSlots.Update(technicianTimeSlot);
            await _context.SaveChangesAsync();
            return technicianTimeSlot;
        }

        public async Task<bool> DeleteTechnicianTimeSlotAsync(int technicianSlotId)
        {
            var technicianTimeSlot = await _context.TechnicianTimeSlots
                .FirstOrDefaultAsync(tts => tts.TechnicianSlotId == technicianSlotId);
            
            if (technicianTimeSlot == null)
                return false;

            _context.TechnicianTimeSlots.Remove(technicianTimeSlot);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int technicianSlotId)
        {
            return await _context.TechnicianTimeSlots
                .AnyAsync(tts => tts.TechnicianSlotId == technicianSlotId);
        }

        public async Task<bool> IsSlotAvailableAsync(int technicianId, DateOnly date, int slotId)
        {
            var technicianTimeSlot = await _context.TechnicianTimeSlots
                .FirstOrDefaultAsync(tts => tts.TechnicianId == technicianId && 
                                          tts.WorkDate == date && 
                                          tts.SlotId == slotId);

            // If no record exists, slot is available by default
            if (technicianTimeSlot == null)
                return true;

            // Slot is available if it's marked as available and not booked
            return technicianTimeSlot.IsAvailable && !technicianTimeSlot.IsBooked;
        }

        public async Task<bool> ReserveSlotAsync(int technicianId, DateOnly date, int slotId, int? bookingId = null)
        {
            var technicianTimeSlot = await _context.TechnicianTimeSlots
                .FirstOrDefaultAsync(tts => tts.TechnicianId == technicianId && 
                                          tts.WorkDate == date && 
                                          tts.SlotId == slotId);

            if (technicianTimeSlot == null)
            {
                // Create new record
                technicianTimeSlot = new TechnicianTimeSlot
                {
                    TechnicianId = technicianId,
                    WorkDate = date,
                    SlotId = slotId,
                    IsAvailable = false,
                    IsBooked = true,
                    BookingId = bookingId,
                    CreatedAt = System.DateTime.UtcNow
                };
                _context.TechnicianTimeSlots.Add(technicianTimeSlot);
            }
            else
            {
                // Update existing record
                if (technicianTimeSlot.IsBooked)
                    return false; // Already booked

                technicianTimeSlot.IsAvailable = false;
                technicianTimeSlot.IsBooked = true;
                technicianTimeSlot.BookingId = bookingId;
                _context.TechnicianTimeSlots.Update(technicianTimeSlot);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReleaseSlotAsync(int technicianId, DateOnly date, int slotId)
        {
            var technicianTimeSlot = await _context.TechnicianTimeSlots
                .FirstOrDefaultAsync(tts => tts.TechnicianId == technicianId && 
                                          tts.WorkDate == date && 
                                          tts.SlotId == slotId);

            if (technicianTimeSlot == null)
                return false;

            technicianTimeSlot.IsAvailable = true;
            technicianTimeSlot.IsBooked = false;
            technicianTimeSlot.BookingId = null;
            _context.TechnicianTimeSlots.Update(technicianTimeSlot);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
