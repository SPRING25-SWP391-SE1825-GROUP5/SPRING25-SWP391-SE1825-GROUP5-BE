using System;
using System.ComponentModel.DataAnnotations;

namespace EVServiceCenter.Application.Models.Requests;

public class GuestBookingRequest
{
    // Guest info
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có đúng 10 chữ số.")]
    public required string PhoneNumber { get; set; }

    [Required]
    [MaxLength(100)]
    public required string FullName { get; set; }

    // Vehicle info
    [MaxLength(50)]
    public required string LicensePlate { get; set; }

    [MaxLength(50)]
    public required string Vin { get; set; }

    [MaxLength(30)]
    public required string Color { get; set; }

    [Range(0, int.MaxValue)]
    public int CurrentMileage { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    // Booking
    [Required]
    public int CenterId { get; set; }

    [Required]
    public DateOnly BookingDate { get; set; }

    [Required]
    public int SlotId { get; set; }

    [Required]
    public int ServiceId { get; set; }

    public int? TechnicianId { get; set; }

    [MaxLength(500)]
    public required string SpecialRequests { get; set; }
}


