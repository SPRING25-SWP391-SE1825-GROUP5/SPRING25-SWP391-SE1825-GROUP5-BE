using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVServiceCenter.Application.Interfaces;
using EVServiceCenter.Application.Models.Requests;
using EVServiceCenter.Application.Models.Responses;
using EVServiceCenter.Domain.Entities;
using EVServiceCenter.Domain.Interfaces;
using EVServiceCenter.Domain.IRepositories;
using Microsoft.Extensions.Logging;

namespace EVServiceCenter.Application.Service
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationRepository _conversationRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ILogger<MessageService> _logger;

        public MessageService(
            IMessageRepository messageRepository,
            IConversationRepository conversationRepository,
            IAccountRepository accountRepository,
            ILogger<MessageService> logger)
        {
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
            _accountRepository = accountRepository;
            _logger = logger;
        }

        public async Task<MessageResponse> CreateMessageAsync(CreateMessageRequest request)
        {
            try
            {
                
                if (!await _conversationRepository.ConversationExistsAsync(request.ConversationId))
                {
                    throw new ArgumentException($"Conversation with ID {request.ConversationId} not found");
                }

                
                if (!request.SenderUserId.HasValue && string.IsNullOrEmpty(request.SenderGuestSessionId))
                {
                    throw new ArgumentException("Either SenderUserId or SenderGuestSessionId must be provided");
                }

                var message = new Message
                {
                    ConversationId = request.ConversationId,
                    SenderUserId = request.SenderUserId,
                    SenderGuestSessionId = request.SenderGuestSessionId,
                    Content = request.Content,
                    AttachmentUrl = request.AttachmentUrl,
                    ReplyToMessageId = request.ReplyToMessageId,
                    CreatedAt = DateTime.UtcNow
                };

                var createdMessage = await _messageRepository.CreateMessageAsync(message);

                
                await _conversationRepository.UpdateLastMessageAsync(
                    request.ConversationId, 
                    createdMessage.MessageId, 
                    createdMessage.CreatedAt);

                return await MapToMessageResponseAsync(createdMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating message for conversation {ConversationId}", request.ConversationId);
                throw;
            }
        }

        public async Task<MessageResponse> GetMessageByIdAsync(long messageId)
        {
            try
            {
                var message = await _messageRepository.GetMessageByIdAsync(messageId);
                if (message == null)
                {
                    throw new ArgumentException($"Message with ID {messageId} not found");
                }

                return await MapToMessageResponseAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message {MessageId}", messageId);
                throw;
            }
        }

        public async Task<MessageResponse> UpdateMessageAsync(long messageId, UpdateMessageRequest request)
        {
            try
            {
                var message = await _messageRepository.GetMessageByIdAsync(messageId);
                if (message == null)
                {
                    throw new ArgumentException($"Message with ID {messageId} not found");
                }

                message.Content = request.Content;
                message.AttachmentUrl = request.AttachmentUrl;

                await _messageRepository.UpdateMessageAsync(message);
                return await MapToMessageResponseAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating message {MessageId}", messageId);
                throw;
            }
        }

        public async Task<bool> DeleteMessageAsync(long messageId)
        {
            try
            {
                if (!await _messageRepository.MessageExistsAsync(messageId))
                {
                    return false;
                }

                var message = await _messageRepository.GetMessageByIdAsync(messageId);
                if (message == null)
                {
                    return false;
                }

                
                message.Content = "[Message deleted]";
                message.AttachmentUrl = null;

                await _messageRepository.UpdateMessageAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
                throw;
            }
        }

        public async Task<List<MessageResponse>> GetMessagesByConversationIdAsync(long conversationId, int page = 1, int pageSize = 50)
        {
            try
            {
                var messages = await _messageRepository.GetMessagesByConversationIdAsync(conversationId, page, pageSize);
                var responses = new List<MessageResponse>();

                foreach (var message in messages)
                {
                    responses.Add(await MapToMessageResponseAsync(message));
                }

                return responses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for conversation {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<List<MessageResponse>> GetMessagesByUserIdAsync(int userId, int page = 1, int pageSize = 50)
        {
            try
            {
                var messages = await _messageRepository.GetMessagesByUserIdAsync(userId, page, pageSize);
                var responses = new List<MessageResponse>();

                foreach (var message in messages)
                {
                    responses.Add(await MapToMessageResponseAsync(message));
                }

                return responses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<MessageResponse>> GetMessagesByGuestSessionIdAsync(string guestSessionId, int page = 1, int pageSize = 50)
        {
            try
            {
                var messages = await _messageRepository.GetMessagesByGuestSessionIdAsync(guestSessionId, page, pageSize);
                var responses = new List<MessageResponse>();

                foreach (var message in messages)
                {
                    responses.Add(await MapToMessageResponseAsync(message));
                }

                return responses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for guest session {GuestSessionId}", guestSessionId);
                throw;
            }
        }

        public async Task<MessageResponse?> GetLastMessageByConversationIdAsync(long conversationId)
        {
            try
            {
                var message = await _messageRepository.GetLastMessageByConversationIdAsync(conversationId);
                if (message == null)
                {
                    return null;
                }

                return await MapToMessageResponseAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting last message for conversation {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<MessageResponse> ReplyToMessageAsync(long messageId, ReplyToMessageRequest request)
        {
            try
            {
                var originalMessage = await _messageRepository.GetMessageByIdAsync(messageId);
                if (originalMessage == null)
                {
                    throw new ArgumentException($"Message with ID {messageId} not found");
                }

                var replyMessage = new Message
                {
                    ConversationId = originalMessage.ConversationId,
                    SenderUserId = request.SenderUserId,
                    SenderGuestSessionId = request.SenderGuestSessionId,
                    Content = request.Content,
                    AttachmentUrl = request.AttachmentUrl,
                    ReplyToMessageId = messageId,
                    CreatedAt = DateTime.UtcNow
                };

                var createdReply = await _messageRepository.CreateMessageAsync(replyMessage);

                
                await _conversationRepository.UpdateLastMessageAsync(
                    originalMessage.ConversationId,
                    createdReply.MessageId,
                    createdReply.CreatedAt);

                return await MapToMessageResponseAsync(createdReply);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replying to message {MessageId}", messageId);
                throw;
            }
        }

        public async Task<List<MessageResponse>> GetMessageRepliesAsync(long messageId)
        {
            try
            {
                var replies = await _messageRepository.GetMessagesWithRepliesAsync(messageId);
                var responses = new List<MessageResponse>();

                foreach (var reply in replies)
                {
                    responses.Add(await MapToMessageResponseAsync(reply));
                }

                return responses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting replies for message {MessageId}", messageId);
                throw;
            }
        }

        public async Task<List<MessageResponse>> SearchMessagesAsync(SearchMessagesRequest request)
        {
            try
            {
                var messages = await _messageRepository.SearchMessagesAsync(
                    request.SearchTerm,
                    request.ConversationId,
                    request.UserId,
                    request.GuestSessionId);

                var responses = new List<MessageResponse>();

                foreach (var message in messages)
                {
                    responses.Add(await MapToMessageResponseAsync(message));
                }

                
                return responses
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching messages with term {SearchTerm}", request.SearchTerm);
                throw;
            }
        }

        public async Task<bool> MessageExistsAsync(long messageId)
        {
            return await _messageRepository.MessageExistsAsync(messageId);
        }

        public async Task<int> CountMessagesByConversationIdAsync(long conversationId)
        {
            return await _messageRepository.CountMessagesByConversationIdAsync(conversationId);
        }

        public async Task<bool> MarkMessageAsReadAsync(long messageId, int? userId = null, string? guestSessionId = null)
        {
            try
            {
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message {MessageId} as read", messageId);
                throw;
            }
        }

        public async Task<MessageResponse> SendMessageAsync(SendMessageRequest request)
        {
            try
            {
                var createRequest = new CreateMessageRequest
                {
                    ConversationId = request.ConversationId,
                    SenderUserId = request.SenderUserId,
                    SenderGuestSessionId = request.SenderGuestSessionId,
                    Content = request.Content,
                    AttachmentUrl = request.AttachmentUrl,
                    ReplyToMessageId = request.ReplyToMessageId
                };

                return await CreateMessageAsync(createRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to conversation {ConversationId}", request.ConversationId);
                throw;
            }
        }

        public async Task<List<MessageResponse>> GetUnreadMessagesAsync(int? userId = null, string? guestSessionId = null)
        {
            try
            {
                await Task.CompletedTask;
                return new List<MessageResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread messages for user {UserId} or guest {GuestSessionId}", userId, guestSessionId);
                throw;
            }
        }

        private async Task<MessageResponse> MapToMessageResponseAsync(Message message)
        {
            var response = new MessageResponse
            {
                MessageId = message.MessageId,
                ConversationId = message.ConversationId,
                SenderUserId = message.SenderUserId,
                SenderGuestSessionId = message.SenderGuestSessionId,
                Content = message.Content ?? string.Empty,
                AttachmentUrl = message.AttachmentUrl,
                ReplyToMessageId = message.ReplyToMessageId,
                CreatedAt = message.CreatedAt,
                IsGuest = !message.SenderUserId.HasValue
            };

            
            if (message.SenderUserId.HasValue && message.SenderUser != null)
            {
                response.SenderName = message.SenderUser.FullName;
                response.SenderEmail = message.SenderUser.Email;
                response.SenderAvatar = message.SenderUser.AvatarUrl;
            }
            else if (!string.IsNullOrEmpty(message.SenderGuestSessionId))
            {
                response.SenderName = "Guest User";
                response.SenderEmail = null;
                response.SenderAvatar = null;
            }

            
            if (message.ReplyToMessageId.HasValue && message.ReplyToMessage != null)
            {
                response.ReplyToMessage = await MapToMessageResponseAsync(message.ReplyToMessage);
            }

            return response;
        }
    }
}
