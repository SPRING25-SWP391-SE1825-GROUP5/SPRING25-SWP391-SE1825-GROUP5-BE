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
    public class CenterScheduleRepository : ICenterScheduleRepository
    {
        private readonly EVDbContext _context;

        public CenterScheduleRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<CenterSchedule>> GetAllCenterSchedulesAsync()
        {
            return await _context.CenterSchedules
                .Include(cs => cs.Center)
                .OrderBy(cs => cs.CenterId)
                .ThenBy(cs => cs.DayOfWeek)
                .ThenBy(cs => cs.StartTime)
                .ToListAsync();
        }

        public async Task<CenterSchedule?> GetCenterScheduleByIdAsync(int centerScheduleId)
        {
            return await _context.CenterSchedules
                .Include(cs => cs.Center)
                .FirstOrDefaultAsync(cs => cs.CenterScheduleId == centerScheduleId);
        }

        public async Task<List<CenterSchedule>> GetCenterSchedulesByCenterAsync(int centerId)
        {
            return await _context.CenterSchedules
                .Include(cs => cs.Center)
                .Where(cs => cs.CenterId == centerId)
                .OrderBy(cs => cs.DayOfWeek)
                .ThenBy(cs => cs.StartTime)
                .ToListAsync();
        }

        public async Task<List<CenterSchedule>> GetCenterSchedulesByCenterAndDayAsync(int centerId, byte dayOfWeek)
        {
            return await _context.CenterSchedules
                .Include(cs => cs.Center)
                .Where(cs => cs.CenterId == centerId && cs.DayOfWeek == dayOfWeek)
                .OrderBy(cs => cs.StartTime)
                .ToListAsync();
        }

        public async Task<List<CenterSchedule>> GetActiveCenterSchedulesAsync()
        {
            return await _context.CenterSchedules
                .Include(cs => cs.Center)
                .Where(cs => cs.IsActive)
                .OrderBy(cs => cs.CenterId)
                .ThenBy(cs => cs.DayOfWeek)
                .ThenBy(cs => cs.StartTime)
                .ToListAsync();
        }

        public async Task<List<CenterSchedule>> GetCenterSchedulesByDateRangeAsync(DateOnly fromDate, DateOnly toDate)
        {
            return await _context.CenterSchedules
                .Include(cs => cs.Center)
                .OrderBy(cs => cs.CenterId)
                .ThenBy(cs => cs.DayOfWeek)
                .ThenBy(cs => cs.StartTime)
                .ToListAsync();
        }

        public async Task<List<CenterSchedule>> GetAvailableSchedulesAsync(int centerId, byte dayOfWeek, TimeOnly startTime, TimeOnly endTime)
        {
            return await _context.CenterSchedules
                .Include(cs => cs.Center)
                .Where(cs => cs.CenterId == centerId && 
                           cs.DayOfWeek == dayOfWeek &&
                           cs.IsActive &&
                           cs.StartTime <= startTime &&
                           cs.EndTime >= endTime)
                .OrderBy(cs => cs.StartTime)
                .ToListAsync();
        }

        public async Task<CenterSchedule> CreateCenterScheduleAsync(CenterSchedule centerSchedule)
        {
            _context.CenterSchedules.Add(centerSchedule);
            await _context.SaveChangesAsync();
            return centerSchedule;
        }

        public async Task<CenterSchedule> UpdateCenterScheduleAsync(CenterSchedule centerSchedule)
        {
            _context.CenterSchedules.Update(centerSchedule);
            await _context.SaveChangesAsync();
            return centerSchedule;
        }

        public async Task<bool> DeleteCenterScheduleAsync(int centerScheduleId)
        {
            var centerSchedule = await _context.CenterSchedules
                .FirstOrDefaultAsync(cs => cs.CenterScheduleId == centerScheduleId);
            
            if (centerSchedule == null)
                return false;

            _context.CenterSchedules.Remove(centerSchedule);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int centerScheduleId)
        {
            return await _context.CenterSchedules
                .AnyAsync(cs => cs.CenterScheduleId == centerScheduleId);
        }

        public async Task<List<CenterSchedule>> GetSchedulesByCenterDayAndTimeAsync(int centerId, byte dayOfWeek, TimeOnly startTime, TimeOnly endTime)
        {
            return await _context.CenterSchedules
                .Include(cs => cs.Center)
                .Where(cs => cs.CenterId == centerId && 
                           cs.DayOfWeek == dayOfWeek &&
                           cs.StartTime == startTime && 
                           cs.EndTime == endTime)
                .ToListAsync();
        }

        public async Task<bool> UpdateScheduleStatusAsync(List<int> scheduleIds, bool isActive)
        {
            var schedules = await _context.CenterSchedules
                .Where(cs => scheduleIds.Contains(cs.CenterScheduleId))
                .ToListAsync();

            foreach (var schedule in schedules)
            {
                schedule.IsActive = isActive;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
