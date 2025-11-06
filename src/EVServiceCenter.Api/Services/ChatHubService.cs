using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EVServiceCenter.Application.Configurations;

namespace EVServiceCenter.Api.Services
{
    /// <summary>
    /// Service implementation for broadcasting messages via SignalR ChatHub
    /// </summary>
    public class ChatHubService : IChatHubService
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ChatSettings _chatSettings;
        private readonly ILogger<ChatHubService> _logger;

        public ChatHubService(
            IHubContext<ChatHub> hubContext,
            IOptions<ChatSettings> chatSettings,
            ILogger<ChatHubService> logger)
        {
            _hubContext = hubContext;
            _chatSettings = chatSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Broadcasts a message to all members of a conversation
        /// </summary>
        public async Task BroadcastMessageToConversationAsync(long conversationId, object messageData)
        {
            try
            {
                var groupName = $"{_chatSettings.SignalR.ConversationGroupPrefix}{conversationId}";
                await _hubContext.Clients.Group(groupName).SendAsync(_chatSettings.SignalR.ReceiveMessageMethod, messageData);

                _logger.LogInformation("Broadcasted message to conversation {ConversationId}", conversationId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting message to conversation {ConversationId}", conversationId);
                throw;
            }
        }

        /// <summary>
        /// Notifies all members in a conversation that a user is typing
        /// </summary>
        public async Task NotifyTypingAsync(long conversationId, int? userId, string? guestSessionId, bool isTyping)
        {
            try
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

                _logger.LogInformation("Broadcasted typing notification to conversation {ConversationId}", conversationId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting typing notification to conversation {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task NotifyNewConversationAsync(int staffUserId, long conversationId)
        {
            try
            {
                var groupName = $"{_chatSettings.SignalR.UserGroupPrefix}{staffUserId}";
                await _hubContext.Clients.Group(groupName).SendAsync(_chatSettings.SignalR.NewConversationMethod, new
                {
                    ConversationId = conversationId,
                    Message = _chatSettings.Messages.NewConversationNotification,
                    Timestamp = System.DateTime.UtcNow
                });

                _logger.LogInformation("Notified staff user {UserId} about new conversation {ConversationId}", staffUserId, conversationId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error notifying staff user {UserId} about new conversation {ConversationId}", staffUserId, conversationId);
                throw;
            }
        }

        public async Task NotifyCenterReassignedAsync(long conversationId, int? oldStaffUserId, int newStaffUserId, int newCenterId, string? reason = null)
        {
            try
            {
                var groupName = $"{_chatSettings.SignalR.ConversationGroupPrefix}{conversationId}";

                // Notify all members in conversation
                await _hubContext.Clients.Group(groupName).SendAsync(_chatSettings.SignalR.CenterReassignedMethod, new
                {
                    ConversationId = conversationId,
                    NewCenterId = newCenterId,
                    NewStaffUserId = newStaffUserId,
                    OldStaffUserId = oldStaffUserId,
                    Reason = reason,
                    Timestamp = System.DateTime.UtcNow
                });

                // Notify new staff
                var newStaffGroup = $"{_chatSettings.SignalR.UserGroupPrefix}{newStaffUserId}";
                await _hubContext.Clients.Group(newStaffGroup).SendAsync(_chatSettings.SignalR.NewConversationMethod, new
                {
                    ConversationId = conversationId,
                    Message = _chatSettings.Messages.NewAssignmentNotification,
                    Timestamp = System.DateTime.UtcNow
                });

                _logger.LogInformation(
                    "Notified conversation {ConversationId} about center reassignment to center {CenterId}",
                    conversationId, newCenterId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error notifying center reassignment for conversation {ConversationId}", conversationId);
                throw;
            }
        }
    }
}

