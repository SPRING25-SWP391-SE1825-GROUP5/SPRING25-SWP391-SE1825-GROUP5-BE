using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class User
{
    public int UserId { get; set; }

    public string Email { get; set; }

    public string PasswordHash { get; set; }

    public string FullName { get; set; }

    public string PhoneNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Address { get; set; }

    public string? Gender { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Role { get; set; }

    public bool IsActive { get; set; }

    public bool EmailVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public byte[]? RefreshToken { get; set; }

    public int FailedLoginAttempts { get; set; }

    public DateTime? LockoutUntil { get; set; }

    public  Customer Customer { get; set; }

    public  ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public  ICollection<Otpcode> Otpcodes { get; set; } = new List<Otpcode>();

    public  ICollection<Staff> Staff { get; set; } = new List<Staff>();

    public  ICollection<Technician> Technicians { get; set; } = new List<Technician>();

    // E-commerce navigation properties
    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();
}
