using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class Technician
{
    public int TechnicianId { get; set; }

    public int UserId { get; set; }

    public int CenterId { get; set; }

    public string TechnicianCode { get; set; }

    public string Specialization { get; set; }

    public int ExperienceYears { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ServiceCenter Center { get; set; }

    public virtual ICollection<TechnicianTimeSlot> TechnicianTimeSlots { get; set; } = new List<TechnicianTimeSlot>();

    public virtual User User { get; set; }

    public virtual ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
