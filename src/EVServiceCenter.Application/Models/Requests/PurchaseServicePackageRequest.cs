using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class PurchaseServicePackageRequest
{
    [Required(ErrorMessage = "ID khách hàng là bắt buộc")]
    public required int CustomerId { get; set; }

    [Required(ErrorMessage = "ID gói dịch vụ là bắt buộc")]
    public required int PackageId { get; set; }

    public int? ServiceId { get; set; }

    public DateTime? ExpiryDate { get; set; }
}
