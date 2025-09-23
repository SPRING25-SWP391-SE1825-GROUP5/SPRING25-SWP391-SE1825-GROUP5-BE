namespace EVServiceCenter.Application.Configurations;

public class PayOsOptions
{
	public string ClientId { get; set; }
	public string ApiKey { get; set; }
	public string ChecksumKey { get; set; }
	public string BaseUrl { get; set; } = "https://api-merchant.payos.vn/v2";
    public string ReturnUrl { get; set; } = "https://localhost:5001/payment/return";
    public string CancelUrl { get; set; } = "https://localhost:5001/payment/cancel";
}


