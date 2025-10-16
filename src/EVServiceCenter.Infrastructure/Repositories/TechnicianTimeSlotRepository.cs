using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EVServiceCenter.Infrastructure.Repositories;

public class TechnicianTimeSlotRepository : ITechnicianTimeSlotRepository
{
    private readonly EVDbContext _context;

    public TechnicianTimeSlotRepository(EVDbContext context)
    {
        _context = context;
    }

    public async Task<TechnicianTimeSlot?> GetByIdAsync(int id)
    {
        return await _context.TechnicianTimeSlots
            .Include(t => t.Technician)
            .ThenInclude(x => x.User)
            .Include(t => t.Slot)
            .Include(t => t.Booking)
            .FirstOrDefaultAsync(t => t.TechnicianSlotId == id);
    }

    public async Task<List<TechnicianTimeSlot>> GetByTechnicianIdAsync(int technicianId)
    {
        return await _context.TechnicianTimeSlots
            .Include(t => t.Technician)
            .ThenInclude(x => x.User)
            .Include(t => t.Slot)
            .Include(t => t.Booking)
            .Where(t => t.TechnicianId == technicianId)
            .OrderBy(t => t.WorkDate)
            .ThenBy(t => t.Slot.SlotTime)
            .ToListAsync();
    }

    public async Task<List<TechnicianTimeSlot>> GetByDateAsync(DateTime date)
    {
        return await _context.TechnicianTimeSlots
            .Include(t => t.Technician)
            .ThenInclude(x => x.User)
            .Include(t => t.Slot)
            .Include(t => t.Booking)
            .Where(t => t.WorkDate.Date == date.Date)
            .OrderBy(t => t.Slot.SlotTime)
            .ToListAsync();
    }

    public async Task<List<TechnicianTimeSlot>> GetByTechnicianAndDateRangeAsync(int technicianId, DateTime startDate, DateTime endDate)
    {
        return await _context.TechnicianTimeSlots
            .Include(t => t.Technician)
            .Include(t => t.Slot)
            .Include(t => t.Booking)
            .Where(t => t.TechnicianId == technicianId && 
                       t.WorkDate >= startDate.Date && 
                       t.WorkDate <= endDate.Date)
            .OrderBy(t => t.WorkDate)
            .ThenBy(t => t.Slot.SlotTime)
            .ToListAsync();
    }

    public async Task<TechnicianTimeSlot> CreateAsync(TechnicianTimeSlot technicianTimeSlot)
    {
        _context.TechnicianTimeSlots.Add(technicianTimeSlot);
        await _context.SaveChangesAsync();
        return technicianTimeSlot;
    }

    public async Task<TechnicianTimeSlot> UpdateAsync(TechnicianTimeSlot technicianTimeSlot)
    {
        _context.TechnicianTimeSlots.Update(technicianTimeSlot);
        await _context.SaveChangesAsync();
        return technicianTimeSlot;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var technicianTimeSlot = await _context.TechnicianTimeSlots.FindAsync(id);
        if (technicianTimeSlot == null)
            return false;

        _context.TechnicianTimeSlots.Remove(technicianTimeSlot);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.TechnicianTimeSlots.AnyAsync(t => t.TechnicianSlotId == id);
    }

    public async Task<List<TechnicianTimeSlot>> GetAvailableSlotsAsync(DateTime date, int slotId)
    {
        return await _context.TechnicianTimeSlots
            .Include(t => t.Technician)
            .ThenInclude(x => x.User)
            .Include(t => t.Slot)
            .Where(t => t.WorkDate.Date == date.Date && 
                       t.SlotId == slotId && 
                       t.IsAvailable)
            .ToListAsync();
    }

    public async Task<bool> IsSlotAvailableAsync(int technicianId, DateTime date, int slotId)
    {
        var tts = await _context.TechnicianTimeSlots
            .FirstOrDefaultAsync(t => t.TechnicianId == technicianId &&
                                     t.WorkDate.Date == date.Date &&
                                     t.SlotId == slotId);

        // Nếu chưa có bản ghi → coi như còn trống (mặc định available)
        if (tts == null) return true;
        return tts.IsAvailable && tts.BookingId == null;
    }

    public async Task<bool> ReserveSlotAsync(int technicianId, DateTime date, int slotId, int bookingId)
    {
        // Tìm slot tồn tại bất kể trạng thái
        var timeSlot = await _context.TechnicianTimeSlots
            .FirstOrDefaultAsync(t => t.TechnicianId == technicianId &&
                                     t.WorkDate.Date == date.Date &&
                                     t.SlotId == slotId);

        if (timeSlot == null)
        {
            // Chưa có thì tạo mới và giữ chỗ
            timeSlot = new TechnicianTimeSlot
            {
                TechnicianId = technicianId,
                WorkDate = date.Date,
                SlotId = slotId,
                IsAvailable = false,
                BookingId = bookingId,
                CreatedAt = DateTime.UtcNow
            };
            _context.TechnicianTimeSlots.Add(timeSlot);
        }
        else
        {
            // Có rồi thì cập nhật giữ chỗ
            timeSlot.IsAvailable = false;
            timeSlot.BookingId = bookingId;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReleaseSlotAsync(int technicianId, DateTime date, int slotId)
    {
        var timeSlot = await _context.TechnicianTimeSlots
            .FirstOrDefaultAsync(t => t.TechnicianId == technicianId &&
                                     t.WorkDate.Date == date.Date &&
                                     t.SlotId == slotId &&
                                     !t.IsAvailable);
        
        if (timeSlot == null)
            return false;

        timeSlot.IsAvailable = true;
        timeSlot.BookingId = null;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<TechnicianTimeSlot>> GetTechnicianTimeSlotsByTechnicianAndDateAsync(int technicianId, DateTime date)
    {
        return await _context.TechnicianTimeSlots
            .Where(t => t.TechnicianId == technicianId &&
                       t.WorkDate.Date == date.Date)
            .Include(t => t.Slot)
            .Include(t => t.Technician)
            .ThenInclude(x => x.User)
            .ToListAsync();
    }
}
