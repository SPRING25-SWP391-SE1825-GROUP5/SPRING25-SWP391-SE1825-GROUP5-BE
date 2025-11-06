namespace EVServiceCenter.Application.Configurations;

public class BookingRealtimeOptions
{
	public int HoldTtlMinutes { get; set; }
	public string HubPath { get; set; } = "/hubs/booking";
}


