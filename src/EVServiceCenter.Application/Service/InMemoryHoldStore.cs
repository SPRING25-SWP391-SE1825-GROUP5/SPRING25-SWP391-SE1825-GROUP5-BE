using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using EVServiceCenter.Application.Interfaces;

namespace EVServiceCenter.Application.Service;

public class InMemoryHoldStore : IHoldStore
{
	private readonly ConcurrentDictionary<string, (int technicianId, int slotId, DateTime expiresAt, int customerId)> _holds = new();

	private static string Key(int centerId, DateOnly date, int slotId, int technicianId) => $"{centerId}:{date:yyyy-MM-dd}:{technicianId}:{slotId}";
	private static string Prefix(int centerId, DateOnly date) => $"{centerId}:{date:yyyy-MM-dd}:'";

	public bool TryHold(int centerId, DateOnly date, int slotId, int technicianId, int customerId, TimeSpan ttl, out DateTime expiresAt)
	{
		expiresAt = DateTime.UtcNow.Add(ttl);
		CleanupExpired();
		var k = Key(centerId, date, slotId, technicianId);
		return _holds.TryAdd(k, (technicianId, slotId, expiresAt, customerId));
	}

	public bool Release(int centerId, DateOnly date, int slotId, int technicianId, int customerId)
	{
		var k = Key(centerId, date, slotId, technicianId);
		if (_holds.TryGetValue(k, out var v) && v.customerId == customerId)
		{
			return _holds.TryRemove(k, out _);
		}
		return false;
	}

	public bool IsHeld(int centerId, DateOnly date, int slotId, int technicianId)
	{
		CleanupExpired();
		return _holds.ContainsKey(Key(centerId, date, slotId, technicianId));
	}

	public IReadOnlyCollection<(int technicianId, int slotId, DateTime expiresAt)> GetHolds(int centerId, DateOnly date)
	{
		CleanupExpired();
		var list = new List<(int technicianId, int slotId, DateTime expiresAt)>();
		var prefix = $"{centerId}:{date:yyyy-MM-dd}";
		foreach (var kv in _holds)
		{
			if (kv.Key.StartsWith(prefix, StringComparison.Ordinal))
			{
				list.Add((kv.Value.technicianId, kv.Value.slotId, kv.Value.expiresAt));
			}
		}
		return list;
	}

	private void CleanupExpired()
	{
		var now = DateTime.UtcNow;
		foreach (var kv in _holds)
		{
			if (kv.Value.expiresAt <= now)
			{
				_holds.TryRemove(kv.Key, out _);
			}
		}
	}
}


