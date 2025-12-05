using Azure;
using Azure.Communication.Email;
using GreenSquirrelDev.Functions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GreenSquirrelDev.Functions.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly EmailClient _emailClient;
    private readonly AzureCommSettings _settings;
    private const int MaxRetries = 3;

    public EmailService(ILogger<EmailService> logger, IOptions<AzureCommSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _emailClient = new EmailClient(_settings.ConnectionString);
    }

    public async Task<EmailResult> SendEpubToKindleAsync(
        string kindleEmail,
        string articleTitle,
        string sourceUrl,
        byte[] epubData,
        string epubFileName)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < MaxRetries)
        {
            attempt++;
            try
            {
                _logger.LogInformation(
                    "Sending EPUB to Kindle (attempt {Attempt}/{MaxRetries}): {FileName} to {Email}",
                    attempt, MaxRetries, epubFileName, kindleEmail);

                var emailMessage = new EmailMessage(
                    senderAddress: _settings.SenderEmail,
                    recipientAddress: kindleEmail,
                    content: new EmailContent($"Article from HiveReader: {articleTitle}")
                    {
                        PlainText = $@"Your article has been sent to your Kindle.

Title: {articleTitle}
Source: {sourceUrl}
Sent: {DateTime.UtcNow:MMMM d, yyyy 'at' h:mm tt} UTC

Sent by HiveReader - greensquirrel.dev",
                        Html = $@"<!DOCTYPE html>
<html>
<head><title>Article from HiveReader</title></head>
<body style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <h2 style=""color: #333;"">Your article has been sent to your Kindle.</h2>
    <table style=""margin: 20px 0; border-collapse: collapse;"">
        <tr>
            <td style=""padding: 8px 0; font-weight: bold; color: #666;"">Title:</td>
            <td style=""padding: 8px 0 8px 16px;"">{System.Net.WebUtility.HtmlEncode(articleTitle)}</td>
        </tr>
        <tr>
            <td style=""padding: 8px 0; font-weight: bold; color: #666;"">Source:</td>
            <td style=""padding: 8px 0 8px 16px;""><a href=""{System.Net.WebUtility.HtmlEncode(sourceUrl)}"" style=""color: #0066cc;"">{System.Net.WebUtility.HtmlEncode(sourceUrl)}</a></td>
        </tr>
        <tr>
            <td style=""padding: 8px 0; font-weight: bold; color: #666;"">Sent:</td>
            <td style=""padding: 8px 0 8px 16px;"">{DateTime.UtcNow:MMMM d, yyyy 'at' h:mm tt} UTC</td>
        </tr>
    </table>
    <hr style=""border: none; border-top: 1px solid #eee; margin: 30px 0;""/>
    <p style=""color: #999; font-size: 12px;"">
        Sent by <a href=""https://greensquirrel.dev"" style=""color: #0066cc;"">HiveReader</a> - greensquirrel.dev
    </p>
</body>
</html>"
                    });

                // Add EPUB attachment
                var attachment = new EmailAttachment(
                    name: epubFileName,
                    contentType: "application/epub+zip",
                    content: new BinaryData(epubData));

                emailMessage.Attachments.Add(attachment);

                // Send email
                var emailSendOperation = await _emailClient.SendAsync(
                    WaitUntil.Started,
                    emailMessage);

                _logger.LogInformation(
                    "Email send initiated: OperationId={OperationId}",
                    emailSendOperation.Id);

                // Poll for completion (with timeout)
                var timeout = TimeSpan.FromMinutes(2);
                var startTime = DateTime.UtcNow;

                while (!emailSendOperation.HasCompleted && DateTime.UtcNow - startTime < timeout)
                {
                    await emailSendOperation.UpdateStatusAsync();
                    if (!emailSendOperation.HasCompleted)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                    }
                }

                if (emailSendOperation.HasCompleted)
                {
                    var result = emailSendOperation.Value;
                    if (result.Status == EmailSendStatus.Succeeded)
                    {
                        _logger.LogInformation(
                            "Email sent successfully: MessageId={MessageId}",
                            emailSendOperation.Id);

                        return new EmailResult
                        {
                            Success = true,
                            MessageId = emailSendOperation.Id
                        };
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Email send failed: Status={Status}",
                            result.Status);

                        lastException = new Exception($"Email send failed with status: {result.Status}");
                    }
                }
                else
                {
                    _logger.LogWarning("Email send operation timed out");
                    lastException = new TimeoutException("Email send operation timed out");
                }
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex,
                    "Azure Communication Service error (attempt {Attempt}): {Message}",
                    attempt, ex.Message);
                lastException = ex;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error sending email (attempt {Attempt}): {Message}",
                    attempt, ex.Message);
                lastException = ex;
            }

            // Wait before retry with exponential backoff
            if (attempt < MaxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogInformation("Retrying in {Delay} seconds...", delay.TotalSeconds);
                await Task.Delay(delay);
            }
        }

        return new EmailResult
        {
            Success = false,
            ErrorMessage = lastException?.Message ?? "Failed to send email after maximum retries"
        };
    }
}
