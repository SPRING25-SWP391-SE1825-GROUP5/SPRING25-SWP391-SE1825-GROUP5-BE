namespace EVServiceCenter.Application.Models.Responses;

public class GuestBookingResponse
{
    public int BookingId { get; set; }
    public required string BookingCode { get; set; }
    public required string CheckoutUrl { get; set; }
}


