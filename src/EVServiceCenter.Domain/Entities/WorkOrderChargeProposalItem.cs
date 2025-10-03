using System;

namespace EVServiceCenter.Domain.Entities;

public partial class WorkOrderChargeProposalItem
{
    public int ProposalItemId { get; set; }

    public int ProposalId { get; set; }

    public int? VehicleModelPartId { get; set; }

    public string Description { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual WorkOrderChargeProposal Proposal { get; set; }

    public virtual VehicleModelPart? VehicleModelPart { get; set; }
}