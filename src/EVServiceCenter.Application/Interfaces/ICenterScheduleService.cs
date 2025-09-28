using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface ICenterScheduleService
    {
        /// <summary>
        /// Tạo center schedule mới
        /// </summary>
        /// <param name="request">Thông tin tạo center schedule</param>
        /// <returns>Kết quả tạo center schedule</returns>
        Task<CenterScheduleResponse> CreateCenterScheduleAsync(CreateCenterScheduleRequest request);

        /// <summary>
        /// Tạo lịch làm việc cho cả tuần (từ thứ 2 đến thứ 7)
        /// </summary>
        /// <param name="request">Thông tin tạo lịch cả tuần</param>
        /// <returns>Kết quả tạo lịch cả tuần</returns>
        Task<CreateWeeklyCenterScheduleResponse> CreateWeeklyCenterScheduleAsync(CreateWeeklyCenterScheduleRequest request);

        /// <summary>
        /// Tạo lịch làm việc cho tất cả trung tâm trong 1 tuần (từ thứ 2 đến thứ 7)
        /// </summary>
        /// <param name="request">Thông tin tạo lịch cho tất cả trung tâm</param>
        /// <returns>Kết quả tạo lịch cho tất cả trung tâm</returns>
        Task<CreateAllCentersScheduleResponse> CreateAllCentersScheduleAsync(CreateAllCentersScheduleRequest request);

        /// <summary>
        /// Lấy danh sách center schedules theo center
        /// </summary>
        /// <param name="centerId">ID của center</param>
        /// <param name="dayOfWeek">Ngày trong tuần (0-6)</param>
        /// <returns>Danh sách center schedules</returns>
        Task<List<CenterScheduleResponse>> GetCenterSchedulesByCenterAsync(int centerId, byte? dayOfWeek = null);

        /// <summary>
        /// Lấy danh sách center schedules đang hoạt động
        /// </summary>
        /// <returns>Danh sách center schedules đang hoạt động</returns>
        Task<List<CenterScheduleResponse>> GetActiveCenterSchedulesAsync();

        /// <summary>
        /// Lấy center schedule theo ID
        /// </summary>
        /// <param name="centerScheduleId">ID của center schedule</param>
        /// <returns>Thông tin center schedule</returns>
        Task<CenterScheduleResponse> GetCenterScheduleByIdAsync(int centerScheduleId);

        /// <summary>
        /// Lấy danh sách center schedules có sẵn trong khoảng thời gian
        /// </summary>
        /// <param name="centerId">ID của center</param>
        /// <param name="dayOfWeek">Ngày trong tuần</param>
        /// <param name="startTime">Thời gian bắt đầu</param>
        /// <param name="endTime">Thời gian kết thúc</param>
        /// <returns>Danh sách center schedules có sẵn</returns>
        Task<List<CenterScheduleResponse>> GetAvailableSchedulesAsync(int centerId, byte dayOfWeek, TimeOnly startTime, TimeOnly endTime);

        /// <summary>
        /// Cập nhật center schedule
        /// </summary>
        /// <param name="centerScheduleId">ID của center schedule</param>
        /// <param name="request">Thông tin cập nhật</param>
        /// <returns>Kết quả cập nhật</returns>
        Task<CenterScheduleResponse> UpdateCenterScheduleAsync(int centerScheduleId, UpdateCenterScheduleRequest request);

        /// <summary>
        /// Xóa center schedule
        /// </summary>
        /// <param name="centerScheduleId">ID của center schedule</param>
        /// <returns>Kết quả xóa</returns>
        Task<bool> DeleteCenterScheduleAsync(int centerScheduleId);

        /// <summary>
        /// Deactivate hoặc Reactivate schedule theo khoảng thời gian
        /// </summary>
        /// <param name="request">Thông tin deactivate/reactivate</param>
        /// <returns>Kết quả deactivate/reactivate</returns>
        Task<DeactivateScheduleResponse> DeactivateScheduleAsync(DeactivateScheduleRequest request);

    }
}
