using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Customer
{
    public int CustomerId { get; set; }

    public int? UserId { get; set; }

    public bool IsGuest { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual User User { get; set; }

    public virtual ICollection<UserPromotion> UserPromotions { get; set; } = new List<UserPromotion>();

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    public virtual ICollection<ServiceCredit> ServiceCredits { get; set; } = new List<ServiceCredit>();

    // E-commerce navigation properties
    

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
}
