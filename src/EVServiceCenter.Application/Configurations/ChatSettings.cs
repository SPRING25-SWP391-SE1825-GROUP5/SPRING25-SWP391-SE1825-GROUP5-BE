namespace EVServiceCenter.Application.Configurations;

public class ChatSettings
{
    public const string SectionName = "Chat";

    public RoleSettings Roles { get; set; } = new();
    public MessageSettings Messages { get; set; } = new();
    public PaginationSettings Pagination { get; set; } = new();
    public SignalRSettings SignalR { get; set; } = new();
    public AssignmentSettings Assignment { get; set; } = new();
}

public class RoleSettings
{
    public string Customer { get; set; } = "CUSTOMER";
    public string Staff { get; set; } = "STAFF";
    public string Admin { get; set; } = "ADMIN";
    public string Manager { get; set; } = "MANAGER";

    public string[] StaffRoles => new[] { Staff, Admin, Manager };
}

public class MessageSettings
{
    public string NewConversationNotification { get; set; } = "Bạn có cuộc trò chuyện mới";
    public string NewAssignmentNotification { get; set; } = "Bạn được assign conversation mới";
    public string CenterReassignedTemplate { get; set; } = "Đã chuyển sang trung tâm {CenterName}";
    public string CenterReassignedWithReasonTemplate { get; set; } = "Đã chuyển sang trung tâm {CenterName}. Lý do: {Reason}";
    public string NoActiveStaffInCenter { get; set; } = "Center {CenterId} không có staff active";
}

public class PaginationSettings
{
    public int DefaultPageSize { get; set; } = 10;
    public int MaxPageSize { get; set; } = 100;
    public int DefaultPage { get; set; } = 1;
}

public class SignalRSettings
{
    public string ConversationGroupPrefix { get; set; } = "conversation:";
    public string UserGroupPrefix { get; set; } = "user:";
    public string ReceiveMessageMethod { get; set; } = "ReceiveMessage";
    public string UserTypingMethod { get; set; } = "UserTyping";
    public string NewConversationMethod { get; set; } = "NewConversation";
    public string CenterReassignedMethod { get; set; } = "CenterReassigned";
}

public class AssignmentSettings
{
    public bool AutoAssignEnabled { get; set; } = true;
    public string Strategy { get; set; } = "BookingFirst"; // BookingFirst, PreferredCenter, RoundRobin
    public bool UseWorkloadBalancing { get; set; } = true;
    public int MaxWorkloadThreshold { get; set; } = 50; // Max conversations per staff
    public int StaffResponseTimeoutMinutes { get; set; } = 1; // Timeout for auto-reassign when staff doesn't respond
}

