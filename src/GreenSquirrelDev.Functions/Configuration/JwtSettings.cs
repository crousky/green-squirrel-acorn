namespace GreenSquirrelDev.Functions.Configuration;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "https://greensquirrel.dev";
    public string Audience { get; set; } = "https://greensquirrel.dev";
    public int ExpirationMinutes { get; set; } = 1440; // 24 hours
}
