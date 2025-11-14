namespace GreenSquirrelDev.Shared.Models;

public class Project
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public List<string> Screenshots { get; set; } = new();
    public List<string> Technologies { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LaunchedAt { get; set; }
    public string PartitionKey { get; set; } = "project";
    public bool HasExtension { get; set; }
    public string? ExtensionUrl { get; set; }
}

public enum ProjectStatus
{
    ComingSoon,
    InDevelopment,
    Live
}
