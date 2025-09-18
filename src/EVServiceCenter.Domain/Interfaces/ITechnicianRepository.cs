using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface ITechnicianRepository
    {
        Task<List<Technician>> GetAllTechniciansAsync();
        Task<Technician> GetTechnicianByIdAsync(int technicianId);
        Task<List<TechnicianTimeSlot>> GetTechnicianAvailabilityAsync(int technicianId, DateOnly date);
        Task UpdateTechnicianAvailabilityAsync(List<TechnicianTimeSlot> timeSlots);
        Task<TechnicianTimeSlot> GetTechnicianTimeSlotAsync(int technicianId, DateOnly date, int slotId);
    }
}
