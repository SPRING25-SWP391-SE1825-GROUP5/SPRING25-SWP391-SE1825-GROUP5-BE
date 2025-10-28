using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification);
        Task<List<Notification>> GetByUserIdAsync(int userId);
        Task MarkAsReadAsync(int notificationId);
        Task<int> GetUnreadCountAsync(int userId);
        Task<List<User>> GetStaffUsersAsync();
    }
}
