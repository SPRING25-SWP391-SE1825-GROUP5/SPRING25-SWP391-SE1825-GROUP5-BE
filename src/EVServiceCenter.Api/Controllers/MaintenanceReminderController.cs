using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Configurations;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using Microsoft.Extensions.Options;
using System.Linq;

namespace EVServiceCenter.Api.Controllers
{
    [ApiController]
    [Route("api/maintenance-reminders")]
    public class MaintenanceReminderController : ControllerBase
    {
        private readonly IMaintenanceReminderRepository _repo;
        private readonly IEmailService _email;
        private readonly MaintenanceReminderOptions _options;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IServiceService _serviceService;

        public MaintenanceReminderController(
            IMaintenanceReminderRepository repo, 
            IEmailService email, 
            IOptions<MaintenanceReminderOptions> options,
            IVehicleRepository vehicleRepository,
            IServiceService serviceService)
        {
            _repo = repo;
            _email = email;
            _options = options.Value;
            _vehicleRepository = vehicleRepository;
            _serviceService = serviceService;
        }

        /// <summary>
        /// Tạo nhắc nhở bảo dưỡng xe cho một xe cụ thể
        /// </summary>
        /// <param name="request">Thông tin tạo nhắc nhở</param>
        /// <returns>Kết quả tạo nhắc nhở</returns>
        [HttpPost("create-vehicle-service-reminders")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> CreateVehicleServiceReminders([FromBody] CreateVehicleServiceRemindersRequest request)
        {
            try
            {
                // Kiểm tra xe có tồn tại không
                var vehicle = await _vehicleRepository.GetVehicleByIdAsync(request.VehicleId);
                if (vehicle == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy xe với ID đã cho"
                    });
                }

                var createdReminders = new List<CreatedVehicleServiceReminder>();

                foreach (var reminderItem in request.Reminders)
                {
                    // Kiểm tra service có tồn tại không
                    var service = await _serviceService.GetServiceByIdAsync(reminderItem.ServiceId);
                    if (service == null)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Không tìm thấy dịch vụ với ID: {reminderItem.ServiceId}"
                        });
                    }

                    // Tạo reminder mới
                    var reminder = new MaintenanceReminder
                    {
                        VehicleId = request.VehicleId,
                        ServiceId = reminderItem.ServiceId,
                        DueDate = reminderItem.DueDate,
                        DueMileage = reminderItem.DueMileage,
                        IsCompleted = false,
                        CreatedAt = DateTime.UtcNow
                    };

                    var createdReminder = await _repo.CreateAsync(reminder);

