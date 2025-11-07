using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Linq;

namespace EVServiceCenter.Api
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            this.logger = logger;
        }


        private string? GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst("nameid") ??
                             Context.User?.FindFirst("UserId") ??
                             Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

            return userIdClaim?.Value;
        }


        private string? GetCurrentUserRole()
        {
            return Context.User?.FindFirst("role")?.Value;
        }


        private string? GetCurrentUserEmail()
        {
            return Context.User?.FindFirst("email")?.Value;
        }


        public async Task JoinConversation(long conversationId)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var userEmail = GetCurrentUserEmail();

            var group = $"conversation:{conversationId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, group);

            logger.LogInformation("User {UserId} ({Role}) {ConnectionId} joined conversation {ConversationId}",
                userId, userRole, Context.ConnectionId, conversationId);

            // Notify other users in the conversation that someone joined
            await Clients.OthersInGroup(group).SendAsync("UserJoined", new
            {
                ConversationId = conversationId,
                UserId = userId,
                UserRole = userRole,
                UserEmail = userEmail,
                ConnectionId = Context.ConnectionId,
                Timestamp = System.DateTime.UtcNow
            });
        }

        public async Task LeaveConversation(long conversationId)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var group = $"conversation:{conversationId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);

            logger.LogInformation("User {UserId} ({Role}) {ConnectionId} left conversation {ConversationId}",
                userId, userRole, Context.ConnectionId, conversationId);

            // Notify other users in the conversation that someone left
            await Clients.OthersInGroup(group).SendAsync("UserLeft", new
            {
                ConversationId = conversationId,
                UserId = userId,
                UserRole = userRole,
                ConnectionId = Context.ConnectionId,
                Timestamp = System.DateTime.UtcNow
            });
        }




        /// <summary>
        /// Sends a message to a conversation group (for testing purposes)
        /// Note: Real messages should be sent via MessageController -> MessageService
        /// </summary>
        public async Task SendMessage(long conversationId, string content)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var userEmail = GetCurrentUserEmail();

            var group = $"conversation:{conversationId}";
            await Clients.Group(group).SendAsync("ReceiveMessage", new
            {
                ConversationId = conversationId,
                Content = content,
                SenderUserId = userId,
                SenderRole = userRole,
                SenderEmail = userEmail,
                SenderConnectionId = Context.ConnectionId,
                Timestamp = System.DateTime.UtcNow,
                IsTestMessage = true
            });

            logger.LogInformation("Test message sent to conversation {ConversationId} by user {UserId} ({Role}) {ConnectionId}",
                conversationId, userId, userRole, Context.ConnectionId);
        }

        /// <summary>
        /// Notifies all members in a conversation that a user is typing
        /// </summary>
        public async Task NotifyTyping(long conversationId, bool isTyping, string? userId = null, string? guestSessionId = null)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();
            var currentUserEmail = GetCurrentUserEmail();

            // Use provided userId or fall back to current user
            var senderUserId = userId ?? currentUserId;

            var group = $"conversation:{conversationId}";

            // Log before sending
            logger.LogInformation("Preparing to send typing notification: ConversationId={ConversationId}, UserId={UserId}, IsTyping={IsTyping}, Group={Group}, ConnectionId={ConnectionId}",
                conversationId, senderUserId, isTyping, group, Context.ConnectionId);

            await Clients.OthersInGroup(group).SendAsync("UserTyping", new
            {
                ConversationId = conversationId,
                UserId = senderUserId ?? string.Empty, // Ensure not null
                UserRole = currentUserRole,
                UserEmail = currentUserEmail,
                GuestSessionId = guestSessionId,
                ConnectionId = Context.ConnectionId,
                IsTyping = isTyping,
                Timestamp = System.DateTime.UtcNow
            });

            logger.LogInformation("Typing notification sent to conversation {ConversationId} by user {UserId} ({Role}) {ConnectionId}",
                conversationId, senderUserId, currentUserRole, Context.ConnectionId);
        }

        /// <summary>
        /// Joins a user-specific group for notifications
        /// </summary>
        public async Task JoinUserGroup(string userId)
        {
            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            // Only allow users to join their own group
            if (string.IsNullOrEmpty(userId) || userId != currentUserId)
            {
                return;
            }

            var group = $"user:{userId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
        }

        /// <summary>
        /// Leaves a user-specific group
        /// </summary>
        public async Task LeaveUserGroup(string userId)
        {
            var currentUserId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            var group = $"user:{userId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
        }


        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            var userEmail = GetCurrentUserEmail();

            logger.LogInformation("User {UserId} ({Role}) {ConnectionId} connected",
                userId, userRole, Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();

            logger.LogInformation("User {UserId} ({Role}) {ConnectionId} disconnected. Exception: {Exception}",
                userId, userRole, Context.ConnectionId, exception?.Message);

            await base.OnDisconnectedAsync(exception);
        }
    }
}
