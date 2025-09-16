using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class ServiceCenter
{
    public int CenterId { get; set; }

    public string CenterName { get; set; }

    public string Address { get; set; }

    public string City { get; set; }

    public string PhoneNumber { get; set; }

    public string Email { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();

    public virtual ICollection<Technician> Technicians { get; set; } = new List<Technician>();

    public virtual ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}
