using System.Diagnostics;
using System.Net;
using System.Text.Json;
using GreenSquirrelDev.Functions.Services;
using GreenSquirrelDev.Shared.DTOs;
using GreenSquirrelDev.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace GreenSquirrelDev.Functions.HiveReader;

public class HiveReaderProcessFunction
{
    private readonly ILogger<HiveReaderProcessFunction> _logger;
    private readonly IJwtService _jwtService;
    private readonly IUserRepository _userRepository;
    private readonly IConversionJobRepository _conversionJobRepository;
    private readonly IEpubService _epubService;
    private readonly IEmailService _emailService;

    private const int MaxHtmlSizeBytes = 10 * 1024 * 1024; // 10MB
    private const int ProcessingTimeoutSeconds = 30;

    public HiveReaderProcessFunction(
        ILogger<HiveReaderProcessFunction> logger,
        IJwtService jwtService,
        IUserRepository userRepository,
        IConversionJobRepository conversionJobRepository,
        IEpubService epubService,
        IEmailService emailService)
    {
        _logger = logger;
        _jwtService = jwtService;
        _userRepository = userRepository;
        _conversionJobRepository = conversionJobRepository;
        _epubService = epubService;
        _emailService = emailService;
    }

    [Function("HiveReaderProcess")]
    public async Task<HttpResponseData> Process(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hive-reader/process")] HttpRequestData req)
    {
        var stopwatch = Stopwatch.StartNew();
        ConversionJob? job = null;

        try
        {
            // Validate authorization
            var userId = ValidateAndGetUserId(req);
            if (userId == null)
            {
                return await CreateErrorResponseAsync(req, HttpStatusCode.Unauthorized,
                    "UNAUTHORIZED", "Authorization header is missing or invalid.");
            }

            // Get user and validate Kindle email
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return await CreateErrorResponseAsync(req, HttpStatusCode.NotFound,
                    "USER_NOT_FOUND", "User not found.");
            }

            if (string.IsNullOrEmpty(user.KindleEmail))
            {
                return await CreateErrorResponseAsync(req, HttpStatusCode.BadRequest,
                    "KINDLE_EMAIL_NOT_CONFIGURED", "Please configure your Kindle email address in account settings.");
            }

            // Parse request
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (requestBody.Length > MaxHtmlSizeBytes)
            {
                return await CreateErrorResponseAsync(req, HttpStatusCode.RequestEntityTooLarge,
                    "CONTENT_TOO_LARGE", "Page content is too large to process.");
            }

            var processRequest = JsonSerializer.Deserialize<HiveReaderProcessRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (processRequest == null || string.IsNullOrWhiteSpace(processRequest.PageHtml))
            {
                return await CreateErrorResponseAsync(req, HttpStatusCode.BadRequest,
                    "INVALID_REQUEST", "Page HTML content is required.");
            }

            if (string.IsNullOrWhiteSpace(processRequest.PageUrl))
            {
                return await CreateErrorResponseAsync(req, HttpStatusCode.BadRequest,
                    "INVALID_REQUEST", "Page URL is required.");
            }

            var title = !string.IsNullOrWhiteSpace(processRequest.PageTitle)
                ? processRequest.PageTitle
                : "Untitled Article";

            _logger.LogInformation(
                "Processing article for user {UserId}: {Title} from {Url}",
                userId, title, processRequest.PageUrl);

            // Create conversion job
            job = await _conversionJobRepository.CreateJobAsync(new ConversionJob
            {
                UserId = userId,
                PageUrl = processRequest.PageUrl,
                PageTitle = title,
                Status = ConversionStatus.Processing
            });

            // Generate EPUB
            var epubResult = await _epubService.GenerateEpubAsync(new EpubInput
            {
                Html = processRequest.PageHtml,
                Title = title,
                Url = processRequest.PageUrl,
                Author = processRequest.Author,
                IncludeImages = user.HiveReaderSettings.IncludeImages
            });

            if (!epubResult.Success)
            {
                job.Status = ConversionStatus.Failed;
                job.ErrorMessage = epubResult.ErrorMessage;
                await _conversionJobRepository.UpdateJobAsync(job);

                return await CreateErrorResponseAsync(req, HttpStatusCode.InternalServerError,
                    "PROCESSING_FAILED", "Failed to convert article to EPUB format.");
            }

            job.EpubSizeBytes = epubResult.EpubData.Length;

            // Send email
            var emailResult = await _emailService.SendEpubToKindleAsync(
                user.KindleEmail,
                title,
                processRequest.PageUrl,
                epubResult.EpubData,
                epubResult.FileName);

            if (!emailResult.Success)
            {
                job.Status = ConversionStatus.Failed;
                job.ErrorMessage = emailResult.ErrorMessage;
                await _conversionJobRepository.UpdateJobAsync(job);

                return await CreateErrorResponseAsync(req, HttpStatusCode.InternalServerError,
                    "EMAIL_SEND_FAILED", "Failed to send article to your Kindle. Please try again.");
            }

            // Update job as successful
            stopwatch.Stop();
            job.Status = ConversionStatus.Success;
            job.SentAt = DateTime.UtcNow;
            job.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            await _conversionJobRepository.UpdateJobAsync(job);

            _logger.LogInformation(
                "Successfully processed and sent article: JobId={JobId}, ProcessingTime={ProcessingTime}ms",
                job.Id, stopwatch.ElapsedMilliseconds);

            // Return success response
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse<HiveReaderProcessResponse>
            {
                Success = true,
                Data = new HiveReaderProcessResponse
                {
                    Status = "success",
                    JobId = job.Id,
                    Message = "Article sent to your Kindle",
                    SentAt = job.SentAt
                }
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing article");

            if (job != null)
            {
                job.Status = ConversionStatus.Failed;
                job.ErrorMessage = ex.Message;
                try
                {
                    await _conversionJobRepository.UpdateJobAsync(job);
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Failed to update job status");
                }
            }

            return await CreateErrorResponseAsync(req, HttpStatusCode.InternalServerError,
                "PROCESSING_FAILED", "An error occurred while processing the article.");
        }
    }

    private string? ValidateAndGetUserId(HttpRequestData req)
    {
        try
        {
            var authHeader = req.Headers.GetValues("Authorization").FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return null;
            }

            var token = authHeader.Substring("Bearer ".Length);
            return _jwtService.GetUserIdFromToken(token);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<HttpResponseData> CreateErrorResponseAsync(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string errorCode,
        string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(new ApiResponse<HiveReaderProcessResponse>
        {
            Success = false,
            Data = new HiveReaderProcessResponse
            {
                Status = "error",
                ErrorCode = errorCode,
                Message = message
            },
            Error = message
        });
        return response;
    }
}
