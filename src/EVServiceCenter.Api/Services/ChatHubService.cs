using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Api.Services
{
    /// <summary>
    /// Service implementation for broadcasting messages via SignalR ChatHub
    /// </summary>
    public class ChatHubService : IChatHubService
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<ChatHubService> _logger;

        public ChatHubService(IHubContext<ChatHub> hubContext, ILogger<ChatHubService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Broadcasts a message to all members of a conversation
        /// </summary>
        public async Task BroadcastMessageToConversationAsync(long conversationId, object messageData)
        {
            try
            {
                var groupName = $"conversation:{conversationId}";
                await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", messageData);
                
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
                var groupName = $"conversation:{conversationId}";
                var typingData = new
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    GuestSessionId = guestSessionId,
                    IsTyping = isTyping,
                    Timestamp = System.DateTime.UtcNow
                };

                await _hubContext.Clients.Group(groupName).SendAsync("UserTyping", typingData);
                
                _logger.LogInformation("Broadcasted typing notification to conversation {ConversationId}", conversationId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting typing notification to conversation {ConversationId}", conversationId);
                throw;
            }
        }
    }
}

