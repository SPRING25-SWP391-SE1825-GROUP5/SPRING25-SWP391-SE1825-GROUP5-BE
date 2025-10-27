using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;

namespace EVServiceCenter.Application.Service
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly INotificationHub? _notificationHub;

        public NotificationService(INotificationRepository notificationRepository, INotificationHub? notificationHub = null)
        {
            _notificationRepository = notificationRepository;
            _notificationHub = notificationHub;
        }

        public async Task SendBookingNotificationAsync(int userId, string title, string message, string type = "BOOKING")
        {
            // Lưu thông báo vào database
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                CreatedAt = DateTime.UtcNow,
                ReadAt = null
            };
            await _notificationRepository.AddAsync(notification);
            
            // Gửi SignalR notification realtime (nếu có hub)
            if (_notificationHub != null)
            {
                var notificationData = new
                {
                    notificationId = notification.NotificationId,
                    userId = notification.UserId,
                    title = notification.Title,
                    message = notification.Message,
                    createdAt = notification.CreatedAt,
                    readAt = notification.ReadAt,
                    type = type,
                    status = "NEW"
                };
                
                await _notificationHub.SendNotificationToUserAsync(userId.ToString(), notificationData);
            }
        }

        public async Task SendTechnicianNotificationAsync(int technicianId, string title, string message, string type = "BOOKING")
        {
            // Gửi thông báo cho kỹ thuật viên
            await SendBookingNotificationAsync(technicianId, title, message, type);
        }

        public async Task SendStaffNotificationAsync(string title, string message, string type = "BOOKING")
        {
            // Lấy tất cả staff/admin users để gửi notification
            var staffUsers = await _notificationRepository.GetStaffUsersAsync();
            
            foreach (var staffUser in staffUsers)
            {
                await SendBookingNotificationAsync(staffUser.UserId, title, message, type);
            }
        }

        public async Task<List<NotificationResponse>> GetUserNotificationsAsync(int userId)
        {
            var notifications = await _notificationRepository.GetByUserIdAsync(userId);
            return notifications.Select(n => new NotificationResponse
            {
                NotificationId = n.NotificationId,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt,
                Type = "BOOKING", // Default type
                Status = n.ReadAt == null ? "NEW" : "READ"
            }).ToList();
        }

        public async Task MarkNotificationAsReadAsync(int notificationId)
        {
            await _notificationRepository.MarkAsReadAsync(notificationId);
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _notificationRepository.GetUnreadCountAsync(userId);
        }
    }
}
