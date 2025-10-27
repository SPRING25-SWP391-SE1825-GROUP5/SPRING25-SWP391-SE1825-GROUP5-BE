using System.Threading.Tasks;

namespace EVServiceCenter.Application.Interfaces
{
    
    public interface IChatHubService
    {
        /// <summary>
        /// Broadcasts a message to all members of a conversation
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="messageData">The message data to broadcast</param>
        Task BroadcastMessageToConversationAsync(long conversationId, object messageData);

        /// <summary>
        /// Notifies all members in a conversation that a user is typing
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="userId">The user ID (optional)</param>
        /// <param name="guestSessionId">The guest session ID (optional)</param>
        /// <param name="isTyping">Whether the user is typing</param>
        Task NotifyTypingAsync(long conversationId, int? userId, string? guestSessionId, bool isTyping);
    }
}
