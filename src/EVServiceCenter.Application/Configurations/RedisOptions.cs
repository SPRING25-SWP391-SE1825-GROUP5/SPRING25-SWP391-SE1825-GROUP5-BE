namespace EVServiceCenter.Application.Configurations;

public class RedisOptions
{
    public const string SectionName = "Redis";
    public int ConnectTimeout { get; set; } = 5000;
    public int SyncTimeout { get; set; } = 5000;
    public int AsyncTimeout { get; set; } = 5000;
    public bool AbortOnConnectFail { get; set; } = false;
}

