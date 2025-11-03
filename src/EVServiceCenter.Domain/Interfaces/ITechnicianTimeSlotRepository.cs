using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces;

public interface ITechnicianTimeSlotRepository
{
    Task<TechnicianTimeSlot?> GetByIdAsync(int id);
    Task<List<TechnicianTimeSlot>> GetByTechnicianIdAsync(int technicianId);
    Task<List<TechnicianTimeSlot>> GetByDateAsync(DateTime date);
    Task<List<TechnicianTimeSlot>> GetByTechnicianAndDateRangeAsync(int technicianId, DateTime startDate, DateTime endDate);
    Task<TechnicianTimeSlot> CreateAsync(TechnicianTimeSlot technicianTimeSlot);
    Task<TechnicianTimeSlot> UpdateAsync(TechnicianTimeSlot technicianTimeSlot);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<List<TechnicianTimeSlot>> GetAvailableSlotsAsync(DateTime date, int slotId);
    Task<bool> IsSlotAvailableAsync(int technicianId, DateTime date, int slotId);
    Task<bool> TechnicianTimeSlotExistsAsync(int technicianId, DateTime date, int slotId);
    Task<bool> ReserveSlotAsync(int technicianId, DateTime date, int slotId, int? bookingId);
    Task<bool> ReleaseSlotAsync(int technicianId, DateTime date, int slotId);
    Task<bool> UpdateSlotBookingIdAsync(int technicianId, DateTime date, int slotId, int bookingId);
    Task<List<TechnicianTimeSlot>> GetTechnicianTimeSlotsByTechnicianAndDateAsync(int technicianId, DateTime date);
    Task<List<TechnicianTimeSlot>> GetExpiredAvailableSlotsAsync(DateOnly workDate, TimeOnly currentTime);
    Task<bool> IsSlotTrulyAvailableAsync(int technicianId, DateTime date, int slotId);
    Task<List<TechnicianTimeSlot>> GetByTechnicianAndDateAsync(int technicianId, DateTime date);
    
    /// <summary>
    /// Lấy tất cả TechnicianTimeSlot của center trong khoảng thời gian (theo WorkDate)
    /// Include Booking để kiểm tra trạng thái booking
    /// </summary>
    /// <param name="centerId">ID trung tâm</param>
    /// <param name="startDate">Ngày bắt đầu</param>
    /// <param name="endDate">Ngày kết thúc</param>
    /// <returns>Danh sách TechnicianTimeSlot với Booking đã include</returns>
    Task<List<TechnicianTimeSlot>> GetByCenterAndDateRangeAsync(int centerId, DateTime startDate, DateTime endDate);
}
