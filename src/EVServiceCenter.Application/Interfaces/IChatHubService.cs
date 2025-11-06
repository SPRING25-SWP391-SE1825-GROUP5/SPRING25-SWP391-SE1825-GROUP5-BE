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

        /// <summary>
        /// Notifies a staff member that a new conversation has been assigned to them
        /// </summary>
        /// <param name="staffUserId">The user ID of the staff member</param>
        /// <param name="conversationId">The conversation ID</param>
        Task NotifyNewConversationAsync(int staffUserId, long conversationId);

        /// <summary>
        /// Notifies all members in a conversation that the center has been reassigned
        /// </summary>
        /// <param name="conversationId">The conversation ID</param>
        /// <param name="oldStaffUserId">The old staff user ID (optional)</param>
        /// <param name="newStaffUserId">The new staff user ID</param>
        /// <param name="newCenterId">The new center ID</param>
        /// <param name="reason">The reason for reassignment (optional)</param>
        Task NotifyCenterReassignedAsync(long conversationId, int? oldStaffUserId, int newStaffUserId, int newCenterId, string? reason = null);
    }
}
