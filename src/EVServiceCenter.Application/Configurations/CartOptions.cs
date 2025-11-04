namespace EVServiceCenter.Application.Configurations;

public class CartOptions
{
    public const string SectionName = "Cart";
    public int TtlDays { get; set; } = 30;
    public string KeyPrefix { get; set; } = "cart:";
    public int RetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}

