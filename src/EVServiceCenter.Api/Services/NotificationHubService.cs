using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EVServiceCenter.Api.Services
{
    public class NotificationHubService : INotificationHub
    {
        private readonly IHubContext<BookingHub> _hubContext;

        public NotificationHubService(IHubContext<BookingHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNotificationToUserAsync(string userId, object notificationData)
        {
            await _hubContext.Clients.Group($"user:{userId}").SendAsync("ReceiveNotification", notificationData);
        }
    }
}

