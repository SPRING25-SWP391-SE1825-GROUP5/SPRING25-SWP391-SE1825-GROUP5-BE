using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces;

public interface ITechnicianTimeSlotService
{
    Task<CreateTechnicianTimeSlotResponse> CreateTechnicianTimeSlotAsync(CreateTechnicianTimeSlotRequest request);
    Task<CreateWeeklyTechnicianTimeSlotResponse> CreateWeeklyTechnicianTimeSlotAsync(CreateWeeklyTechnicianTimeSlotRequest request);
    Task<CreateAllTechniciansTimeSlotResponse> CreateAllTechniciansTimeSlotAsync(CreateAllTechniciansTimeSlotRequest request);
    Task<CreateAllTechniciansWeeklyTimeSlotResponse> CreateAllTechniciansWeeklyTimeSlotAsync(CreateAllTechniciansWeeklyTimeSlotRequest request);
    Task<TechnicianTimeSlotResponse> GetTechnicianTimeSlotByIdAsync(int id);
    Task<List<TechnicianTimeSlotResponse>> GetTechnicianTimeSlotsByTechnicianIdAsync(int technicianId);
    Task<List<TechnicianTimeSlotResponse>> GetTechnicianTimeSlotsByDateAsync(DateTime date);
    Task<TechnicianTimeSlotResponse> UpdateTechnicianTimeSlotAsync(int id, UpdateTechnicianTimeSlotRequest request);
    Task<bool> DeleteTechnicianTimeSlotAsync(int id);
    Task<List<TechnicianAvailabilityResponse>> GetTechnicianAvailabilityAsync(int centerId, DateTime startDate, DateTime endDate);
    Task<List<TechnicianTimeSlotResponse>> GetTechnicianScheduleAsync(int technicianId, DateTime startDate, DateTime endDate);
    Task<List<TechnicianTimeSlotResponse>> GetCenterTechnicianScheduleAsync(int centerId, DateTime startDate, DateTime endDate);
}
