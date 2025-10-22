using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Api
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            this.logger = logger;
        }

        
        public async Task JoinConversation(long conversationId)
        {
            var group = $"conversation:{conversationId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
            logger.LogInformation("User {ConnectionId} joined conversation {ConversationId}", Context.ConnectionId, conversationId);
        }

                public async Task LeaveConversation(long conversationId)
        {
            var group = $"conversation:{conversationId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
            logger.LogInformation("User {ConnectionId} left conversation {ConversationId}", Context.ConnectionId, conversationId);
        }

        
        public async Task Typing(long conversationId, bool isTyping)
        {
            var group = $"conversation:{conversationId}";
            await Clients.OthersInGroup(group).SendAsync("UserTyping", Context.ConnectionId, isTyping);
        }

       
        public async Task SendMessage(long conversationId, string content)
        {
            var group = $"conversation:{conversationId}";
            await Clients.Group(group).SendAsync("ReceiveMessage", new
            {
                ConversationId = conversationId,
                Content = content,
                SenderConnectionId = Context.ConnectionId,
                Timestamp = System.DateTime.UtcNow
            });
        }

        
        public override async Task OnConnectedAsync()
        {
            logger.LogInformation("User {ConnectionId} connected", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        
        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            logger.LogInformation("User {ConnectionId} disconnected", Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
