using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

// Base feedback request - chỉ chứa thông tin cần thiết cho feedback
public class CreateFeedbackRequest
{
    [Range(1,5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public bool IsAnonymous { get; set; }

    [Required]
    public int CustomerId { get; set; }

    // Các fields này chỉ dùng cho endpoint generic (POST /api/Feedback)
    public int? PartId { get; set; }
    public int? TechnicianId { get; set; }
    public int? OrderId { get; set; }
    public int? WorkOrderId { get; set; }
}

// Request cho Order Part Feedback - OrderId và PartId từ path parameters
public class CreateOrderPartFeedbackRequest
{
    [Range(1,5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public bool IsAnonymous { get; set; }

    [Required]
    public int CustomerId { get; set; }
    // OrderId và PartId được lấy từ path parameters
}

// Request cho WorkOrder Part Feedback - WorkOrderId và PartId từ path parameters
public class CreateWorkOrderPartFeedbackRequest
{
    [Range(1,5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public bool IsAnonymous { get; set; }

    [Required]
    public int CustomerId { get; set; }
    // WorkOrderId và PartId được lấy từ path parameters
}

// Request cho WorkOrder Technician Feedback - WorkOrderId và TechnicianId từ path parameters
public class CreateWorkOrderTechnicianFeedbackRequest
{
    [Range(1,5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public bool IsAnonymous { get; set; }

    [Required]
    public int CustomerId { get; set; }
    // WorkOrderId và TechnicianId được lấy từ path parameters
}

public class UpdateFeedbackRequest
{
    [Range(1,5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public bool IsAnonymous { get; set; }
}


// Public feedback (không yêu cầu mua hàng): cho phụ tùng
public class PublicPartFeedbackRequest
{
    [Range(1,5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public bool IsAnonymous { get; set; }

    // Cho phép ẩn danh nên CustomerId có thể null
    public int? CustomerId { get; set; }
}

// Public feedback (không yêu cầu mua hàng): cho kỹ thuật viên
public class PublicTechnicianFeedbackRequest
{
    [Range(1,5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public bool IsAnonymous { get; set; }

    public int? CustomerId { get; set; }
}


