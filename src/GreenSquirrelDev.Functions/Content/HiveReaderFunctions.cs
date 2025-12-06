using System.Net;
using GreenSquirrelDev.Functions.Services;
using GreenSquirrelDev.Shared.DTOs;
using GreenSquirrelDev.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace GreenSquirrelDev.Functions.Content;

public class HiveReaderFunctions
{
    private readonly ILogger<HiveReaderFunctions> _logger;
    private readonly IJwtService _jwtService;
    private readonly IUserRepository _userRepository;
    private readonly IEpubService _epubService;
    private readonly IEmailService _emailService;

    public HiveReaderFunctions(
        ILogger<HiveReaderFunctions> logger,
        IJwtService jwtService,
        IUserRepository userRepository,
        IEpubService epubService,
        IEmailService emailService)
    {
        _logger = logger;
        _jwtService = jwtService;
        _userRepository = userRepository;
        _epubService = epubService;
        _emailService = emailService;
    }

    [Function("ProcessArticle")]
    public async Task<HttpResponseData> ProcessArticle(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hive-reader/process")] HttpRequestData req)
    {
        _logger.LogInformation("Processing article for HiveReader");

        try
        {
            // 1. Auth
            var (userId, authResponse) = await ValidateUser(req);
            if (authResponse != null) return authResponse;

            // 2. Deserialize Request
            var request = await req.ReadFromJsonAsync<ProcessArticleRequest>();
            if (request == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse<string> { Success = false, Error = "Invalid request body" });
                return badResponse;
            }

            // 3. Get User Settings (Kindle Email)
            string kindleEmail = "";
            if (userId == "localtester-guid")
            {
                 kindleEmail = "tester@kindle.com"; // Mock
            }
            else
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.KindleEmail))
                {
                     var errResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await errResponse.WriteAsJsonAsync(new ApiResponse<string> { Success = false, Error = "Kindle email not configured." });
                    return errResponse;
                }
                kindleEmail = user.KindleEmail;
            }

            // 4. Generate EPUB
            var author = string.IsNullOrEmpty(request.Author) ? "Unknown" : request.Author;
            var epubBytes = await _epubService.GenerateEpubAsync(request.PageHtml, request.PageTitle, author);

            // 5. Send Email
            await _emailService.SendEpubToKindleAsync(kindleEmail, request.PageTitle, epubBytes);

            // 6. Response
            var successResponse = req.CreateResponse(HttpStatusCode.OK);
            await successResponse.WriteAsJsonAsync(new ApiResponse<ProcessArticleResponse>
            {
                Success = true,
                Data = new ProcessArticleResponse
                {
                    JobId = Guid.NewGuid().ToString(),
                    Message = "Article sent to Kindle successfully",
                    SentAt = DateTime.UtcNow
                }
            });
            return successResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing article");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
             await errorResponse.WriteAsJsonAsync(new ApiResponse<string> { Success = false, Error = "Processing failed: " + ex.Message });
            return errorResponse;
        }
    }

    [Function("GetKindleEmail")]
    public async Task<HttpResponseData> GetKindleEmail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/me/kindle-email")] HttpRequestData req)
    {
        var (userId, authResponse) = await ValidateUser(req);
        if (authResponse != null) return authResponse;

        if (userId == "localtester-guid")
        {
             var mockResponse = req.CreateResponse(HttpStatusCode.OK);
            await mockResponse.WriteAsJsonAsync(new ApiResponse<string> { Success = true, Data = "tester@kindle.com" });
            return mockResponse;
        }

        var user = await _userRepository.GetUserByIdAsync(userId);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new ApiResponse<string> { Success = true, Data = user?.KindleEmail });
        return response;
    }

    [Function("UpdateKindleEmail")]
    public async Task<HttpResponseData> UpdateKindleEmail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/me/kindle-email")] HttpRequestData req)
    {
        var (userId, authResponse) = await ValidateUser(req);
        if (authResponse != null) return authResponse;

        var request = await req.ReadFromJsonAsync<UpdateKindleEmailRequest>();
        if (request == null || string.IsNullOrEmpty(request.Email) || !request.Email.EndsWith("@kindle.com"))
        {
            var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
            await badReq.WriteAsJsonAsync(new ApiResponse<string> { Success = false, Error = "Invalid email. Must be @kindle.com" });
            return badReq;
        }

        if (userId == "localtester-guid")
        {
             // Mock update
             var mockResponse = req.CreateResponse(HttpStatusCode.OK);
            await mockResponse.WriteAsJsonAsync(new ApiResponse<string> { Success = true, Data = "Email updated (LocalTester)" });
            return mockResponse;
        }

        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            return notFound;
        }

        user.KindleEmail = request.Email;
        await _userRepository.UpdateUserAsync(user);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new ApiResponse<string> { Success = true, Data = "Kindle email updated" });
        return response;
    }

    private async Task<(string? userId, HttpResponseData? errorResponse)> ValidateUser(HttpRequestData req)
    {
        var authHeader = req.Headers.GetValues("Authorization").FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            var resp = req.CreateResponse(HttpStatusCode.Unauthorized);
            await resp.WriteAsJsonAsync(new ApiResponse<string> { Success = false, Error = "Missing token" });
            return (null, resp);
        }

        var token = authHeader.Substring("Bearer ".Length);
        if (token == "LocalTester")
        {
            return ("localtester-guid", null);
        }

        var userId = _jwtService.GetUserIdFromToken(token);
        if (string.IsNullOrEmpty(userId))
        {
             var resp = req.CreateResponse(HttpStatusCode.Unauthorized);
            await resp.WriteAsJsonAsync(new ApiResponse<string> { Success = false, Error = "Invalid token" });
            return (null, resp);
        }

        return (userId, null);
    }
}
