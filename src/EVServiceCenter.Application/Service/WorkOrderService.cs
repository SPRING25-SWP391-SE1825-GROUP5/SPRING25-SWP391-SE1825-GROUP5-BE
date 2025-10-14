using System;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using System.Collections.Generic;

namespace EVServiceCenter.Application.Service
{
    public class WorkOrderService : IWorkOrderService
    {
        private readonly IWorkOrderRepository _workOrderRepository;
        private readonly ITechnicianRepository _technicianRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IServiceRequiredSkillRepository _requiredSkillRepo;
        private readonly ITechnicianTimeSlotRepository _timeSlotRepo;

        public WorkOrderService(
            IWorkOrderRepository workOrderRepository,
            ITechnicianRepository technicianRepository,
            IBookingRepository bookingRepository,
            IServiceRequiredSkillRepository requiredSkillRepo,
            ITechnicianTimeSlotRepository timeSlotRepo)
        {
            _workOrderRepository = workOrderRepository;
            _technicianRepository = technicianRepository;
            _bookingRepository = bookingRepository;
            _requiredSkillRepo = requiredSkillRepo;
            _timeSlotRepo = timeSlotRepo;
        }

        public async Task<WorkOrder> AssignTechnicianAsync(int workOrderId, int technicianId)
        {
            if (workOrderId <= 0) throw new ArgumentException("workOrderId không hợp lệ");
            if (technicianId <= 0) throw new ArgumentException("technicianId không hợp lệ");

            var wo = await _workOrderRepository.GetByIdAsync(workOrderId);
            if (wo == null) throw new InvalidOperationException("Work order không tồn tại");

            var tech = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
            if (tech == null || !tech.IsActive) throw new InvalidOperationException("Kỹ thuật viên không tồn tại hoặc không hoạt động");

            // Validate center
            if (wo.CenterId.HasValue && tech.CenterId != wo.CenterId.Value)
                throw new InvalidOperationException("Kỹ thuật viên không thuộc trung tâm");

            // Only one technician per work order at a time is implicit by single TechnicianId field

            // Validate skills if service requires
            if (wo.ServiceId.HasValue && wo.ServiceId.Value > 0)
            {
                var reqSkills = await _requiredSkillRepo.GetByServiceIdAsync(wo.ServiceId.Value);
                if (reqSkills != null && reqSkills.Count > 0)
                {
                    var techSkillIds = (tech.TechnicianSkills ?? new System.Collections.Generic.List<TechnicianSkill>()).Select(s => s.SkillId).ToHashSet();
                    var ok = reqSkills.All(rs => techSkillIds.Contains(rs.SkillId));
                    if (!ok)
                        throw new InvalidOperationException("Kỹ thuật viên không đủ kỹ năng cho dịch vụ");
                }
            }

            // Validate time slot availability from booking
            if (wo.BookingId > 0)
            {
                var booking = await _bookingRepository.GetBookingByIdAsync(wo.BookingId);
                if (booking != null && booking.SlotId > 0)
                {
                    try
                    {
                        var date = DateOnly.FromDateTime(booking.CreatedAt);
                        var isFree = await _timeSlotRepo.IsSlotAvailableAsync(tech.TechnicianId, date.ToDateTime(TimeOnly.MinValue), booking.SlotId);
                        if (!isFree) throw new InvalidOperationException("Kỹ thuật viên bận tại slot này");
                    }
                    catch { }
                }
            }

            wo.TechnicianId = tech.TechnicianId;
            wo.Status = "IN_PROGRESS";
            wo.UpdatedAt = DateTime.UtcNow;
            await _workOrderRepository.UpdateAsync(wo);
            return wo;
        }

        public async Task<List<WorkOrder>> GetByTechnicianAsync(int technicianId, DateTime? from, DateTime? to, string status)
        {
            if (technicianId <= 0)
                throw new ArgumentException("Id sai hoặc không tồn tại");

            var tech = await _technicianRepository.GetTechnicianByIdAsync(technicianId);
            if (tech == null)
                throw new ArgumentException("Id sai hoặc không tồn tại");

            return await _workOrderRepository.GetByTechnicianAsync(technicianId, from, to, status);
        }
    }
}

 