using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface ICenterScheduleRepository
    {
        Task<List<CenterSchedule>> GetAllCenterSchedulesAsync();
        Task<CenterSchedule?> GetCenterScheduleByIdAsync(int centerScheduleId);
        Task<List<CenterSchedule>> GetCenterSchedulesByCenterAsync(int centerId);
        Task<List<CenterSchedule>> GetCenterSchedulesByCenterAndDayAsync(int centerId, byte dayOfWeek);
        Task<List<CenterSchedule>> GetActiveCenterSchedulesAsync();
        Task<List<CenterSchedule>> GetCenterSchedulesByDateRangeAsync(DateOnly fromDate, DateOnly toDate);
        Task<CenterSchedule> CreateCenterScheduleAsync(CenterSchedule centerSchedule);
        Task<CenterSchedule> UpdateCenterScheduleAsync(CenterSchedule centerSchedule);
        Task<bool> DeleteCenterScheduleAsync(int centerScheduleId);
        Task<bool> ExistsAsync(int centerScheduleId);
        Task<List<CenterSchedule>> GetAvailableSchedulesAsync(int centerId, byte dayOfWeek, TimeOnly startTime, TimeOnly endTime);
        Task<List<CenterSchedule>> GetSchedulesByCenterDayAndTimeAsync(int centerId, byte dayOfWeek, TimeOnly startTime, TimeOnly endTime);
        Task<bool> UpdateScheduleStatusAsync(List<int> scheduleIds, bool isActive);
    }
}
