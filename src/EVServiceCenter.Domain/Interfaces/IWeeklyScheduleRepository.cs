using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IWeeklyScheduleRepository
    {
        Task<List<WeeklySchedule>> GetAllWeeklySchedulesAsync();
        Task<WeeklySchedule?> GetWeeklyScheduleByIdAsync(int weeklyScheduleId);
        Task<List<WeeklySchedule>> GetWeeklySchedulesByLocationAsync(int centerId);
        Task<List<WeeklySchedule>> GetWeeklySchedulesByTechnicianAsync(int technicianId);
        Task<List<WeeklySchedule>> GetWeeklySchedulesByLocationAndDayAsync(int centerId, byte dayOfWeek);
        Task<List<WeeklySchedule>> GetActiveWeeklySchedulesAsync();
        Task<List<WeeklySchedule>> GetWeeklySchedulesByDateRangeAsync(DateOnly fromDate, DateOnly toDate);
        Task<WeeklySchedule> CreateWeeklyScheduleAsync(WeeklySchedule weeklySchedule);
        Task<WeeklySchedule> UpdateWeeklyScheduleAsync(WeeklySchedule weeklySchedule);
        Task<bool> DeleteWeeklyScheduleAsync(int weeklyScheduleId);
        Task<bool> ExistsAsync(int weeklyScheduleId);
    }
}

