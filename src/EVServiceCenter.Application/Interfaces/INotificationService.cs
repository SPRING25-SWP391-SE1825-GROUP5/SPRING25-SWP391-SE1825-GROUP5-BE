using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface INotificationService
    {
        Task SendBookingNotificationAsync(int userId, string title, string message, string type = "BOOKING");
        Task SendTechnicianNotificationAsync(int technicianId, string title, string message, string type = "BOOKING");
        Task SendStaffNotificationAsync(string title, string message, string type = "BOOKING");
        Task<List<NotificationResponse>> GetUserNotificationsAsync(int userId);
        Task MarkNotificationAsReadAsync(int notificationId);
        Task<int> GetUnreadCountAsync(int userId);
    }
}
