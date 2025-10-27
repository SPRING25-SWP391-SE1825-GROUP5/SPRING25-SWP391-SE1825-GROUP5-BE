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
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationMemberRepository _conversationMemberRepository;
        private readonly IAuthRepository _authRepository;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(
            IConversationRepository conversationRepository,
            IMessageRepository messageRepository,
            IConversationMemberRepository conversationMemberRepository,
            IAuthRepository authRepository,
            ILogger<ConversationService> logger)
        {
            _conversationRepository = conversationRepository;
            _messageRepository = messageRepository;
            _conversationMemberRepository = conversationMemberRepository;
            _authRepository = authRepository;
            _logger = logger;
        }

        public async Task<ConversationResponse> CreateConversationAsync(CreateConversationRequest request)
        {
            try
            {
                var conversation = new Conversation
                {
                    Subject = request.Subject,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdConversation = await _conversationRepository.CreateConversationAsync(conversation);

                // Add members to conversation
                foreach (var memberRequest in request.Members)
                {
                    var member = new ConversationMember
                    {
                        ConversationId = createdConversation.ConversationId,
                        UserId = memberRequest.UserId,
                        GuestSessionId = memberRequest.GuestSessionId,
                        RoleInConversation = memberRequest.RoleInConversation,
                        LastReadAt = null
                    };

                    await _conversationMemberRepository.CreateConversationMemberAsync(member);
                }

                return await MapToConversationResponseAsync(createdConversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation");
                throw;
            }
        }

        public async Task<ConversationResponse> GetConversationByIdAsync(long conversationId)
        {
            try
            {
                var conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
                if (conversation == null)
                {
                    throw new ArgumentException($"Conversation with ID {conversationId} not found");
                }

                return await MapToConversationResponseAsync(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation by ID: {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<ConversationResponse> UpdateConversationAsync(long conversationId, UpdateConversationRequest request)
        {
            try
            {
                var conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
                if (conversation == null)
                {
                    throw new ArgumentException($"Conversation with ID {conversationId} not found");
                }

                conversation.Subject = request.Subject;
                conversation.UpdatedAt = DateTime.UtcNow;

                await _conversationRepository.UpdateConversationAsync(conversation);
                return await MapToConversationResponseAsync(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating conversation: {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<bool> DeleteConversationAsync(long conversationId)
        {
            try
            {
                var conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
                if (conversation == null)
                {
                    return false;
                }

                // Note: This will be implemented when we add ConversationMemberRepository
                // For now, we'll just mark as deleted or handle in repository
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation: {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<ConversationResponse> GetOrCreateConversationAsync(GetOrCreateConversationRequest request)
        {
            try
            {
                // Try to find existing conversation
                var existingConversation = await _conversationRepository.GetConversationByMembersAsync(
                    request.Member1.UserId, 
                    request.Member2.UserId,
                    request.Member1.GuestSessionId,
                    request.Member2.GuestSessionId);

                if (existingConversation != null)
                {
                    return await MapToConversationResponseAsync(existingConversation);
                }

                // Create new conversation
                var createRequest = new CreateConversationRequest
                {
                    Subject = request.Subject,
                    Members = new List<AddMemberToConversationRequest>
                    {
                        new AddMemberToConversationRequest
                        {
                            UserId = request.Member1.UserId,
                            GuestSessionId = request.Member1.GuestSessionId,
                            RoleInConversation = request.Member1.RoleInConversation
                        },
                        new AddMemberToConversationRequest
                        {
                            UserId = request.Member2.UserId,
                            GuestSessionId = request.Member2.GuestSessionId,
                            RoleInConversation = request.Member2.RoleInConversation
                        }
                    }
                };

                return await CreateConversationAsync(createRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating conversation");
                throw;
            }
        }

        public async Task<List<ConversationResponse>> GetConversationsByUserIdAsync(int userId, int page = 1, int pageSize = 10)
        {
            try
            {
                var conversations = await _conversationRepository.GetConversationsByUserIdAsync(userId);
                
                var mappedConversations = new List<ConversationResponse>();
                foreach (var conversation in conversations
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize))
                {
                    mappedConversations.Add(await MapToConversationResponseAsync(conversation));
                }
                return mappedConversations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations by user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<List<ConversationResponse>> GetConversationsByGuestSessionIdAsync(string guestSessionId, int page = 1, int pageSize = 10)
        {
            try
            {
                var conversations = await _conversationRepository.GetConversationsByGuestSessionIdAsync(guestSessionId);
                
                var mappedConversations = new List<ConversationResponse>();
                foreach (var conversation in conversations
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize))
                {
                    mappedConversations.Add(await MapToConversationResponseAsync(conversation));
                }
                return mappedConversations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations by guest session ID: {GuestSessionId}", guestSessionId);
                throw;
            }
        }

        public async Task<List<ConversationResponse>> GetAllConversationsAsync(int page = 1, int pageSize = 10, string? searchTerm = null)
        {
            try
            {
                var conversations = await _conversationRepository.GetConversationsWithPaginationAsync(page, pageSize, searchTerm);
                
                var mappedConversations = new List<ConversationResponse>();
                foreach (var conversation in conversations)
                {
                    mappedConversations.Add(await MapToConversationResponseAsync(conversation));
                }
                return mappedConversations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all conversations");
                throw;
            }
        }

        public async Task<ConversationMemberResponse> AddMemberToConversationAsync(long conversationId, AddMemberToConversationRequest request)
        {
            try
            {
                // Note: This will be implemented when we add ConversationMemberRepository
                // For now, return a placeholder response
                await Task.CompletedTask;
                return new ConversationMemberResponse
                {
                    ConversationId = conversationId,
                    UserId = request.UserId,
                    GuestSessionId = request.GuestSessionId,
                    RoleInConversation = request.RoleInConversation
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding member to conversation: {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<bool> RemoveMemberFromConversationAsync(long conversationId, int? userId = null, string? guestSessionId = null)
        {
            try
            {
                return await _conversationMemberRepository.RemoveMemberFromConversationAsync(conversationId, userId, guestSessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member from conversation: {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<List<ConversationMemberResponse>> GetConversationMembersAsync(long conversationId)
        {
            try
            {
                var members = await _conversationMemberRepository.GetMembersByConversationIdAsync(conversationId);
                var responses = new List<ConversationMemberResponse>();
                
                foreach (var member in members)
                {
                    responses.Add(await MapToConversationMemberResponseAsync(member));
                }
                
                return responses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation members: {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<bool> UpdateMemberRoleAsync(long conversationId, int? userId, string? guestSessionId, string newRole)
        {
            try
            {
                // Note: This will be implemented when we add ConversationMemberRepository
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating member role: {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<bool> ConversationExistsAsync(long conversationId)
        {
            try
            {
                return await _conversationRepository.ConversationExistsAsync(conversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if conversation exists: {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<int> CountConversationsAsync(string? searchTerm = null)
        {
            try
            {
                return await _conversationRepository.CountConversationsAsync(searchTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting conversations");
                throw;
            }
        }

        public async Task<ConversationResponse> UpdateLastReadTimeAsync(long conversationId, int? userId = null, string? guestSessionId = null)
        {
            try
            {
                await _conversationMemberRepository.UpdateMemberLastReadTimeAsync(conversationId, userId, guestSessionId);
                
                var conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
                if (conversation == null)
                {
                    throw new ArgumentException($"Conversation with ID {conversationId} not found");
                }

                return await MapToConversationResponseAsync(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last read time: {ConversationId}", conversationId);
                throw;
            }
        }

        private async Task<ConversationResponse> MapToConversationResponseAsync(Conversation conversation)
        {
            var response = new ConversationResponse
            {
                ConversationId = conversation.ConversationId,
                Subject = conversation.Subject,
                LastMessageAt = conversation.LastMessageAt,
                LastMessageId = conversation.LastMessageId,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt
            };

            // Map last message
            if (conversation.LastMessage != null)
            {
                response.LastMessage = await MapToMessageResponseAsync(conversation.LastMessage);
            }

            // Map members
            if (conversation.ConversationMembers.Any())
            {
                var members = new List<ConversationMemberResponse>();
                foreach (var member in conversation.ConversationMembers)
                {
                    members.Add(await MapToConversationMemberResponseAsync(member));
                }
                response.Members = members;
            }

            // Map messages (limit to recent messages for performance)
            if (conversation.Messages.Any())
            {
                var recentMessages = conversation.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(50)
                    .ToList();
                
                var messages = new List<MessageResponse>();
                foreach (var message in recentMessages)
                {
                    messages.Add(await MapToMessageResponseAsync(message));
                }
                response.Messages = messages;
            }

            return response;
        }

        private async Task<ConversationMemberResponse> MapToConversationMemberResponseAsync(ConversationMember member)
        {
            var response = new ConversationMemberResponse
            {
                MemberId = member.MemberId,
                ConversationId = member.ConversationId,
                UserId = member.UserId,
                GuestSessionId = member.GuestSessionId,
                RoleInConversation = member.RoleInConversation,
                LastReadAt = member.LastReadAt
            };

            // Get user information if available
            if (member.UserId.HasValue)
            {
                // Try to get user info from navigation property first
                if (member.User != null)
                {
                    response.UserName = member.User.FullName;
                    response.UserEmail = member.User.Email;
                    // Add avatar field when available
                }
                else
                {
                    // Fallback: load user from database
                    var user = await _authRepository.GetUserByIdAsync(member.UserId.Value);
                    if (user != null)
                    {
                        response.UserName = user.FullName;
                        response.UserEmail = user.Email;
                        // Add avatar field when available
                    }
                }
            }

            return response;
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
                IsGuest = !string.IsNullOrEmpty(message.SenderGuestSessionId)
            };

            // Get sender information
            if (message.SenderUserId.HasValue && message.SenderUser != null)
            {
                response.SenderName = message.SenderUser.FullName;
                response.SenderEmail = message.SenderUser.Email;
                // Add avatar field when available
            }
            else if (!string.IsNullOrEmpty(message.SenderGuestSessionId))
            {
                response.SenderName = "Guest User";
            }

            // Map reply to message
            if (message.ReplyToMessage != null)
            {
                response.ReplyToMessage = await MapToMessageResponseAsync(message.ReplyToMessage);
            }

            return response;
        }
    }
}
