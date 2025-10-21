using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IMessageService
    {
        // Basic CRUD operations
        Task<MessageResponse> CreateMessageAsync(CreateMessageRequest request);
        Task<MessageResponse> GetMessageByIdAsync(long messageId);
        Task<MessageResponse> UpdateMessageAsync(long messageId, UpdateMessageRequest request);
        Task<bool> DeleteMessageAsync(long messageId);
        
        // Message retrieval
        Task<List<MessageResponse>> GetMessagesByConversationIdAsync(long conversationId, int page = 1, int pageSize = 50);
        Task<List<MessageResponse>> GetMessagesByUserIdAsync(int userId, int page = 1, int pageSize = 50);
        Task<List<MessageResponse>> GetMessagesByGuestSessionIdAsync(string guestSessionId, int page = 1, int pageSize = 50);
        Task<MessageResponse?> GetLastMessageByConversationIdAsync(long conversationId);
        
        // Message features
        Task<MessageResponse> ReplyToMessageAsync(long messageId, ReplyToMessageRequest request);
        Task<List<MessageResponse>> GetMessageRepliesAsync(long messageId);
        Task<List<MessageResponse>> SearchMessagesAsync(SearchMessagesRequest request);
        
        // Message utilities
        Task<bool> MessageExistsAsync(long messageId);
        Task<int> CountMessagesByConversationIdAsync(long conversationId);
        Task<bool> MarkMessageAsReadAsync(long messageId, int? userId = null, string? guestSessionId = null);
        
        // Real-time messaging
        Task<MessageResponse> SendMessageAsync(SendMessageRequest request);
        Task<List<MessageResponse>> GetUnreadMessagesAsync(int? userId = null, string? guestSessionId = null);
    }
}
