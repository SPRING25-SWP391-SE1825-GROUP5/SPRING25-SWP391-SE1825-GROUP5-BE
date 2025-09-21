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
    public class WeeklyScheduleRepository : IWeeklyScheduleRepository
    {
        private readonly EVDbContext _context;

        public WeeklyScheduleRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<WeeklySchedule>> GetAllWeeklySchedulesAsync()
        {
            return await _context.WeeklySchedules
                .Include(ws => ws.Location)
                .Include(ws => ws.Technician)
                .OrderBy(ws => ws.LocationId)
                .ThenBy(ws => ws.DayOfWeek)
                .ThenBy(ws => ws.StartTime)
                .ToListAsync();
        }

        public async Task<WeeklySchedule?> GetWeeklyScheduleByIdAsync(int weeklyScheduleId)
        {
            return await _context.WeeklySchedules
                .Include(ws => ws.Location)
                .Include(ws => ws.Technician)
                .FirstOrDefaultAsync(ws => ws.WeeklyScheduleId == weeklyScheduleId);
        }

        public async Task<List<WeeklySchedule>> GetWeeklySchedulesByLocationAsync(int centerId)
        {
            return await _context.WeeklySchedules
                .Include(ws => ws.Location)
                .Include(ws => ws.Technician)
                .Where(ws => ws.LocationId == centerId)
                .OrderBy(ws => ws.DayOfWeek)
                .ThenBy(ws => ws.StartTime)
                .ToListAsync();
        }

        public async Task<List<WeeklySchedule>> GetWeeklySchedulesByTechnicianAsync(int technicianId)
        {
            return await _context.WeeklySchedules
                .Include(ws => ws.Location)
                .Include(ws => ws.Technician)
                .Where(ws => ws.TechnicianId == technicianId)
                .OrderBy(ws => ws.DayOfWeek)
                .ThenBy(ws => ws.StartTime)
                .ToListAsync();
        }

        public async Task<List<WeeklySchedule>> GetWeeklySchedulesByLocationAndDayAsync(int centerId, byte dayOfWeek)
        {
            return await _context.WeeklySchedules
                .Include(ws => ws.Location)
                .Include(ws => ws.Technician)
                .Where(ws => ws.LocationId == centerId && ws.DayOfWeek == dayOfWeek)
                .OrderBy(ws => ws.StartTime)
                .ToListAsync();
        }

        public async Task<List<WeeklySchedule>> GetActiveWeeklySchedulesAsync()
        {
            return await _context.WeeklySchedules
                .Include(ws => ws.Location)
                .Include(ws => ws.Technician)
                .Where(ws => ws.IsActive)
                .OrderBy(ws => ws.LocationId)
                .ThenBy(ws => ws.DayOfWeek)
                .ThenBy(ws => ws.StartTime)
                .ToListAsync();
        }

        public async Task<List<WeeklySchedule>> GetWeeklySchedulesByDateRangeAsync(DateOnly fromDate, DateOnly toDate)
        {
            return await _context.WeeklySchedules
                .Include(ws => ws.Location)
                .Include(ws => ws.Technician)
                .Where(ws => ws.EffectiveFrom <= toDate && 
                           (ws.EffectiveTo == null || ws.EffectiveTo >= fromDate))
                .OrderBy(ws => ws.LocationId)
                .ThenBy(ws => ws.DayOfWeek)
                .ThenBy(ws => ws.StartTime)
                .ToListAsync();
        }

        public async Task<WeeklySchedule> CreateWeeklyScheduleAsync(WeeklySchedule weeklySchedule)
        {
            _context.WeeklySchedules.Add(weeklySchedule);
            await _context.SaveChangesAsync();
            return weeklySchedule;
        }

        public async Task<WeeklySchedule> UpdateWeeklyScheduleAsync(WeeklySchedule weeklySchedule)
        {
            _context.WeeklySchedules.Update(weeklySchedule);
            await _context.SaveChangesAsync();
            return weeklySchedule;
        }

        public async Task<bool> DeleteWeeklyScheduleAsync(int weeklyScheduleId)
        {
            var weeklySchedule = await _context.WeeklySchedules
                .FirstOrDefaultAsync(ws => ws.WeeklyScheduleId == weeklyScheduleId);
            
            if (weeklySchedule == null)
                return false;

            _context.WeeklySchedules.Remove(weeklySchedule);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int weeklyScheduleId)
        {
            return await _context.WeeklySchedules
                .AnyAsync(ws => ws.WeeklyScheduleId == weeklyScheduleId);
        }
    }
}

