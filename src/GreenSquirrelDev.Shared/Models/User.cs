namespace GreenSquirrelDev.Shared.Models;

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string GoogleUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    public string PartitionKey { get; set; } = "user";
    public List<ExtensionToken> ExtensionTokens { get; set; } = new();

    // HiveReader Kindle settings
    public string? KindleEmail { get; set; }
    public DateTime? KindleEmailUpdatedAt { get; set; }
    public HiveReaderSettings HiveReaderSettings { get; set; } = new();
}

public class ExtensionToken
{
    public string ExtensionId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class HiveReaderSettings
{
    public bool EmailNotifications { get; set; } = true;
    public bool IncludeImages { get; set; } = true;
    public string FontSize { get; set; } = "medium";
}
