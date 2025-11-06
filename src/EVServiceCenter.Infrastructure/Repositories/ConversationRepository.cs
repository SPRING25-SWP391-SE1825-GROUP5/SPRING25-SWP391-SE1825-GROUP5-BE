using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Infrastructure.Configurations;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;

namespace EVServiceCenter.Infrastructure.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly EVDbContext _context;

        public ConversationRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<Conversation>> GetAllConversationsAsync()
        {
            try
            {
                return await _context.Conversations
                    .Include(c => c.LastMessage)
                    .Include(c => c.Messages)
                    .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Conversation?> GetConversationByIdAsync(long conversationId)
        {
            return await _context.Conversations
                .Include(c => c.LastMessage)
                .Include(c => c.Messages)
                .Include(c => c.ConversationMembers)
                    .ThenInclude(cm => cm.User)
                .Include(c => c.AssignedStaff)
                    .ThenInclude(s => s!.User)
                .Include(c => c.AssignedStaff)
                    .ThenInclude(s => s!.Center)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);
        }

        public async Task<Conversation> CreateConversationAsync(Conversation conversation)
        {
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        public async Task UpdateConversationAsync(Conversation conversation)
        {
            _context.Conversations.Update(conversation);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ConversationExistsAsync(long conversationId)
        {
            return await _context.Conversations.AnyAsync(c => c.ConversationId == conversationId);
        }

        public async Task<List<Conversation>> GetConversationsByUserIdAsync(int userId)
        {
            return await _context.Conversations
                .Include(c => c.LastMessage)
                .Include(c => c.Messages)
                .Include(c => c.ConversationMembers)
                    .ThenInclude(cm => cm.User) // Eager load User data for each member
                .Where(c => c.ConversationMembers.Any(cm => cm.UserId == userId))
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Conversation>> GetConversationsByGuestSessionIdAsync(string guestSessionId)
        {
            return await _context.Conversations
                .Include(c => c.LastMessage)
                .Include(c => c.Messages)
                .Include(c => c.ConversationMembers)
                    .ThenInclude(cm => cm.User) // Eager load User data for each member
                .Where(c => c.ConversationMembers.Any(cm => cm.GuestSessionId == guestSessionId))
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Conversation?> GetConversationByMembersAsync(int? userId1, int? userId2, string? guestSessionId1 = null, string? guestSessionId2 = null)
        {
            var query = _context.Conversations
                .Include(c => c.LastMessage)
                .Include(c => c.Messages)
                .AsQueryable();

            if (userId1.HasValue && userId2.HasValue)
            {
                // User to User conversation
                query = query.Where(c => c.ConversationMembers.Any(cm => cm.UserId == userId1) &&
                                       c.ConversationMembers.Any(cm => cm.UserId == userId2));
            }
            else if (userId1.HasValue && !string.IsNullOrEmpty(guestSessionId2))
            {
                // User to Guest conversation
                query = query.Where(c => c.ConversationMembers.Any(cm => cm.UserId == userId1) &&
                                       c.ConversationMembers.Any(cm => cm.GuestSessionId == guestSessionId2));
            }
            else if (!string.IsNullOrEmpty(guestSessionId1) && !string.IsNullOrEmpty(guestSessionId2))
            {
                // Guest to Guest conversation
                query = query.Where(c => c.ConversationMembers.Any(cm => cm.GuestSessionId == guestSessionId1) &&
                                       c.ConversationMembers.Any(cm => cm.GuestSessionId == guestSessionId2));
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task UpdateLastMessageAsync(long conversationId, long messageId, DateTime lastMessageAt)
        {
            var conversation = await _context.Conversations.FindAsync(conversationId);
            if (conversation != null)
            {
                conversation.LastMessageId = messageId;
                conversation.LastMessageAt = lastMessageAt;
                conversation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Conversation>> GetConversationsWithPaginationAsync(int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            var query = _context.Conversations
                .Include(c => c.LastMessage)
                .Include(c => c.Messages)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.Subject != null && c.Subject.Contains(searchTerm));
            }

            return await query
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountConversationsAsync(string? searchTerm = null)
        {
            var query = _context.Conversations.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.Subject != null && c.Subject.Contains(searchTerm));
            }

            return await query.CountAsync();
        }

        public async Task<int> CountActiveConversationsByStaffIdAsync(int staffId)
        {
            return await _context.Conversations
                .Where(c => c.AssignedStaffId == staffId && c.AssignedStaffId != null)
                .CountAsync();
        }

        public async Task<List<Conversation>> GetConversationsByStaffIdAsync(int staffId, int page = 1, int pageSize = 10)
        {
            return await _context.Conversations
                .Include(c => c.LastMessage)
                .Include(c => c.AssignedStaff)
                    .ThenInclude(s => s!.User)
                .Include(c => c.AssignedStaff)
                    .ThenInclude(s => s!.Center)
                .Where(c => c.AssignedStaffId == staffId)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Conversation>> GetUnassignedConversationsAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Conversations
                .Include(c => c.LastMessage)
                .Include(c => c.ConversationMembers)
                    .ThenInclude(cm => cm.User)
                .Where(c => c.AssignedStaffId == null)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