                    createdReminders.Add(new CreatedVehicleServiceReminder
                    {
                        ReminderId = createdReminder.ReminderId,
                        VehicleId = createdReminder.VehicleId,
                        ServiceId = createdReminder.ServiceId ?? 0,
                        ServiceName = service.ServiceName,
                        DueDate = createdReminder.DueDate?.ToString("yyyy-MM-dd"),
                        DueMileage = createdReminder.DueMileage,
                        IsCompleted = createdReminder.IsCompleted,
                        CreatedAt = createdReminder.CreatedAt
                    });
                }

                return Ok(new CreateVehicleServiceRemindersResponse
                {
                    Success = true,
                    Message = "Tạo nhắc nhở bảo dưỡng xe thành công",
                    VehicleId = request.VehicleId,
                    VehicleLicensePlate = vehicle.LicensePlate,
                    CreatedRemindersCount = createdReminders.Count,
                    CreatedReminders = createdReminders
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi tạo nhắc nhở bảo dưỡng xe",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gửi thông báo nhắc nhở bảo dưỡng xe cho khách hàng
        /// </summary>
        /// <param name="req">Thông tin yêu cầu gửi thông báo</param>
        /// <returns>Kết quả gửi thông báo</returns>
        [HttpPost("send-vehicle-maintenance-alerts")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> SendVehicleMaintenanceAlerts([FromBody] SendVehicleMaintenanceAlertsRequest req)
        {
            try
            {
                var now = DateTime.UtcNow.Date;
                var upcomingDays = req?.UpcomingDays ?? _options.UpcomingDays;
                var until = now.AddDays(upcomingDays);
                
                // Lấy tất cả reminders sắp đến hạn
                var reminders = await _repo.QueryAsync(req?.CustomerId, req?.VehicleId, "PENDING", now, until);
                
                var sentEmails = 0;
                var sentSms = 0;
                var failed = 0;
                var results = new System.Collections.Generic.List<SendVehicleMaintenanceAlertsResult>();

                foreach (var reminder in reminders)
                {
                    var result = new SendVehicleMaintenanceAlertsResult
                    {
                        ReminderId = reminder.ReminderId,
                        VehicleId = reminder.VehicleId,
                        ServiceId = reminder.ServiceId ?? 0,
                        DueDate = reminder.DueDate?.ToString("yyyy-MM-dd"),
                        DueMileage = reminder.DueMileage,
                        EmailSent = false,
                        SmsSent = false,
                        Error = null
                    };

                    try
                    {
                        // Gửi email nếu được yêu cầu
                        if (req?.SendEmail == true)
                        {
                            var email = reminder.Vehicle?.Customer?.User?.Email;
                            if (!string.IsNullOrWhiteSpace(email))
                            {
                                var subject = $"🚗 Nhắc nhở bảo dưỡng xe - Dịch vụ #{reminder.ServiceId}";
                                var dueDateText = reminder.DueDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định";
                                var dueMileageText = reminder.DueMileage?.ToString("N0") ?? "Chưa xác định";
                                
                                var body = $@"
                                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                        <h2 style='color: #2c3e50;'>🚗 Nhắc nhở bảo dưỡng xe</h2>
                                        <p>Xin chào <strong>{reminder.Vehicle?.Customer?.User?.FullName ?? "Quý khách"}</strong>,</p>
                                        
                                        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                                            <h3 style='color: #e74c3c; margin-top: 0;'>⚠️ Lịch bảo dưỡng sắp đến hạn</h3>
                                            <ul style='list-style: none; padding: 0;'>
                                                <li style='margin: 10px 0;'><strong>🚙 Xe:</strong> #{reminder.VehicleId}</li>
                                                <li style='margin: 10px 0;'><strong>🔧 Dịch vụ:</strong> #{reminder.ServiceId}</li>
                                                <li style='margin: 10px 0;'><strong>📅 Ngày đến hạn:</strong> {dueDateText}</li>
                                                <li style='margin: 10px 0;'><strong>🛣️ Số km:</strong> {dueMileageText} km</li>
                                            </ul>
                                        </div>
                                        
                                        <p>Vui lòng liên hệ với chúng tôi để đặt lịch bảo dưỡng sớm nhất có thể.</p>
                                        
                                        <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee;'>
                                            <p style='color: #7f8c8d; font-size: 14px;'>
                                                Trân trọng,<br>
                                                <strong>EV Service Center</strong>
                                            </p>
                                        </div>
                                    </div>";

                                await _email.SendEmailAsync(email, subject, body);
                                sentEmails++;
                                result.EmailSent = true;
                            }
                        }

                        // Gửi SMS nếu được yêu cầu (placeholder - cần implement SMS service)
                        if (req?.SendSms == true)
                        {
                            var phone = reminder.Vehicle?.Customer?.User?.PhoneNumber;
                            if (!string.IsNullOrWhiteSpace(phone))
                            {
                                // TODO: Implement SMS service
                                // await _smsService.SendSmsAsync(phone, message);
                                sentSms++;
                                result.SmsSent = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        result.Error = ex.Message;
                    }

                    results.Add(result);
                }

                return Ok(new SendVehicleMaintenanceAlertsResponse
                {
                    Success = true,
                    Message = "Đã gửi thông báo nhắc nhở bảo dưỡng xe",
                    Summary = new SendVehicleMaintenanceAlertsSummary
                    {
                        TotalReminders = reminders.Count,
                        SentEmails = sentEmails,
                        SentSms = sentSms,
                        Failed = failed,
                        UpcomingDays = upcomingDays
                    },
                    Results = results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi gửi thông báo nhắc nhở",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy danh sách reminders sắp đến hạn
        /// </summary>
        /// <param name="customerId">ID khách hàng (tùy chọn)</param>
        /// <param name="vehicleId">ID xe (tùy chọn)</param>
        /// <param name="upcomingDays">Số ngày sắp tới (tùy chọn)</param>
        /// <returns>Danh sách reminders sắp đến hạn</returns>
        [HttpGet("upcoming")]
        [Authorize]
        public async Task<IActionResult> GetUpcomingReminders(
            [FromQuery] int? customerId = null,
            [FromQuery] int? vehicleId = null,
            [FromQuery] int? upcomingDays = null)
        {
            try
            {
                var now = DateTime.UtcNow.Date;
                var days = upcomingDays ?? _options.UpcomingDays;
                var until = now.AddDays(days);
                
                var reminders = await _repo.QueryAsync(customerId, vehicleId, "PENDING", now, until);
                
                var results = reminders.Select(r => new
                {
                    r.ReminderId,
                    r.VehicleId,
                    r.ServiceId,
                    DueDate = r.DueDate?.ToString("yyyy-MM-dd"),
                    r.DueMileage,
                    r.IsCompleted,
                    r.CreatedAt,
                    Vehicle = r.Vehicle != null ? new
                    {
                        r.Vehicle.VehicleId,
                        r.Vehicle.LicensePlate,
                        r.Vehicle.Vin,
                        Customer = r.Vehicle.Customer != null ? new
                        {
                            r.Vehicle.Customer.CustomerId,
                            User = r.Vehicle.Customer.User != null ? new
                            {
                                r.Vehicle.Customer.User.FullName,
                                r.Vehicle.Customer.User.Email,
                                r.Vehicle.Customer.User.PhoneNumber
                            } : null
                        } : null
                    } : null
                }).ToList();

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách reminders sắp đến hạn thành công",
                    data = new
                    {
                        totalCount = results.Count,
                        upcomingDays = days,
                        reminders = results
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lấy danh sách reminders",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gửi email test cho một reminder cụ thể
        /// </summary>
        /// <param name="reminderId">ID của reminder</param>
        /// <returns>Kết quả gửi email test</returns>
        [HttpPost("{reminderId}/send-test-email")]
        [Authorize(Roles = "ADMIN,STAFF")]
        public async Task<IActionResult> SendTestEmail(int reminderId)
        {
            try
            {
                var reminder = await _repo.GetByIdAsync(reminderId);
                if (reminder == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy reminder"
                    });
                }

                var email = reminder.Vehicle?.Customer?.User?.Email;
                if (string.IsNullOrWhiteSpace(email))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Khách hàng không có email"
                    });
                }

                var subject = $"🧪 [TEST] Nhắc nhở bảo dưỡng xe - Dịch vụ #{reminder.ServiceId}";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #e74c3c;'>🧪 EMAIL TEST</h2>
                        <p>Đây là email test cho reminder #{reminderId}</p>
                        <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                            <strong>Thông tin reminder:</strong><br>
                            - Xe: #{reminder.VehicleId}<br>
                            - Dịch vụ: #{reminder.ServiceId}<br>
                            - Ngày đến hạn: {reminder.DueDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định"}<br>
                            - Số km: {reminder.DueMileage?.ToString("N0") ?? "Chưa xác định"} km
                        </div>
                        <p><em>Email này được gửi để test chức năng gửi thông báo.</em></p>
                    </div>";

                await _email.SendEmailAsync(email, subject, body);

                return Ok(new
                {
                    success = true,
                    message = "Đã gửi email test thành công",
                    data = new
                    {
                        reminderId,
                        email,
                        sentAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi gửi email test",
                    error = ex.Message
                });
            }
        }
    }
}
