using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GreenSquirrelDev.Functions.Services;

public interface IEmailService
{
    Task SendEpubToKindleAsync(string kindleEmail, string title, string epubPath);
    Task SendEpubToKindleAsync(string kindleEmail, string title, byte[] epubContent);
}

public class EmailService : IEmailService
{
    private readonly EmailClient? _emailClient;
    private readonly ILogger<EmailService> _logger;
    private readonly string _senderAddress;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;
        _senderAddress = configuration["AzureCommunicationService:SenderAddress"] ?? "noreply@greensquirrel.dev";
        var connectionString = configuration["AzureCommunicationService:ConnectionString"];
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            _emailClient = new EmailClient(connectionString);
        }
        else
        {
             _logger.LogWarning("Azure Communication Service ConnectionString is missing. Email sending will be simulated.");
        }
    }

    public async Task SendEpubToKindleAsync(string kindleEmail, string title, string epubPath)
    {
        var bytes = await File.ReadAllBytesAsync(epubPath);
         await SendEpubToKindleAsync(kindleEmail, title, bytes);
    }

    public async Task SendEpubToKindleAsync(string kindleEmail, string title, byte[] epubContent)
    {
        if (_emailClient == null)
        {
            _logger.LogInformation($"[SIMULATION] Sending email to {kindleEmail} with attachment {title}.epub");
            
            // Save to test-epub folder
            try 
            {
                var directory = Path.Combine(Directory.GetCurrentDirectory(), "test-epub");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var fileName = $"{SanitizeFileName(title)}.epub";
                var filePath = Path.Combine(directory, fileName);
                
                await File.WriteAllBytesAsync(filePath, epubContent);
                _logger.LogInformation($"[SIMULATION] Saved EPUB to: {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[SIMULATION] Failed to save EPUB locally.");
            }
            
            return;
        }

        try
        {
            var subject = $"Article from HiveReader: {title}";
            var htmlContent = $@"
                <html>
                    <body>
                        <h1>Article Sent to Kindle</h1>
                        <p><strong>Title:</strong> {title}</p>
                        <p>Sent by HiveReader</p>
                    </body>
                </html>";

            var emailMessage = new EmailMessage(
                senderAddress: _senderAddress,
                recipientAddress: kindleEmail,
                content: new EmailContent(subject) { Html = htmlContent, PlainText = $"Article: {title}" }
            );

            var attachmentName = $"{SanitizeFileName(title)}.epub";
            var attachment = new EmailAttachment(
                attachmentName,
                "application/epub+zip", 
                BinaryData.FromBytes(epubContent)
            );
            
            emailMessage.Attachments.Add(attachment);

            EmailSendOperation emailSendOperation = await _emailClient.SendAsync(WaitUntil.Started, emailMessage);
            _logger.LogInformation($"Email sent. OperationId: {emailSendOperation.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to Kindle");
            throw;
        }
    }

    private string SanitizeFileName(string fileName)
    {
        return string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
    }
}
