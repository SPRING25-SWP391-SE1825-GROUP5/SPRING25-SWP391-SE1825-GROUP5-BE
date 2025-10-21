using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Domain.Entities;

namespace EVServiceCenter.Domain.Interfaces
{
    public interface IConversationMemberRepository
    {
        Task<List<ConversationMember>> GetAllConversationMembersAsync();
        Task<ConversationMember?> GetConversationMemberByIdAsync(long memberId);
        Task<ConversationMember> CreateConversationMemberAsync(ConversationMember member);
        Task UpdateConversationMemberAsync(ConversationMember member);
        Task<bool> DeleteConversationMemberAsync(long memberId);
        Task<bool> ConversationMemberExistsAsync(long memberId);
        Task<List<ConversationMember>> GetMembersByConversationIdAsync(long conversationId);
        Task<List<ConversationMember>> GetMembersByUserIdAsync(int userId);
        Task<List<ConversationMember>> GetMembersByGuestSessionIdAsync(string guestSessionId);
        Task<ConversationMember?> GetMemberByConversationAndUserAsync(long conversationId, int userId);
        Task<ConversationMember?> GetMemberByConversationAndGuestAsync(long conversationId, string guestSessionId);
        Task<bool> RemoveMemberFromConversationAsync(long conversationId, int? userId = null, string? guestSessionId = null);
        Task UpdateMemberLastReadTimeAsync(long conversationId, int? userId = null, string? guestSessionId = null);
    }
}
