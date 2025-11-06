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
    public class MessageRepository : IMessageRepository
    {
        private readonly EVDbContext _context;

        public MessageRepository(EVDbContext context)
        {
            _context = context;
        }

        public async Task<List<Message>> GetAllMessagesAsync()
        {
            try
            {
                return await _context.Messages
                    .Include(m => m.Conversation)
                    .Include(m => m.SenderUser)
                    .Include(m => m.ReplyToMessage)
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Message?> GetMessageByIdAsync(long messageId)
        {
            return await _context.Messages
                .Include(m => m.Conversation)
                .Include(m => m.SenderUser)
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(r => r!.SenderUser)
                .FirstOrDefaultAsync(m => m.MessageId == messageId);
        }

        public async Task<Message> CreateMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task UpdateMessageAsync(Message message)
        {
            _context.Messages.Update(message);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> MessageExistsAsync(long messageId)
        {
            return await _context.Messages.AnyAsync(m => m.MessageId == messageId);
        }

        public async Task<List<Message>> GetMessagesByConversationIdAsync(long conversationId, int page = 1, int pageSize = 50)
        {
            return await _context.Messages
                .Include(m => m.SenderUser)
                .Include(m => m.ReplyToMessage)
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Message>> GetMessagesByUserIdAsync(int userId, int page = 1, int pageSize = 50)
        {
            return await _context.Messages
                .Include(m => m.Conversation)
                .Include(m => m.SenderUser)
                .Include(m => m.ReplyToMessage)
                .Where(m => m.SenderUserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Message>> GetMessagesByGuestSessionIdAsync(string guestSessionId, int page = 1, int pageSize = 50)
        {
            return await _context.Messages
                .Include(m => m.Conversation)
                .Include(m => m.SenderUser)
                .Include(m => m.ReplyToMessage)
                .Where(m => m.SenderGuestSessionId == guestSessionId)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Message?> GetLastMessageByConversationIdAsync(long conversationId)
        {
            return await _context.Messages
                .Include(m => m.SenderUser)
                .Include(m => m.ReplyToMessage)
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<int> CountMessagesByConversationIdAsync(long conversationId)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .CountAsync();
        }

        public async Task<List<Message>> SearchMessagesAsync(string searchTerm, long? conversationId = null, int? userId = null, string? guestSessionId = null)
        {
            var query = _context.Messages
                .Include(m => m.Conversation)
                .Include(m => m.SenderUser)
                .Include(m => m.ReplyToMessage)
                .Where(m => m.Content != null && m.Content.Contains(searchTerm))
                .AsQueryable();

            if (conversationId.HasValue)
            {
                query = query.Where(m => m.ConversationId == conversationId.Value);
            }

            if (userId.HasValue)
            {
                query = query.Where(m => m.SenderUserId == userId.Value);
            }

            if (!string.IsNullOrEmpty(guestSessionId))
            {
                query = query.Where(m => m.SenderGuestSessionId == guestSessionId);
            }

            return await query
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Message>> GetMessagesWithRepliesAsync(long messageId)
        {
            return await _context.Messages
                .Include(m => m.SenderUser)
                .Include(m => m.ReplyToMessage)
                .Where(m => m.ReplyToMessageId == messageId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }
    }
}
