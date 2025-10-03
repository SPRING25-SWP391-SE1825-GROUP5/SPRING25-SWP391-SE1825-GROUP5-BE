using System;
using System.Collections.Generic;

namespace EVServiceCenter.Domain.Entities;

public partial class WorkOrderChargeProposal
{
    public int ProposalId { get; set; }

    public int WorkOrderId { get; set; }

    public string Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public int? ApprovedByCustomerId { get; set; }

    public int? CreatedByTechnicianId { get; set; }

    public string Note { get; set; }

    public virtual WorkOrder WorkOrder { get; set; }

    public virtual Customer? ApprovedByCustomer { get; set; }

    public virtual Technician? CreatedByTechnician { get; set; }

    public virtual ICollection<WorkOrderChargeProposalItem> WorkOrderChargeProposalItems { get; set; } = new List<WorkOrderChargeProposalItem>();
}