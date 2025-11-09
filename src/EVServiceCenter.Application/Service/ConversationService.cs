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
using Microsoft.Extensions.Options;
using EVServiceCenter.Application.Configurations;

namespace EVServiceCenter.Application.Service
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationMemberRepository _conversationMemberRepository;
        private readonly IAuthRepository _authRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IStaffRepository _staffRepository;
        private readonly ICenterRepository _centerRepository;
        private readonly IChatHubService _chatHubService;
        private readonly ChatSettings _chatSettings;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(
            IConversationRepository conversationRepository,
            IMessageRepository messageRepository,
            IConversationMemberRepository conversationMemberRepository,
            IAuthRepository authRepository,
            IBookingRepository bookingRepository,
            IStaffRepository staffRepository,
            ICenterRepository centerRepository,
            IChatHubService chatHubService,
            IOptions<ChatSettings> chatSettings,
            ILogger<ConversationService> logger)
        {
            _conversationRepository = conversationRepository;
            _messageRepository = messageRepository;
            _conversationMemberRepository = conversationMemberRepository;
            _authRepository = authRepository;
            _bookingRepository = bookingRepository;
            _staffRepository = staffRepository;
            _centerRepository = centerRepository;
            _chatHubService = chatHubService;
            _chatSettings = chatSettings.Value;
            _logger = logger;
        }

        public async Task<ConversationResponse> CreateConversationAsync(CreateConversationRequest request)
        {
            try
            {
                // Log incoming request for debugging
                _logger.LogInformation(
                    "CreateConversationAsync called with {MemberCount} members. Members: {Members}",
                    request.Members.Count,
                    string.Join(", ", request.Members.Select(m =>
                        $"Role={m.RoleInConversation}, UserId={m.UserId?.ToString() ?? "(null)"}, GuestSessionId='{m.GuestSessionId ?? "(null)"}'")));

                // Normalize and validate members first
                foreach (var memberRequest in request.Members)
                {
                    // Normalize GuestSessionId: trim and convert empty string to null
                    if (!string.IsNullOrWhiteSpace(memberRequest.GuestSessionId))
                    {
                        memberRequest.GuestSessionId = memberRequest.GuestSessionId.Trim();
                    }
                    else
                    {
                        memberRequest.GuestSessionId = null;
                    }
                }

                // Validate that we have at least one valid member before processing
                bool hasValidMember = request.Members.Any(m =>
                    m.UserId.HasValue || !string.IsNullOrWhiteSpace(m.GuestSessionId));

                if (!hasValidMember)
                {
                    _logger.LogError(
                        "Cannot create conversation: No valid members found (all members have both UserId and GuestSessionId as null/empty)");
                    throw new ArgumentException("Conversation must have at least one member with either UserId or GuestSessionId");
                }

                // Find customer from members
                int? customerUserId = null;
                string? customerGuestSessionId = null;

                foreach (var memberRequest in request.Members)
                {
                    if (memberRequest.RoleInConversation == _chatSettings.Roles.Customer)
                    {
                        customerUserId = memberRequest.UserId;
                        customerGuestSessionId = memberRequest.GuestSessionId;
                        break;
                    }
                }

                // Always create new conversation (allow multiple conversations per customer)
                var conversation = new Conversation
                {
                    Subject = request.Subject,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdConversation = await _conversationRepository.CreateConversationAsync(conversation);
                _logger.LogInformation("Created new conversation {ConversationId}", createdConversation.ConversationId);

                // Add members to conversation
                foreach (var memberRequest in request.Members)
                {
                    // Skip placeholder staff entries (role is staff but no concrete user)
                    if (_chatSettings.Roles.StaffRoles.Contains(memberRequest.RoleInConversation)
                        && !memberRequest.UserId.HasValue)
                    {
                        _logger.LogInformation(
                            "Skipping placeholder staff member with role {Role} (no UserId)",
                            memberRequest.RoleInConversation);
                        continue;
                    }

                    // Normalize GuestSessionId again (in case it wasn't normalized earlier)
                    string? normalizedGuestSessionId = string.IsNullOrWhiteSpace(memberRequest.GuestSessionId)
                        ? null
                        : memberRequest.GuestSessionId.Trim();

                    // Skip members with both UserId and GuestSessionId as null/empty (violates CK_ConversationMembers_ActorXor constraint)
                    bool hasUserId = memberRequest.UserId.HasValue;
                    bool hasGuestSessionId = !string.IsNullOrWhiteSpace(normalizedGuestSessionId);

                    if (!hasUserId && !hasGuestSessionId)
                    {
                        _logger.LogWarning(
                            "Skipping member with role {Role} because both UserId and GuestSessionId are null/empty. UserId: {UserId}, GuestSessionId: '{GuestSessionId}'",
                            memberRequest.RoleInConversation,
                            memberRequest.UserId,
                            memberRequest.GuestSessionId ?? "(null)");
                        continue;
                    }

                    _logger.LogInformation(
                        "Adding member to conversation {ConversationId}: Role={Role}, UserId={UserId}, GuestSessionId='{GuestSessionId}'",
                        createdConversation.ConversationId,
                        memberRequest.RoleInConversation,
                        memberRequest.UserId?.ToString() ?? "(null)",
                        normalizedGuestSessionId ?? "(null)");

                    var member = new ConversationMember
                    {
                        ConversationId = createdConversation.ConversationId,
                        UserId = memberRequest.UserId,
                        GuestSessionId = normalizedGuestSessionId, // Use normalized value (null instead of empty string)
                        RoleInConversation = memberRequest.RoleInConversation,
                        LastReadAt = null
                    };

                    await _conversationMemberRepository.CreateConversationMemberAsync(member);
                    _logger.LogInformation(
                        "Successfully added member {MemberId} to conversation {ConversationId}",
                        member.MemberId, createdConversation.ConversationId);
                }

                // Auto-assign staff if customer exists and there is no REAL staff member in request
                bool hasStaffMember = request.Members.Any(m =>
                    _chatSettings.Roles.StaffRoles.Contains(m.RoleInConversation)
                    && m.UserId.HasValue);

                if (!hasStaffMember && customerUserId.HasValue && _chatSettings.Assignment.AutoAssignEnabled)
                {
                    Staff? assignedStaff = null;

                    // Nếu có PreferredStaffId, sử dụng staff đó
                    if (request.PreferredStaffId.HasValue)
                    {
                        assignedStaff = await _staffRepository.GetStaffByIdAsync(request.PreferredStaffId.Value);

                        // Validate staff exists, is active, and is not MANAGER (only STAFF role)
                        if (assignedStaff != null && assignedStaff.IsActive &&
                            assignedStaff.User != null && assignedStaff.User.Role == "STAFF")
                        {
                            // Staff hợp lệ, sử dụng
                            _logger.LogInformation(
                                "Using preferred staff {StaffId} (User {UserId}) for conversation {ConversationId}",
                                assignedStaff.StaffId, assignedStaff.UserId, createdConversation.ConversationId);
                        }
                        else
                        {
                            // Staff không hợp lệ (không tồn tại, không active, hoặc là MANAGER), fallback to auto-assign
                            _logger.LogWarning(
                                "Preferred staff {StaffId} is not valid (exists: {Exists}, active: {Active}, role: {Role}), falling back to auto-assign",
                                request.PreferredStaffId.Value,
                                assignedStaff != null,
                                assignedStaff?.IsActive ?? false,
                                assignedStaff?.User?.Role ?? "null");
                            assignedStaff = null;
                        }
                    }

                    // Nếu không có PreferredStaffId hoặc PreferredStaffId không hợp lệ, auto-assign
                    if (assignedStaff == null)
                    {
                        // Get customer location (priority: request location, fallback: booking center location)
                        var (customerLat, customerLng) = await GetCustomerLocationAsync(
                            customerUserId.Value,
                            request.CustomerLatitude,
                            request.CustomerLongitude);

                        assignedStaff = await AssignStaffAsync(
                            customerUserId.Value,
                            request.PreferredCenterId,
                            customerLat,
                            customerLng);
                    }

                    if (assignedStaff != null)
                    {
                        createdConversation.AssignedStaffId = assignedStaff.StaffId;
                        await _conversationRepository.UpdateConversationAsync(createdConversation);

                        // Add staff as member
                        var staffMember = new ConversationMember
                        {
                            ConversationId = createdConversation.ConversationId,
                            UserId = assignedStaff.UserId,
                            RoleInConversation = _chatSettings.Roles.Staff,
                            LastReadAt = null
                        };
                        await _conversationMemberRepository.CreateConversationMemberAsync(staffMember);

                        // Notify staff via SignalR
                        await _chatHubService.NotifyNewConversationAsync(assignedStaff.UserId, createdConversation.ConversationId);

                        _logger.LogInformation(
                            "Assigned staff {StaffId} (User {UserId}) to conversation {ConversationId}",
                            assignedStaff.StaffId, assignedStaff.UserId, createdConversation.ConversationId);
                    }
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
                    _logger.LogWarning("Conversation {ConversationId} not found for deletion", conversationId);
                    return false;
                }

                // Delete conversation (repository will handle cascade delete of members and messages)
                await _conversationRepository.DeleteConversationAsync(conversationId);

                _logger.LogInformation("Successfully deleted conversation {ConversationId}", conversationId);
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
                    PreferredCenterId = request.PreferredCenterId,
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
                var lastReadAt = DateTime.UtcNow;
                await _conversationMemberRepository.UpdateMemberLastReadTimeAsync(conversationId, userId, guestSessionId);

                var conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
                if (conversation == null)
                {
                    throw new ArgumentException($"Conversation with ID {conversationId} not found");
                }

                // Broadcast read status update to all conversation members via SignalR
                try
                {
                    await _chatHubService.NotifyMessageReadAsync(conversationId, userId, guestSessionId, lastReadAt);
                    _logger.LogInformation("Broadcasted read status update for conversation {ConversationId}, UserId: {UserId}, GuestSessionId: {GuestSessionId}, LastReadAt: {LastReadAt}",
                        conversationId, userId, guestSessionId, lastReadAt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error broadcasting read status update for conversation {ConversationId}", conversationId);
                    // Don't throw - read status update should still succeed even if broadcast fails
                }

                return await MapToConversationResponseAsync(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last read time: {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<ConversationResponse> ReassignCenterAsync(long conversationId, int newCenterId, string? reason = null)
        {
            try
            {
                var conversation = await _conversationRepository.GetConversationByIdAsync(conversationId);
                if (conversation == null)
                {
                    throw new ArgumentException($"Conversation with ID {conversationId} not found");
                }

                var centerStaff = await _staffRepository.GetStaffByCenterIdAsync(newCenterId);
                centerStaff = FilterNonManagerStaff(centerStaff).ToList();
                var activeStaff = centerStaff.Where(s => s.IsActive).FirstOrDefault();

                if (activeStaff == null)
                {
                    throw new ArgumentException(
                        _chatSettings.Messages.NoActiveStaffInCenter.Replace("{CenterId}", newCenterId.ToString()));
                }

                var oldStaff = conversation.AssignedStaffId.HasValue
                    ? await _staffRepository.GetStaffByIdAsync(conversation.AssignedStaffId.Value)
                    : null;

                if (oldStaff != null)
                {
                    await _conversationMemberRepository.RemoveMemberFromConversationAsync(
                        conversationId,
                        oldStaff.UserId,
                        null);
                }

                var newStaffMember = new ConversationMember
                {
                    ConversationId = conversationId,
                    UserId = activeStaff.UserId,
                    RoleInConversation = _chatSettings.Roles.Staff
                };
                await _conversationMemberRepository.CreateConversationMemberAsync(newStaffMember);

                conversation.AssignedStaffId = activeStaff.StaffId;
                conversation.UpdatedAt = DateTime.UtcNow;
                await _conversationRepository.UpdateConversationAsync(conversation);

                await _chatHubService.NotifyCenterReassignedAsync(
                    conversationId,
                    oldStaff?.UserId,
                    activeStaff.UserId,
                    newCenterId,
                    reason);

                var centerName = activeStaff.Center?.CenterName ?? newCenterId.ToString();
                var messageContent = reason != null
                    ? _chatSettings.Messages.CenterReassignedWithReasonTemplate
                        .Replace("{CenterName}", centerName)
                        .Replace("{Reason}", reason)
                    : _chatSettings.Messages.CenterReassignedTemplate
                        .Replace("{CenterName}", centerName);

                var systemMessage = new Message
                {
                    ConversationId = conversationId,
                    Content = messageContent,
                    CreatedAt = DateTime.UtcNow
                };
                await _messageRepository.CreateMessageAsync(systemMessage);

                _logger.LogInformation(
                    "Reassigned conversation {ConversationId} from staff {OldStaffId} to staff {NewStaffId} (center {CenterId})",
                    conversationId, oldStaff?.StaffId, activeStaff.StaffId, newCenterId);

                return await MapToConversationResponseAsync(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning center for conversation {ConversationId}", conversationId);
                throw;
            }
        }

        public async Task<List<ConversationResponse>> GetConversationsByStaffIdAsync(int staffId, int page = 0, int pageSize = 0)
        {
            try
            {
                if (page <= 0) page = _chatSettings.Pagination.DefaultPage;
                if (pageSize <= 0) pageSize = _chatSettings.Pagination.DefaultPageSize;
                if (pageSize > _chatSettings.Pagination.MaxPageSize) pageSize = _chatSettings.Pagination.MaxPageSize;

                var conversations = await _conversationRepository.GetConversationsByStaffIdAsync(staffId, page, pageSize);
                var responses = new List<ConversationResponse>();

                foreach (var conversation in conversations)
                {
                    responses.Add(await MapToConversationResponseAsync(conversation));
                }

                return responses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations by staff ID: {StaffId}", staffId);
                throw;
            }
        }

        public async Task<List<ConversationResponse>> GetUnassignedConversationsAsync(int page = 0, int pageSize = 0)
        {
            try
            {
                if (page <= 0) page = _chatSettings.Pagination.DefaultPage;
                if (pageSize <= 0) pageSize = _chatSettings.Pagination.DefaultPageSize;
                if (pageSize > _chatSettings.Pagination.MaxPageSize) pageSize = _chatSettings.Pagination.MaxPageSize;

                var conversations = await _conversationRepository.GetUnassignedConversationsAsync(page, pageSize);
                var responses = new List<ConversationResponse>();

                foreach (var conversation in conversations)
                {
                    responses.Add(await MapToConversationResponseAsync(conversation));
                }

                return responses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unassigned conversations");
                throw;
            }
        }

        public async Task<List<object>> GetStaffByCenterAsync(int centerId)
        {
            try
            {
                // Lấy danh sách staff của center
                var staffList = await _staffRepository.GetStaffByCenterIdAsync(centerId);

                // Chỉ lấy staff có role STAFF (không lấy MANAGER), và phải active
                var staffOnly = staffList
                    .Where(s => s.User != null &&
                               s.User.Role == "STAFF" && // Chỉ lấy STAFF, không lấy MANAGER
                               s.IsActive)
                    .Select(s => new
                    {
                        staffId = s.StaffId,
                        userId = s.UserId,
                        fullName = s.User?.FullName ?? "",
                        email = s.User?.Email ?? "",
                        phoneNumber = s.User?.PhoneNumber ?? "",
                        avatar = s.User?.AvatarUrl,
                        centerId = s.CenterId,
                        centerName = s.Center?.CenterName ?? ""
                    })
                    .Cast<object>()
                    .ToList();

                return staffOnly;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting staff by center {CenterId}", centerId);
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
                UpdatedAt = conversation.UpdatedAt,
                AssignedStaffId = conversation.AssignedStaffId
            };

            // Map assigned staff info
            if (conversation.AssignedStaff != null)
            {
                response.AssignedStaffName = conversation.AssignedStaff.User?.FullName;
                response.AssignedStaffEmail = conversation.AssignedStaff.User?.Email;
                response.AssignedCenterId = conversation.AssignedStaff.CenterId;
                response.AssignedCenterName = conversation.AssignedStaff.Center?.CenterName;
            }

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

            // Build attachments array from AttachmentUrl (same logic as MessageService)
            var attachments = new List<AttachmentResponse>();
            if (!string.IsNullOrEmpty(message.AttachmentUrl))
            {
                try
                {
                    // Try to parse as JSON array first
                    var urls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(message.AttachmentUrl);
                    if (urls != null && urls.Count > 0)
                    {
                        // Multiple URLs (JSON array)
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

        private async Task<Staff?> AssignStaffAsync(int customerUserId, int? preferredCenterId = null, double? customerLat = null, double? customerLng = null)
        {
            try
            {
                int? targetCenterId = null;

                // Strategy 0: If customer location is provided, find nearest center using Haversine
                if (customerLat.HasValue && customerLng.HasValue)
                {
                    var allCenters = await _centerRepository.GetActiveCentersAsync();
                    var centersWithGeo = allCenters
                        .Where(c => c.Latitude.HasValue && c.Longitude.HasValue)
                        .Select(c => new {
                            CenterId = c.CenterId,
                            Lat = (double)c.Latitude!.Value,
                            Lng = (double)c.Longitude!.Value
                        })
                        .ToList();

                    if (centersWithGeo.Any())
                    {
                        // Calculate distance to each center and find nearest
                        var centerDistances = centersWithGeo
                            .Select(c => new {
                                c.CenterId,
                                DistanceKm = HaversineKm(customerLat.Value, customerLng.Value, c.Lat, c.Lng)
                            })
                            .OrderBy(c => c.DistanceKm)
                            .ToList();

                        var nearestCenter = centerDistances.First();
                        targetCenterId = nearestCenter.CenterId;

                        _logger.LogInformation(
                            "Found nearest center {CenterId} at distance {DistanceKm:F2} km for customer {CustomerId} at ({Lat}, {Lng})",
                            nearestCenter.CenterId, nearestCenter.DistanceKm, customerUserId, customerLat.Value, customerLng.Value);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "No centers with geo coordinates found, falling back to other strategies for customer {CustomerId}",
                            customerUserId);
                    }
                }

                // Strategy 1: Check customer's recent booking (if no location-based center found)
                if (!targetCenterId.HasValue)
                {
                var customerBookings = await _bookingRepository.GetByCustomerIdAsync(customerUserId);
                var recentBooking = customerBookings
                    .Where(b => b.CenterId > 0)
                    .OrderByDescending(b => b.CreatedAt)
                    .FirstOrDefault();

                if (recentBooking != null && recentBooking.CenterId > 0)
                {
                    targetCenterId = recentBooking.CenterId;
                    _logger.LogInformation(
                        "Found recent booking for customer {CustomerId} at center {CenterId}",
                        customerUserId, targetCenterId);
                    }
                }

                // Strategy 2: Use preferred center if provided (overrides booking if set)
                if (preferredCenterId.HasValue && preferredCenterId.Value > 0)
                {
                    targetCenterId = preferredCenterId.Value;
                    _logger.LogInformation(
                        "Using preferred center {CenterId} for customer {CustomerId}",
                        preferredCenterId.Value, customerUserId);
                }

                // Strategy 3: Round-robin by workload (if no center found)
                if (!targetCenterId.HasValue)
                {
                    var allStaff = await _staffRepository.GetAllStaffAsync();
                    var activeStaff = FilterNonManagerStaff(allStaff).Where(s => s.IsActive).ToList();

                    if (!activeStaff.Any())
                    {
                        _logger.LogWarning("No active staff found for assignment");
                        return null;
                    }

                    // Calculate workload for each staff
                    var staffWorkloads = new List<(Staff Staff, int ConversationCount)>();
                    foreach (var staff in activeStaff)
                    {
                        var workload = await _conversationRepository.CountActiveConversationsByStaffIdAsync(staff.StaffId);
                        staffWorkloads.Add((staff, workload));
                    }

                    // Select staff with least workload
                    var selected = staffWorkloads
                        .OrderBy(s => s.ConversationCount)
                        .ThenBy(s => s.Staff.StaffId)
                        .First();

                    _logger.LogInformation(
                        "Assigned staff {StaffId} (Center {CenterId}) with workload {Workload} to customer {CustomerId}",
                        selected.Staff.StaffId, selected.Staff.CenterId, selected.ConversationCount, customerUserId);

                    return selected.Staff;
                }

                // Strategy 4: Find staff from target center
                if (targetCenterId.HasValue)
                {
                    var centerStaff = await _staffRepository.GetStaffByCenterIdAsync(targetCenterId.Value);
                    var activeStaff = FilterNonManagerStaff(centerStaff).Where(s => s.IsActive).ToList();

                    if (activeStaff.Any())
                    {
                        // If multiple staff in center, use round-robin
                        var staffWorkloads = new List<(Staff Staff, int ConversationCount)>();
                        foreach (var staff in activeStaff)
                        {
                            var workload = await _conversationRepository.CountActiveConversationsByStaffIdAsync(staff.StaffId);
                            staffWorkloads.Add((staff, workload));
                        }

                        var selected = staffWorkloads
                            .OrderBy(s => s.ConversationCount)
                            .ThenBy(s => s.Staff.StaffId)
                            .First();

                        _logger.LogInformation(
                            "Assigned staff {StaffId} from center {CenterId} with workload {Workload} to customer {CustomerId}",
                            selected.Staff.StaffId, targetCenterId.Value, selected.ConversationCount, customerUserId);

                        return selected.Staff;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "No active staff found in center {CenterId} for customer {CustomerId}",
                            targetCenterId.Value, customerUserId);

                        // Fallback to any active staff (excluding managers)
                        var allStaff = await _staffRepository.GetAllStaffAsync();
                        var anyActiveStaff = FilterNonManagerStaff(allStaff).Where(s => s.IsActive).FirstOrDefault();
                        return anyActiveStaff;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning staff to conversation for customer {CustomerId}", customerUserId);
                return null;
            }
        }

        private IEnumerable<Staff> FilterNonManagerStaff(IEnumerable<Staff> staffList)
        {
            return staffList.Where(s => s.User != null && s.User.Role != _chatSettings.Roles.Manager);
        }

        private async Task<(double? lat, double? lng)> GetCustomerLocationAsync(int customerUserId, decimal? requestLat, decimal? requestLng)
        {
            try
            {
                // Priority 1: Use location from request if provided
                if (requestLat.HasValue && requestLng.HasValue)
                {
                    _logger.LogInformation(
                        "Using customer location from request: ({Lat}, {Lng}) for customer {CustomerId}",
                        requestLat.Value, requestLng.Value, customerUserId);
                    return ((double)requestLat.Value, (double)requestLng.Value);
                }

                // Priority 2: Fallback to booking center location
                var customerBookings = await _bookingRepository.GetByCustomerIdAsync(customerUserId);
                var recentBooking = customerBookings
                    .Where(b => b.CenterId > 0)
                    .OrderByDescending(b => b.CreatedAt)
                    .FirstOrDefault();

                if (recentBooking != null && recentBooking.CenterId > 0)
                {
                    var center = await _centerRepository.GetCenterByIdAsync(recentBooking.CenterId);
                    if (center != null && center.Latitude.HasValue && center.Longitude.HasValue)
                    {
                        _logger.LogInformation(
                            "Using center location from recent booking: Center {CenterId} ({Lat}, {Lng}) for customer {CustomerId}",
                            center.CenterId, center.Latitude.Value, center.Longitude.Value, customerUserId);
                        return ((double)center.Latitude.Value, (double)center.Longitude.Value);
                    }
                }

                _logger.LogInformation(
                    "No location found for customer {CustomerId} (no request location and no booking with center location)",
                    customerUserId);
                return (null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer location for customer {CustomerId}", customerUserId);
                return (null, null);
            }
        }

        private static double HaversineKm(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371d;
            double dLat = (lat2 - lat1) * Math.PI / 180d;
            double dLng = (lng2 - lng1) * Math.PI / 180d;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1 * Math.PI / 180d) * Math.Cos(lat2 * Math.PI / 180d) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            return R * c;
        }
    }
}
