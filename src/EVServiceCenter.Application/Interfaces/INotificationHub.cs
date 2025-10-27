using System.Threading.Tasks;

namespace EVServiceCenter.Application.Interfaces
{
    public interface INotificationHub
    {
        Task SendNotificationToUserAsync(string userId, object notificationData);
    }
}

