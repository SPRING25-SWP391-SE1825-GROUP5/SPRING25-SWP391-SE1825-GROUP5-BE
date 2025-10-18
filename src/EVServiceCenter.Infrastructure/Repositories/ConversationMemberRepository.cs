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
    public class ConversationMemberRepository : IConversationMemberRepository
    {
        private readonly EVDbContext _context;

        public ConversationMemberRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<ConversationMember>> GetAllConversationMembersAsync()
        {
            try
            {
                return await _context.ConversationMembers
                    .Include(cm => cm.Conversation)
                    .Include(cm => cm.User)
                    .ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<ConversationMember?> GetConversationMemberByIdAsync(long memberId)
        {
            return await _context.ConversationMembers
                .Include(cm => cm.Conversation)
                .Include(cm => cm.User)
                .FirstOrDefaultAsync(cm => cm.MemberId == memberId);
        }

        public async Task<ConversationMember> CreateConversationMemberAsync(ConversationMember member)
        {
            _context.ConversationMembers.Add(member);
            await _context.SaveChangesAsync();
            return member;
        }

        public async Task UpdateConversationMemberAsync(ConversationMember member)
        {
            _context.ConversationMembers.Update(member);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteConversationMemberAsync(long memberId)
        {
            var member = await _context.ConversationMembers.FindAsync(memberId);
            if (member == null)
            {
                return false;
            }

            _context.ConversationMembers.Remove(member);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ConversationMemberExistsAsync(long memberId)
        {
            return await _context.ConversationMembers.AnyAsync(cm => cm.MemberId == memberId);
        }

        public async Task<List<ConversationMember>> GetMembersByConversationIdAsync(long conversationId)
        {
            return await _context.ConversationMembers
                .Include(cm => cm.User)
                .Where(cm => cm.ConversationId == conversationId)
                .ToListAsync();
        }

        public async Task<List<ConversationMember>> GetMembersByUserIdAsync(int userId)
        {
            return await _context.ConversationMembers
                .Include(cm => cm.Conversation)
                .Where(cm => cm.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<ConversationMember>> GetMembersByGuestSessionIdAsync(string guestSessionId)
        {
            return await _context.ConversationMembers
                .Include(cm => cm.Conversation)
                .Where(cm => cm.GuestSessionId == guestSessionId)
                .ToListAsync();
        }

        public async Task<ConversationMember?> GetMemberByConversationAndUserAsync(long conversationId, int userId)
        {
            return await _context.ConversationMembers
                .Include(cm => cm.User)
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.UserId == userId);
        }

        public async Task<ConversationMember?> GetMemberByConversationAndGuestAsync(long conversationId, string guestSessionId)
        {
            return await _context.ConversationMembers
                .Include(cm => cm.User)
                .FirstOrDefaultAsync(cm => cm.ConversationId == conversationId && cm.GuestSessionId == guestSessionId);
        }

        public async Task<bool> RemoveMemberFromConversationAsync(long conversationId, int? userId = null, string? guestSessionId = null)
        {
            var query = _context.ConversationMembers.Where(cm => cm.ConversationId == conversationId);

            if (userId.HasValue)
            {
                query = query.Where(cm => cm.UserId == userId);
            }
            else if (!string.IsNullOrEmpty(guestSessionId))
            {
                query = query.Where(cm => cm.GuestSessionId == guestSessionId);
            }
            else
            {
                return false;
            }

            var members = await query.ToListAsync();
            if (!members.Any())
            {
                return false;
            }

            _context.ConversationMembers.RemoveRange(members);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateMemberLastReadTimeAsync(long conversationId, int? userId = null, string? guestSessionId = null)
        {
            var query = _context.ConversationMembers.Where(cm => cm.ConversationId == conversationId);

            if (userId.HasValue)
            {
                query = query.Where(cm => cm.UserId == userId);
            }
            else if (!string.IsNullOrEmpty(guestSessionId))
            {
                query = query.Where(cm => cm.GuestSessionId == guestSessionId);
            }
            else
            {
                return;
            }

            var members = await query.ToListAsync();
            foreach (var member in members)
            {
                member.LastReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}
