using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Customer
{
    public int CustomerId { get; set; }

    public int? UserId { get; set; }

    public string CustomerCode { get; set; }

    public string NormalizedPhone { get; set; }

    public bool IsGuest { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();

    public virtual User User { get; set; }

    public virtual ICollection<UserPromotion> UserPromotions { get; set; } = new List<UserPromotion>();

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
