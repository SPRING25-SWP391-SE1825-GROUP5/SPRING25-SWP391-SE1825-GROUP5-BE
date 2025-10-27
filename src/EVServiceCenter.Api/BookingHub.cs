using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EVServiceCenter.Api;

public class BookingHub : Hub
{
	public Task JoinCenterDateGroup(int centerId, string date)
	{
		var group = $"center:{centerId}:date:{date}";
		return Groups.AddToGroupAsync(Context.ConnectionId, group);
	}

	public Task LeaveCenterDateGroup(int centerId, string date)
	{
		var group = $"center:{centerId}:date:{date}";
		return Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
	}

	// Thêm methods cho notifications
	public Task JoinUserGroup(string userId)
	{
		return Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
	}

	public Task LeaveUserGroup(string userId)
	{
		return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
	}

	public Task JoinStaffGroup()
	{
		return Groups.AddToGroupAsync(Context.ConnectionId, "Staff");
	}

	public Task LeaveStaffGroup()
	{
		return Groups.RemoveFromGroupAsync(Context.ConnectionId, "Staff");
	}

	public override async Task OnConnectedAsync()
	{
		// Tự động join group dựa trên user role
		var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

		if (!string.IsNullOrEmpty(userId))
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
		}

		if (userRole == "STAFF" || userRole == "ADMIN" || userRole == "TECHNICIAN")
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, "Staff");
		}

		await base.OnConnectedAsync();
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		// Cleanup groups khi disconnect
		var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
		var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

		if (!string.IsNullOrEmpty(userId))
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
		}

		if (userRole == "STAFF" || userRole == "ADMIN" || userRole == "TECHNICIAN")
		{
			await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Staff");
		}

		await base.OnDisconnectedAsync(exception);
	}
}


