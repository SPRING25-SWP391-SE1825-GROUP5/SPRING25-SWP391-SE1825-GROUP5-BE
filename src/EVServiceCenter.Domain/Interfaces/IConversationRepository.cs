using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IConversationRepository
    {
        Task<List<Conversation>> GetAllConversationsAsync();
        Task<Conversation?> GetConversationByIdAsync(long conversationId);
        Task<Conversation> CreateConversationAsync(Conversation conversation);
        Task UpdateConversationAsync(Conversation conversation);
        Task<bool> ConversationExistsAsync(long conversationId);
        Task<List<Conversation>> GetConversationsByUserIdAsync(int userId);
        Task<List<Conversation>> GetConversationsByGuestSessionIdAsync(string guestSessionId);
        Task<Conversation?> GetConversationByMembersAsync(int? userId1, int? userId2, string? guestSessionId1 = null, string? guestSessionId2 = null);
        Task UpdateLastMessageAsync(long conversationId, long messageId, DateTime lastMessageAt);
        Task<List<Conversation>> GetConversationsWithPaginationAsync(int page = 1, int pageSize = 10, string? searchTerm = null);
        Task<int> CountConversationsAsync(string? searchTerm = null);
    }
}
