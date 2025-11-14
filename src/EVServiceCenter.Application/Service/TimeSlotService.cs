using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service
{
    public class TimeSlotService : ITimeSlotService
    {
        private readonly ITimeSlotRepository _timeSlotRepository;

        public TimeSlotService(ITimeSlotRepository timeSlotRepository)
        {
            _timeSlotRepository = timeSlotRepository;
        }

        public async Task<List<TimeSlotResponse>> GetAllTimeSlotsAsync()
        {
            try
            {
                var timeSlots = await _timeSlotRepository.GetAllTimeSlotsAsync();
                return timeSlots.Select(ts => MapToTimeSlotResponse(ts)).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách time slots: {ex.Message}");
            }
        }

        public async Task<List<TimeSlotResponse>> GetActiveTimeSlotsAsync()
        {
            try
            {
                var timeSlots = await _timeSlotRepository.GetActiveTimeSlotsAsync();
                return timeSlots.Select(ts => MapToTimeSlotResponse(ts)).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi lấy danh sách time slots đang hoạt động: {ex.Message}");
            }
        }

        public async Task<TimeSlotResponse> CreateTimeSlotAsync(CreateTimeSlotRequest request)
        {
            try
            {
                await ValidateCreateTimeSlotRequestAsync(request);

                var timeSlot = new TimeSlot
                {
                    SlotTime = request.SlotTime,
                    SlotLabel = request.SlotLabel.Trim(),
                    IsActive = request.IsActive
                };

                var createdTimeSlot = await _timeSlotRepository.CreateTimeSlotAsync(timeSlot);

                return MapToTimeSlotResponse(createdTimeSlot);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo time slot: {ex.Message}");
            }
        }

        public async Task<TimeSlotResponse> GetByIdAsync(int slotId)
        {
            var ts = await _timeSlotRepository.GetByIdAsync(slotId);
            if (ts == null) throw new ArgumentException("Time slot không tồn tại");
            return MapToTimeSlotResponse(ts);
        }

        public async Task<TimeSlotResponse> UpdateTimeSlotAsync(int slotId, UpdateTimeSlotRequest request)
        {
            var ts = await _timeSlotRepository.GetByIdAsync(slotId);
            if (ts == null) throw new ArgumentException("Time slot không tồn tại");

            var all = await _timeSlotRepository.GetAllTimeSlotsAsync();
            if (all.Any(x => x.SlotId != slotId && x.SlotTime == request.SlotTime))
                throw new ArgumentException("Thời gian slot này đã tồn tại");
            if (all.Any(x => x.SlotId != slotId && x.SlotLabel.Equals(request.SlotLabel.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Nhãn slot này đã tồn tại");

            ts.SlotTime = request.SlotTime;
            ts.SlotLabel = request.SlotLabel.Trim();
            ts.IsActive = request.IsActive;

            return MapToTimeSlotResponse(await _timeSlotRepository.UpdateAsync(ts));
        }

        public async Task<bool> DeleteTimeSlotAsync(int slotId)
        {
            return await _timeSlotRepository.DeleteAsync(slotId);
        }

        private TimeSlotResponse MapToTimeSlotResponse(TimeSlot timeSlot)
        {
            return new TimeSlotResponse
            {
                SlotId = timeSlot.SlotId,
                SlotTime = timeSlot.SlotTime,
                SlotLabel = timeSlot.SlotLabel,
                IsActive = timeSlot.IsActive
            };
        }

        private async Task ValidateCreateTimeSlotRequestAsync(CreateTimeSlotRequest request)
        {
            var errors = new List<string>();

            var existingTimeSlots = await _timeSlotRepository.GetAllTimeSlotsAsync();
            if (existingTimeSlots.Any(ts => ts.SlotTime == request.SlotTime))
            {
                errors.Add("Thời gian slot này đã tồn tại. Vui lòng chọn thời gian khác.");
            }

            if (existingTimeSlots.Any(ts => ts.SlotLabel.Equals(request.SlotLabel.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add("Nhãn slot này đã tồn tại. Vui lòng chọn nhãn khác.");
            }

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }
    }
}
