namespace GreenSquirrelDev.Shared.Models;

public class ConversionJob
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public ConversionStatus Status { get; set; } = ConversionStatus.Processing;
    public string? ErrorMessage { get; set; }
    public long? EpubSizeBytes { get; set; }
    public long? ProcessingTimeMs { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string PartitionKey { get; set; } = string.Empty; // Set to UserId for partitioning
}

public enum ConversionStatus
{
    Processing,
    Success,
    Failed
}
