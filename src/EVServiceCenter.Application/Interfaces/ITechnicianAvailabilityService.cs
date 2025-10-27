using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models;

namespace EVServiceCenter.Application.Interfaces
{
    public interface ITechnicianAvailabilityService
    {
        /// <summary>
        /// Lấy availability của tất cả technician trong center
        /// </summary>
        Task<TechnicianAvailabilityResponse> GetCenterTechniciansAvailabilityAsync(
            int centerId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 30);

        /// <summary>
        /// Lấy availability của 1 technician cụ thể trong center
        /// </summary>
        Task<TechnicianAvailabilityResponse> GetTechnicianAvailabilityAsync(
            int centerId,
            int technicianId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 30);
    }
}
