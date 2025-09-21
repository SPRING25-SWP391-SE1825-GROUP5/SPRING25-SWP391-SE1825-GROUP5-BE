using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IWeeklyTimeSlotService
    {
        /// <summary>
        /// Tạo time slot tổng theo tuần cho khách hàng
        /// </summary>
        /// <param name="request">Thông tin tạo weekly time slot</param>
        /// <returns>Kết quả tạo weekly time slot</returns>
        Task<WeeklyTimeSlotSummaryResponse> CreateWeeklyTimeSlotsAsync(CreateWeeklyTimeSlotRequest request);

        /// <summary>
        /// Lấy danh sách weekly time slots theo center
        /// </summary>
        /// <param name="centerId">ID của center</param>
        /// <param name="startDate">Ngày bắt đầu</param>
        /// <param name="endDate">Ngày kết thúc</param>
        /// <returns>Danh sách weekly time slots</returns>
        Task<List<WeeklyTimeSlotResponse>> GetWeeklyTimeSlotsByLocationAsync(int centerId, DateOnly? startDate = null, DateOnly? endDate = null);

        /// <summary>
        /// Lấy danh sách weekly time slots theo technician
        /// </summary>
        /// <param name="technicianId">ID của technician</param>
        /// <param name="startDate">Ngày bắt đầu</param>
        /// <param name="endDate">Ngày kết thúc</param>
        /// <returns>Danh sách weekly time slots</returns>
        Task<List<WeeklyTimeSlotResponse>> GetWeeklyTimeSlotsByTechnicianAsync(int technicianId, DateOnly? startDate = null, DateOnly? endDate = null);

        /// <summary>
        /// Lấy danh sách weekly time slots đang hoạt động
        /// </summary>
        /// <returns>Danh sách weekly time slots đang hoạt động</returns>
        Task<List<WeeklyTimeSlotResponse>> GetActiveWeeklyTimeSlotsAsync();

        /// <summary>
        /// Xóa weekly time slot
        /// </summary>
        /// <param name="weeklyScheduleId">ID của weekly schedule</param>
        /// <returns>Kết quả xóa</returns>
        Task<bool> DeleteWeeklyTimeSlotAsync(int weeklyScheduleId);

        /// <summary>
        /// Cập nhật weekly time slot
        /// </summary>
        /// <param name="weeklyScheduleId">ID của weekly schedule</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Kết quả cập nhật</returns>
        Task<WeeklyTimeSlotResponse> UpdateWeeklyTimeSlotAsync(int weeklyScheduleId, CreateWeeklyTimeSlotRequest request);
    }
}
