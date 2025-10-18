using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IMessageRepository
    {
        Task<List<Message>> GetAllMessagesAsync();
        Task<Message?> GetMessageByIdAsync(long messageId);
        Task<Message> CreateMessageAsync(Message message);
        Task UpdateMessageAsync(Message message);
        Task<bool> MessageExistsAsync(long messageId);
        Task<List<Message>> GetMessagesByConversationIdAsync(long conversationId, int page = 1, int pageSize = 50);
        Task<List<Message>> GetMessagesByUserIdAsync(int userId, int page = 1, int pageSize = 50);
        Task<List<Message>> GetMessagesByGuestSessionIdAsync(string guestSessionId, int page = 1, int pageSize = 50);
        Task<Message?> GetLastMessageByConversationIdAsync(long conversationId);
        Task<int> CountMessagesByConversationIdAsync(long conversationId);
        Task<List<Message>> SearchMessagesAsync(string searchTerm, long? conversationId = null, int? userId = null, string? guestSessionId = null);
        Task<List<Message>> GetMessagesWithRepliesAsync(long messageId);
    }
}
