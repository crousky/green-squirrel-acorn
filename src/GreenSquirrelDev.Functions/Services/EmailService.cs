using Azure;
using Azure.Communication.Email;
using GreenSquirrelDev.Functions.Helpers;
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
    private readonly IEpubService _epubService;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IEpubService epubService)
    {
        _logger = logger;
        _epubService = epubService;
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
        _logger.LogInformation("EmailService: Starting email send to kindleEmail={KindleEmail}, title={Title}, epubSize={EpubSize} bytes", 
            LoggingHelper.MaskEmail(kindleEmail), title, epubContent?.Length ?? 0);
        
        if (_emailClient == null)
        {
            _logger.LogInformation("EmailService: [SIMULATION MODE] Sending email to {KindleEmail} with attachment {Title}.epub", 
                LoggingHelper.MaskEmail(kindleEmail), title);
            
            // Save to test-epub folder
            try 
            {
                var directory = Path.Combine(Directory.GetCurrentDirectory(), "test-epub");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger.LogInformation("EmailService: Created test-epub directory at {Directory}", directory);
                }

                var fileName = $"{_epubService.SanitizeFilename(title)}.epub";
                var filePath = Path.Combine(directory, fileName);
                
                await File.WriteAllBytesAsync(filePath, epubContent);
                _logger.LogInformation("EmailService: [SIMULATION] Saved EPUB to: {FilePath}, size={FileSize} bytes", 
                    filePath, epubContent.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailService: [SIMULATION] Failed to save EPUB locally for title={Title}", title);
            }
            
            return;
        }

        try
        {
            var subject = $"Article from Scurry: {title}";
            var htmlContent = $@"
                <html>
                    <body>
                        <h1>Article Sent to Kindle</h1>
                        <p><strong>Title:</strong> {title}</p>
                        <p>Sent by Scurry</p>
                    </body>
                </html>";

            _logger.LogInformation("EmailService: Creating email message with sender={Sender}, recipient={Recipient}, subject={Subject}", 
                LoggingHelper.MaskEmail(_senderAddress), LoggingHelper.MaskEmail(kindleEmail), subject);

            var emailMessage = new EmailMessage(
                senderAddress: _senderAddress,
                recipientAddress: kindleEmail,
                content: new EmailContent(subject) { Html = htmlContent, PlainText = $"Article: {title}" }
            );

            var attachmentName = $"{_epubService.SanitizeFilename(title)}.epub";
            var attachment = new EmailAttachment(
                attachmentName,
                "application/epub+zip", 
                BinaryData.FromBytes(epubContent)
            );
            
            emailMessage.Attachments.Add(attachment);
            
            _logger.LogInformation("EmailService: Email message created with attachment={AttachmentName}, attachmentSize={AttachmentSize} bytes", 
                attachmentName, epubContent.Length);

            _logger.LogInformation("EmailService: Sending email via Azure Communication Service to {KindleEmail}", LoggingHelper.MaskEmail(kindleEmail));
            EmailSendOperation emailSendOperation = await _emailClient.SendAsync(WaitUntil.Started, emailMessage);
            
            _logger.LogInformation("EmailService: Email sent successfully. OperationId={OperationId}, recipient={Recipient}, title={Title}", 
                emailSendOperation.Id, LoggingHelper.MaskEmail(kindleEmail), title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EmailService: Failed to send email to Kindle. recipient={KindleEmail}, title={Title}, epubSize={EpubSize}", 
                LoggingHelper.MaskEmail(kindleEmail), title, epubContent?.Length ?? 0);
            throw;
        }
    }
}
