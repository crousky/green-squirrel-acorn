namespace GreenSquirrelDev.Functions.Services;

public interface IEmailService
{
    Task<EmailResult> SendEpubToKindleAsync(string kindleEmail, string articleTitle, string sourceUrl, byte[] epubData, string epubFileName);
}

public class EmailResult
{
    public bool Success { get; set; }
    public string? MessageId { get; set; }
    public string? ErrorMessage { get; set; }
}
