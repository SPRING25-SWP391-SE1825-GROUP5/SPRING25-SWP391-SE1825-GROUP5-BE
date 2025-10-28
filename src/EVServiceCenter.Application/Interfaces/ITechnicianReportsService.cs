using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface ITechnicianReportsService
    {
        Task<TechnicianPerformanceResponse> GetTechnicianPerformanceAsync(int centerId, string period = "month");
        Task<TechnicianScheduleResponse> GetTechnicianScheduleAsync(int centerId, DateTime date);
    }
}
