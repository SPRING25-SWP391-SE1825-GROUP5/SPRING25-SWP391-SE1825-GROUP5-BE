using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        private readonly IChatHubService _chatHubService;

        public MessageService(
            IMessageRepository messageRepository,
            IConversationRepository conversationRepository,
            IAccountRepository accountRepository,
            ILogger<MessageService> logger,
            IChatHubService chatHubService)
        {
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
            _accountRepository = accountRepository;
            _ = logger;
            _chatHubService = chatHubService;
        }

        public async Task<MessageResponse> CreateMessageAsync(CreateMessageRequest request)
        {
            try
            {

                if (!await _conversationRepository.ConversationExistsAsync(request.ConversationId))
                {
                    throw new ArgumentException($"Conversation with ID {request.ConversationId} not found");
                }

                if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.AttachmentUrl))
                {
                    throw new ArgumentException("Either Content or AttachmentUrl must be provided");
                }

                if (!request.SenderUserId.HasValue && string.IsNullOrWhiteSpace(request.SenderGuestSessionId))
                {
                    throw new ArgumentException("Either SenderUserId or SenderGuestSessionId must be provided");
                }

                int? normalizedSenderUserId = request.SenderUserId;
                string? normalizedSenderGuestSessionId = request.SenderGuestSessionId;

                if (normalizedSenderUserId.HasValue)
                {
                    normalizedSenderGuestSessionId = null;
                }
                else if (!string.IsNullOrWhiteSpace(normalizedSenderGuestSessionId))
                {
                    normalizedSenderUserId = null;
                    normalizedSenderGuestSessionId = normalizedSenderGuestSessionId.Trim();
                }

                var message = new Message
                {
                    ConversationId = request.ConversationId,
                    SenderUserId = normalizedSenderUserId,
                    SenderGuestSessionId = normalizedSenderGuestSessionId,
                    Content = request.Content,
                    AttachmentUrl = request.AttachmentUrl,
                    ReplyToMessageId = request.ReplyToMessageId,
                    CreatedAt = DateTime.UtcNow
                };

                var createdMessage = await _messageRepository.CreateMessageAsync(message);

                var reloadedMessage = await _messageRepository.GetMessageByIdAsync(createdMessage.MessageId);
                if (reloadedMessage != null)
                {
                    createdMessage = reloadedMessage;
                }

                await _conversationRepository.UpdateLastMessageAsync(
                    request.ConversationId,
                    createdMessage.MessageId,
                    createdMessage.CreatedAt);

                await BroadcastMessageToConversationAsync(createdMessage);

                return await MapToMessageResponseAsync(createdMessage);
            }
            catch (Exception)
            {
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
            catch (Exception)
            {
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
            catch (Exception)
            {
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
            catch (Exception)
            {
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
            catch (Exception)
            {
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
            catch (Exception)
            {
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
            catch (Exception)
            {
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
            catch (Exception)
            {
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

                int? normalizedSenderUserId = request.SenderUserId;
                string? normalizedSenderGuestSessionId = request.SenderGuestSessionId;

                if (normalizedSenderUserId.HasValue)
                {
                    normalizedSenderGuestSessionId = null;
                }
                else if (!string.IsNullOrWhiteSpace(normalizedSenderGuestSessionId))
                {
                    normalizedSenderUserId = null;
                    normalizedSenderGuestSessionId = normalizedSenderGuestSessionId.Trim();
                }

                var replyMessage = new Message
                {
                    ConversationId = originalMessage.ConversationId,
                    SenderUserId = normalizedSenderUserId,
                    SenderGuestSessionId = normalizedSenderGuestSessionId,
                    Content = request.Content,
                    AttachmentUrl = request.AttachmentUrl,
                    ReplyToMessageId = messageId,
                    CreatedAt = DateTime.UtcNow
                };

                var createdReply = await _messageRepository.CreateMessageAsync(replyMessage);

                var reloadedReply = await _messageRepository.GetMessageByIdAsync(createdReply.MessageId);
                if (reloadedReply != null)
                {
                    createdReply = reloadedReply;
                }

                await _conversationRepository.UpdateLastMessageAsync(
                    originalMessage.ConversationId,
                    createdReply.MessageId,
                    createdReply.CreatedAt);

                await BroadcastMessageToConversationAsync(createdReply);

                return await MapToMessageResponseAsync(createdReply);
            }
            catch (Exception)
            {
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
            catch (Exception)
            {
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
            catch (Exception)
            {
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
            catch (Exception)
            {
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
            catch (Exception)
            {
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
            catch (Exception)
            {
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

            var attachments = new List<AttachmentResponse>();
            if (!string.IsNullOrEmpty(message.AttachmentUrl))
            {
                try
                {
                    var urls = JsonSerializer.Deserialize<List<string>>(message.AttachmentUrl);
                    if (urls != null && urls.Count > 0)
                    {
                        foreach (var url in urls)
                        {
                            if (!string.IsNullOrEmpty(url))
                            {
                                attachments.Add(new AttachmentResponse
                                {
                                    Id = $"att-{message.MessageId}-{attachments.Count}",
                                    Type = "image",
                                    Url = url,
                                    Name = url.Split('/').LastOrDefault() ?? "image",
                                    Size = 0,
                                    Thumbnail = url
                                });
                            }
                        }
                    }
                }
                catch
                {
                    // Not a JSON array, treat as single URL string
                    attachments.Add(new AttachmentResponse
                    {
                        Id = $"att-{message.MessageId}",
                        Type = "image",
                        Url = message.AttachmentUrl,
                        Name = message.AttachmentUrl.Split('/').LastOrDefault() ?? "image",
                        Size = 0,
                        Thumbnail = message.AttachmentUrl
                    });
                }
            }
            response.Attachments = attachments.Count > 0 ? attachments : null;

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


        private async Task BroadcastMessageToConversationAsync(Message message)
        {
            try
            {

                var attachments = new List<object>();
                if (!string.IsNullOrEmpty(message.AttachmentUrl))
                {
                    try
                    {
                        var urls = JsonSerializer.Deserialize<List<string>>(message.AttachmentUrl);
                        if (urls != null && urls.Count > 0)
                        {
                            foreach (var url in urls)
                            {
                                if (!string.IsNullOrEmpty(url))
                                {
                                    attachments.Add(new
                                    {
                                        id = $"att-{message.MessageId}-{attachments.Count}",
                                        type = "image",
                                        url = url,
                                        name = url.Split('/').LastOrDefault() ?? "image",
                                        size = 0,
                                        thumbnail = url
                                    });
                                }
                            }
                        }
                    }
                    catch
                    {
                        attachments.Add(new
                        {
                            id = $"att-{message.MessageId}",
                            type = "image",
                            url = message.AttachmentUrl,
                            name = message.AttachmentUrl.Split('/').LastOrDefault() ?? "image",
                            size = 0,
                            thumbnail = message.AttachmentUrl
                        });
                    }
                }

                var messageData = new
                {
                    MessageId = message.MessageId,
                    ConversationId = message.ConversationId,
                    Content = message.Content,
                    AttachmentUrl = message.AttachmentUrl,
                    Attachments = attachments,
                    ReplyToMessageId = message.ReplyToMessageId,
                    SenderUserId = message.SenderUserId,
                    SenderGuestSessionId = message.SenderGuestSessionId,
                    SenderName = message.SenderUserId.HasValue && message.SenderUser != null
                        ? message.SenderUser.FullName
                        : "Guest User",
                    SenderEmail = message.SenderUserId.HasValue && message.SenderUser != null
                        ? message.SenderUser.Email
                        : null,
                    SenderAvatar = message.SenderUserId.HasValue && message.SenderUser != null
                        ? message.SenderUser.AvatarUrl
                        : null,
                    CreatedAt = message.CreatedAt,
                    IsGuest = !message.SenderUserId.HasValue,
                    ReplyToMessage = message.ReplyToMessageId.HasValue && message.ReplyToMessage != null ? new
                    {
                        MessageId = message.ReplyToMessage.MessageId,
                        Content = message.ReplyToMessage.Content,
                        SenderUserId = message.ReplyToMessage.SenderUserId,
                        SenderName = message.ReplyToMessage.SenderUserId.HasValue && message.ReplyToMessage.SenderUser != null
                            ? message.ReplyToMessage.SenderUser.FullName
                            : "Guest User",
                        CreatedAt = message.ReplyToMessage.CreatedAt
                    } : null
                };


                await _chatHubService.BroadcastMessageToConversationAsync(message.ConversationId, messageData);
            }
            catch (Exception)
            {
            }
        }

        public async Task NotifyTypingAsync(long conversationId, int? userId, string? guestSessionId, bool isTyping)
        {
            try
            {
                if (!await _conversationRepository.ConversationExistsAsync(conversationId))
                {
                    throw new ArgumentException($"Conversation with ID {conversationId} not found");
                }

                await _chatHubService.NotifyTypingAsync(conversationId, userId, guestSessionId, isTyping);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
