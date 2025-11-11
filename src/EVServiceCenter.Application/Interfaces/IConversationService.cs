using System.Collections.Generic;
using System.Threading.Tasks;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;

namespace EVServiceCenter.Application.Interfaces
{
    public interface IConversationService
    {
        // Basic CRUD operations
        Task<ConversationResponse> CreateConversationAsync(CreateConversationRequest request);
        Task<ConversationResponse> GetConversationByIdAsync(long conversationId);
        Task<ConversationResponse> UpdateConversationAsync(long conversationId, UpdateConversationRequest request);
        Task<bool> DeleteConversationAsync(long conversationId);

        // Conversation management
        Task<ConversationResponse> GetOrCreateConversationAsync(GetOrCreateConversationRequest request);
        Task<List<ConversationResponse>> GetConversationsByUserIdAsync(int userId, int page = 1, int pageSize = 10);
        Task<List<ConversationResponse>> GetConversationsByGuestSessionIdAsync(string guestSessionId, int page = 1, int pageSize = 10);
        Task<List<ConversationResponse>> GetAllConversationsAsync(int page = 1, int pageSize = 10, string? searchTerm = null);

        // Member management
        Task<ConversationMemberResponse> AddMemberToConversationAsync(long conversationId, AddMemberToConversationRequest request);
        Task<bool> RemoveMemberFromConversationAsync(long conversationId, int? userId = null, string? guestSessionId = null);
        Task<List<ConversationMemberResponse>> GetConversationMembersAsync(long conversationId);
        Task<bool> UpdateMemberRoleAsync(long conversationId, int? userId, string? guestSessionId, string newRole);

        // Conversation utilities
        Task<bool> ConversationExistsAsync(long conversationId);
        Task<int> CountConversationsAsync(string? searchTerm = null);
        Task<ConversationResponse> UpdateLastReadTimeAsync(long conversationId, int? userId = null, string? guestSessionId = null);

        // Assignment and reassignment
        Task<ConversationResponse> ReassignCenterAsync(long conversationId, int newCenterId, string? reason = null);
        Task<List<ConversationResponse>> GetConversationsByStaffIdAsync(int staffId, int page = 0, int pageSize = 0);
        Task<List<ConversationResponse>> GetUnassignedConversationsAsync(int page = 0, int pageSize = 0);
        Task<List<object>> GetStaffByCenterAsync(int centerId);
    }
}
