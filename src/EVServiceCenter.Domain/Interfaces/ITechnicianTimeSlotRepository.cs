using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface ITechnicianTimeSlotRepository
    {
        Task<List<TechnicianTimeSlot>> GetAllTechnicianTimeSlotsAsync();
        Task<TechnicianTimeSlot> GetTechnicianTimeSlotByIdAsync(int technicianSlotId);
        Task<List<TechnicianTimeSlot>> GetTechnicianTimeSlotsByTechnicianAsync(int technicianId);
        Task<List<TechnicianTimeSlot>> GetTechnicianTimeSlotsByDateAsync(DateOnly date);
        Task<List<TechnicianTimeSlot>> GetTechnicianTimeSlotsByTechnicianAndDateAsync(int technicianId, DateOnly date);
        Task<List<TechnicianTimeSlot>> GetAvailableTechnicianTimeSlotsAsync(int technicianId, DateOnly date);
        Task<TechnicianTimeSlot> CreateTechnicianTimeSlotAsync(TechnicianTimeSlot technicianTimeSlot);
        Task<TechnicianTimeSlot> UpdateTechnicianTimeSlotAsync(TechnicianTimeSlot technicianTimeSlot);
        Task<bool> DeleteTechnicianTimeSlotAsync(int technicianSlotId);
        Task<bool> ExistsAsync(int technicianSlotId);
        Task<bool> IsSlotAvailableAsync(int technicianId, DateOnly date, int slotId);
        Task<bool> ReserveSlotAsync(int technicianId, DateOnly date, int slotId, int? bookingId = null);
        Task<bool> ReleaseSlotAsync(int technicianId, DateOnly date, int slotId);
    }
}
