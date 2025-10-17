using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface ITechnicianService
    {
        Task<TechnicianListResponse> GetAllTechniciansAsync(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, int? centerId = null);
        Task<TechnicianResponse> GetTechnicianByIdAsync(int technicianId);
        Task<TechnicianAvailabilityResponse> GetTechnicianAvailabilityAsync(int technicianId, DateOnly date);
        Task<bool> UpdateTechnicianAvailabilityAsync(int technicianId, UpdateTechnicianAvailabilityRequest request);
        Task<TechnicianBookingsResponse> GetBookingsByDateAsync(int technicianId, DateOnly date);
        Task UpsertSkillsAsync(int technicianId, UpsertTechnicianSkillsRequest request);
        Task RemoveSkillAsync(int technicianId, int skillId);
        Task<List<TechnicianSkillResponse>> GetTechnicianSkillsAsync(int technicianId);
    }
}
