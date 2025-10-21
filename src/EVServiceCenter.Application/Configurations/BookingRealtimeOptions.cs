namespace EVServiceCenter.Application.Configurations;

public class BookingRealtimeOptions
{
	public int HoldTtlMinutes { get; set; } = 5;
	public string HubPath { get; set; } = "/hubs/booking";
}


