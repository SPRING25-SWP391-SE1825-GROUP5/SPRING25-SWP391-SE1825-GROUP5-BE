namespace EVServiceCenter.Application.Configurations;

public class GuestSessionOptions
{
    public string CookieName { get; set; } = "guest_session_id";
    public int TtlMinutes { get; set; } = 43200; // 30 days
    public bool SecureOnly { get; set; } = false; // set true in production behind https
    public string SameSite { get; set; } = "Lax"; // Lax, Strict, None
    public string Path { get; set; } = "/";
}


