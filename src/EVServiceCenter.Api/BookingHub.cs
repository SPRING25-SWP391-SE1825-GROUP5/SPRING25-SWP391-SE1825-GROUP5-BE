using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

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
}


