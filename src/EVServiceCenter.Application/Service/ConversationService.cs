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
                foreach (var memberRequest in request.Members)
                {
                    if (!string.IsNullOrWhiteSpace(memberRequest.GuestSessionId))
                    {
                        memberRequest.GuestSessionId = memberRequest.GuestSessionId.Trim();
                    }
                    else
                    {
                        memberRequest.GuestSessionId = null;
                    }
                }

                bool hasValidMember = request.Members.Any(m =>
                    m.UserId.HasValue || !string.IsNullOrWhiteSpace(m.GuestSessionId));

                if (!hasValidMember)
                {
                    throw new ArgumentException("Conversation must have at least one member with either UserId or GuestSessionId");
                }

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

                var conversation = new Conversation
                {
                    Subject = request.Subject,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdConversation = await _conversationRepository.CreateConversationAsync(conversation);

                foreach (var memberRequest in request.Members)
                {
                    if (_chatSettings.Roles.StaffRoles.Contains(memberRequest.RoleInConversation)
                        && !memberRequest.UserId.HasValue)
                    {
                        continue;
                    }

                    string? normalizedGuestSessionId = string.IsNullOrWhiteSpace(memberRequest.GuestSessionId)
                        ? null
                        : memberRequest.GuestSessionId.Trim();

                    bool hasUserId = memberRequest.UserId.HasValue;
                    bool hasGuestSessionId = !string.IsNullOrWhiteSpace(normalizedGuestSessionId);

                    if (!hasUserId && !hasGuestSessionId)
                    {
                        continue;
                    }

                    var member = new ConversationMember
                    {
                        ConversationId = createdConversation.ConversationId,
                        UserId = memberRequest.UserId,
                        GuestSessionId = normalizedGuestSessionId,
                        RoleInConversation = memberRequest.RoleInConversation,
                        LastReadAt = null
                    };

                    await _conversationMemberRepository.CreateConversationMemberAsync(member);
                }

                bool hasStaffMember = request.Members.Any(m =>
                    _chatSettings.Roles.StaffRoles.Contains(m.RoleInConversation)
                    && m.UserId.HasValue);

                if (!hasStaffMember && customerUserId.HasValue && _chatSettings.Assignment.AutoAssignEnabled)
                {
                    Staff? assignedStaff = null;

                    if (request.PreferredStaffId.HasValue)
                    {
                        assignedStaff = await _staffRepository.GetStaffByIdAsync(request.PreferredStaffId.Value);

                        if (assignedStaff != null && assignedStaff.IsActive &&
                            assignedStaff.User != null && assignedStaff.User.Role == "STAFF")
                        {
                        }
                        else
                        {
                            assignedStaff = null;
                        }
                    }

                    if (assignedStaff == null)
                    {
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

                        var staffMember = new ConversationMember
                        {
                            ConversationId = createdConversation.ConversationId,
                            UserId = assignedStaff.UserId,
                            RoleInConversation = _chatSettings.Roles.Staff,
                            LastReadAt = null
                        };
                        await _conversationMemberRepository.CreateConversationMemberAsync(staffMember);

                        await _chatHubService.NotifyNewConversationAsync(assignedStaff.UserId, createdConversation.ConversationId);
                    }
                }

                return await MapToConversationResponseAsync(createdConversation);
            }
            catch
            {
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
            catch
            {
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
            catch
            {
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

                await _conversationRepository.DeleteConversationAsync(conversationId);

                return true;
            }
            catch
            {
                throw;
            }
        }

        public async Task<ConversationResponse> GetOrCreateConversationAsync(GetOrCreateConversationRequest request)
        {
            try
            {
                var existingConversation = await _conversationRepository.GetConversationByMembersAsync(
                    request.Member1.UserId,
                    request.Member2.UserId,
                    request.Member1.GuestSessionId,
                    request.Member2.GuestSessionId);

                if (existingConversation != null)
                {
                    return await MapToConversationResponseAsync(existingConversation);
                }

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
            catch
            {
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
            catch
            {
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
            catch
            {
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
            catch
            {
                throw;
            }
        }

        public async Task<ConversationMemberResponse> AddMemberToConversationAsync(long conversationId, AddMemberToConversationRequest request)
        {
            try
            {
                await Task.CompletedTask;
                return new ConversationMemberResponse
                {
                    ConversationId = conversationId,
                    UserId = request.UserId,
                    GuestSessionId = request.GuestSessionId,
                    RoleInConversation = request.RoleInConversation
                };
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> RemoveMemberFromConversationAsync(long conversationId, int? userId = null, string? guestSessionId = null)
        {
            try
            {
                return await _conversationMemberRepository.RemoveMemberFromConversationAsync(conversationId, userId, guestSessionId);
            }
            catch
            {
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
            catch
            {
                throw;
            }
        }

        public async Task<bool> UpdateMemberRoleAsync(long conversationId, int? userId, string? guestSessionId, string newRole)
        {
            try
            {
                await Task.CompletedTask;
                return true;
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> ConversationExistsAsync(long conversationId)
        {
            try
            {
                return await _conversationRepository.ConversationExistsAsync(conversationId);
            }
            catch
            {
                throw;
            }
        }

        public async Task<int> CountConversationsAsync(string? searchTerm = null)
        {
            try
            {
                return await _conversationRepository.CountConversationsAsync(searchTerm);
            }
            catch
            {
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

                try
                {
                    await _chatHubService.NotifyMessageReadAsync(conversationId, userId, guestSessionId, lastReadAt);
                }
                catch
                {
                }

                return await MapToConversationResponseAsync(conversation);
            }
            catch
            {
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

                return await MapToConversationResponseAsync(conversation);
            }
            catch
            {
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
            catch
            {
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
            catch
            {
                throw;
            }
        }

        public async Task<List<object>> GetStaffByCenterAsync(int centerId)
        {
            try
            {
                var staffList = await _staffRepository.GetStaffByCenterIdAsync(centerId);

                var staffOnly = staffList
                    .Where(s => s.User != null &&
                               s.User.Role == "STAFF" &&
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
            catch
            {
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

            if (conversation.AssignedStaff != null)
            {
                response.AssignedStaffName = conversation.AssignedStaff.User?.FullName;
                response.AssignedStaffEmail = conversation.AssignedStaff.User?.Email;
                response.AssignedCenterId = conversation.AssignedStaff.CenterId;
                response.AssignedCenterName = conversation.AssignedStaff.Center?.CenterName;
            }

            if (conversation.LastMessage != null)
            {
                response.LastMessage = await MapToMessageResponseAsync(conversation.LastMessage);
            }

            if (conversation.ConversationMembers.Any())
            {
                var members = new List<ConversationMemberResponse>();
                foreach (var member in conversation.ConversationMembers)
                {
                    members.Add(await MapToConversationMemberResponseAsync(member));
                }
                response.Members = members;
            }

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

            if (member.UserId.HasValue)
            {
                if (member.User != null)
                {
                    response.UserName = member.User.FullName;
                    response.UserEmail = member.User.Email;
                }
                else
                {
                    var user = await _authRepository.GetUserByIdAsync(member.UserId.Value);
                    if (user != null)
                    {
                        response.UserName = user.FullName;
                        response.UserEmail = user.Email;
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

            var attachments = new List<AttachmentResponse>();
            if (!string.IsNullOrEmpty(message.AttachmentUrl))
            {
                try
                {
                    var urls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(message.AttachmentUrl);
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
            }
            else if (!string.IsNullOrEmpty(message.SenderGuestSessionId))
            {
                response.SenderName = "Guest User";
            }

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
                        var centerDistances = centersWithGeo
                            .Select(c => new {
                                c.CenterId,
                                DistanceKm = HaversineKm(customerLat.Value, customerLng.Value, c.Lat, c.Lng)
                            })
                            .OrderBy(c => c.DistanceKm)
                            .ToList();

                        var nearestCenter = centerDistances.First();
                        targetCenterId = nearestCenter.CenterId;
                    }
                }

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
                }
                }

                if (preferredCenterId.HasValue && preferredCenterId.Value > 0)
                {
                    targetCenterId = preferredCenterId.Value;
                }

                if (!targetCenterId.HasValue)
                {
                    var allStaff = await _staffRepository.GetAllStaffAsync();
                    var activeStaff = FilterNonManagerStaff(allStaff).Where(s => s.IsActive).ToList();

                    if (!activeStaff.Any())
                    {
                        return null;
                    }

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

                    return selected.Staff;
                }

                if (targetCenterId.HasValue)
                {
                    var centerStaff = await _staffRepository.GetStaffByCenterIdAsync(targetCenterId.Value);
                    var activeStaff = FilterNonManagerStaff(centerStaff).Where(s => s.IsActive).ToList();

                    if (activeStaff.Any())
                    {
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

                        return selected.Staff;
                    }
                    else
                    {
                        var allStaff = await _staffRepository.GetAllStaffAsync();
                        return FilterNonManagerStaff(allStaff).Where(s => s.IsActive).FirstOrDefault();
                    }
                }

                return null;
            }
            catch
            {
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
                if (requestLat.HasValue && requestLng.HasValue)
                {
                    return ((double)requestLat.Value, (double)requestLng.Value);
                }

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
                        return ((double)center.Latitude.Value, (double)center.Longitude.Value);
                    }
                }

                return (null, null);
            }
            catch
            {
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
