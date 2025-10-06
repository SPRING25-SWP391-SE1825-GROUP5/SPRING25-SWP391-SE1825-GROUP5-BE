namespace EVServiceCenter.Application.Interfaces;

public interface IHoldStore
{
	bool TryHold(int centerId, System.DateOnly date, int slotId, int technicianId, int customerId, System.TimeSpan ttl, out System.DateTime expiresAt);
	bool Release(int centerId, System.DateOnly date, int slotId, int technicianId, int customerId);
	bool IsHeld(int centerId, System.DateOnly date, int slotId, int technicianId);
	System.Collections.Generic.IReadOnlyCollection<(int technicianId, int slotId, System.DateTime expiresAt)> GetHolds(int centerId, System.DateOnly date);
}


