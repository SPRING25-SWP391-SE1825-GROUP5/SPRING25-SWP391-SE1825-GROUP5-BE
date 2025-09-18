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
                // Validate request
                await ValidateCreateTimeSlotRequestAsync(request);

                // Create time slot entity
                var timeSlot = new TimeSlot
                {
                    SlotTime = request.SlotTime,
                    SlotLabel = request.SlotLabel.Trim(),
                    IsActive = request.IsActive
                };

                // Save time slot
                var createdTimeSlot = await _timeSlotRepository.CreateTimeSlotAsync(timeSlot);

                return MapToTimeSlotResponse(createdTimeSlot);
            }
            catch (ArgumentException)
            {
                throw; // Rethrow validation errors
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi tạo time slot: {ex.Message}");
            }
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

            // Check for duplicate slot time
            var existingTimeSlots = await _timeSlotRepository.GetAllTimeSlotsAsync();
            if (existingTimeSlots.Any(ts => ts.SlotTime == request.SlotTime))
            {
                errors.Add("Thời gian slot này đã tồn tại. Vui lòng chọn thời gian khác.");
            }

            // Check for duplicate slot label
            if (existingTimeSlots.Any(ts => ts.SlotLabel.Equals(request.SlotLabel.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add("Nhãn slot này đã tồn tại. Vui lòng chọn nhãn khác.");
            }

            if (errors.Any())
                throw new ArgumentException(string.Join(" ", errors));
        }
    }
}
