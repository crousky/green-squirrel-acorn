namespace GreenSquirrelDev.Functions.Services;

public interface IEpubService
{
    Task<EpubResult> GenerateEpubAsync(EpubInput input);
}

public class EpubInput
{
    public string Html { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Author { get; set; }
    public bool IncludeImages { get; set; } = true;
}

public class EpubResult
{
    public bool Success { get; set; }
    public byte[] EpubData { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public string? ErrorMessage { get; set; }
}
