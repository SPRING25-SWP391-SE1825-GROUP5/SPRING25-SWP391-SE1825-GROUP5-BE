namespace EVServiceCenter.Application.Configurations;

public class PayOsOptions
{
	public required string ClientId { get; set; }
	public required string ApiKey { get; set; }
	public required string ChecksumKey { get; set; }
	public required string BaseUrl { get; set; } = "https://api-merchant.payos.vn/v2";
    public required string ReturnUrl { get; set; } = "https://localhost:5001/payment/return";
    public required string CancelUrl { get; set; } = "https://localhost:5001/payment/cancel";
    public int MinAmount { get; set; } = 1000;
    public int DescriptionMaxLength { get; set; } = 25;
}


