using System;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EVServiceCenter.Application.Configurations;

namespace EVServiceCenter.Api.Services
{
    public class ChatHubService : IChatHubService
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ChatSettings _chatSettings;

        public ChatHubService(
            IHubContext<ChatHub> hubContext,
            IOptions<ChatSettings> chatSettings,
            ILogger<ChatHubService> logger)
        {
            _hubContext = hubContext;
            _chatSettings = chatSettings.Value;
            _ = logger;
        }

        public async Task BroadcastMessageToConversationAsync(long conversationId, object messageData)
        {
            var groupName = $"{_chatSettings.SignalR.ConversationGroupPrefix}{conversationId}";
            await _hubContext.Clients.Group(groupName).SendAsync(_chatSettings.SignalR.ReceiveMessageMethod, messageData);
        }

        public async Task NotifyTypingAsync(long conversationId, int? userId, string? guestSessionId, bool isTyping)
        {
            var groupName = $"{_chatSettings.SignalR.ConversationGroupPrefix}{conversationId}";
            var typingData = new
            {
                ConversationId = conversationId,
                UserId = userId,
                GuestSessionId = guestSessionId,
                IsTyping = isTyping,
                Timestamp = System.DateTime.UtcNow
            };

            await _hubContext.Clients.Group(groupName).SendAsync(_chatSettings.SignalR.UserTypingMethod, typingData);
        }

        public async Task NotifyNewConversationAsync(int staffUserId, long conversationId)
        {
            var groupName = $"{_chatSettings.SignalR.UserGroupPrefix}{staffUserId}";
            await _hubContext.Clients.Group(groupName).SendAsync(_chatSettings.SignalR.NewConversationMethod, new
            {
                ConversationId = conversationId,
                Message = _chatSettings.Messages.NewConversationNotification,
                Timestamp = System.DateTime.UtcNow
            });
        }

        public async Task NotifyCenterReassignedAsync(long conversationId, int? oldStaffUserId, int newStaffUserId, int newCenterId, string? reason = null)
        {
            var groupName = $"{_chatSettings.SignalR.ConversationGroupPrefix}{conversationId}";

            await _hubContext.Clients.Group(groupName).SendAsync(_chatSettings.SignalR.CenterReassignedMethod, new
            {
                ConversationId = conversationId,
                NewCenterId = newCenterId,
                NewStaffUserId = newStaffUserId,
                OldStaffUserId = oldStaffUserId,
                Reason = reason,
                Timestamp = System.DateTime.UtcNow
            });

            var newStaffGroup = $"{_chatSettings.SignalR.UserGroupPrefix}{newStaffUserId}";
            await _hubContext.Clients.Group(newStaffGroup).SendAsync(_chatSettings.SignalR.NewConversationMethod, new
            {
                ConversationId = conversationId,
                Message = _chatSettings.Messages.NewAssignmentNotification,
                Timestamp = System.DateTime.UtcNow
            });
        }

        public async Task NotifyMessageReadAsync(long conversationId, int? userId, string? guestSessionId, DateTime lastReadAt)
        {
            var groupName = $"{_chatSettings.SignalR.ConversationGroupPrefix}{conversationId}";
            var readData = new
            {
                ConversationId = conversationId,
                UserId = userId,
                GuestSessionId = guestSessionId,
                LastReadAt = lastReadAt,
                Timestamp = System.DateTime.UtcNow
            };

            await _hubContext.Clients.Group(groupName).SendAsync(_chatSettings.SignalR.MessageReadMethod, readData);
        }
    }
}
