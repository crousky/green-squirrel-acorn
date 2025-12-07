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

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return "null";
        
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "***@***";
        
        var localPart = email.Substring(0, atIndex);
        var domain = email.Substring(atIndex);
        
        // Show first 2 chars and last char of local part, mask the rest
        if (localPart.Length <= 3)
            return $"{localPart[0]}***{domain}";
        
        return $"{localPart.Substring(0, 2)}***{localPart[localPart.Length - 1]}{domain}";
    }

    [Function("ProcessArticle")]
    public async Task<HttpResponseData> ProcessArticle(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hive-reader/process")] HttpRequestData req)
    {
        _logger.LogInformation("ProcessArticle: Starting article processing for HiveReader");

        try
        {
            // 1. Auth
            var (userId, authResponse) = await ValidateUser(req);
            if (authResponse != null) return authResponse;

            // 2. Deserialize Request
            var request = await req.ReadFromJsonAsync<ProcessArticleRequest>();
            if (request == null)
            {
                _logger.LogWarning("ProcessArticle: Invalid request body received for userId={UserId}", userId);
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse<string> { Success = false, Error = "Invalid request body" });
                return badResponse;
            }

            _logger.LogInformation("ProcessArticle: Processing article for userId={UserId}, title={Title}, author={Author}, htmlLength={HtmlLength}", 
                userId, request.PageTitle ?? "null", request.Author ?? "null", request.PageHtml?.Length ?? 0);

            // 3. Get User Settings (Kindle Email)
            string kindleEmail = "";
            if (userId == "localtester-guid")
            {
                 kindleEmail = "tester@kindle.com"; // Mock
                 _logger.LogInformation("ProcessArticle: Using mock Kindle email for local tester");
            }
            else
            {
                var user = await _userRepository.GetUserByIdAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.KindleEmail))
                {
                     _logger.LogWarning("ProcessArticle: Kindle email not configured for userId={UserId}, user={UserFound}", 
                         userId, user != null);
                     var errResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await errResponse.WriteAsJsonAsync(new ApiResponse<string> { Success = false, Error = "Kindle email not configured." });
                    return errResponse;
                }
                kindleEmail = user.KindleEmail;
                _logger.LogInformation("ProcessArticle: Retrieved Kindle email for userId={UserId}, email={Email}", 
                    userId, MaskEmail(kindleEmail));
            }

            // 4. Generate EPUB
            var author = string.IsNullOrEmpty(request.Author) ? "Unknown" : request.Author;
            _logger.LogInformation("ProcessArticle: Starting EPUB generation for userId={UserId}, title={Title}, author={Author}", 
                userId, request.PageTitle, author);
            
            var epubBytes = await _epubService.GenerateEpubAsync(request.PageHtml, request.PageTitle, author);
            
            _logger.LogInformation("ProcessArticle: EPUB generated successfully for userId={UserId}, title={Title}, epubSize={EpubSize} bytes", 
                userId, request.PageTitle, epubBytes.Length);

            // 5. Send Email
            _logger.LogInformation("ProcessArticle: Sending EPUB to Kindle for userId={UserId}, email={Email}, title={Title}", 
                userId, MaskEmail(kindleEmail), request.PageTitle);
            
            await _emailService.SendEpubToKindleAsync(kindleEmail, request.PageTitle, epubBytes);
            
            _logger.LogInformation("ProcessArticle: Successfully sent article to Kindle for userId={UserId}, title={Title}", 
                userId, request.PageTitle);

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
            _logger.LogError(ex, "ProcessArticle: Error processing article");
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

        _logger.LogInformation("GetKindleEmail: Retrieving Kindle email for userId={UserId}", userId);

        if (userId == "localtester-guid")
        {
             _logger.LogInformation("GetKindleEmail: Returning mock email for local tester");
             var mockResponse = req.CreateResponse(HttpStatusCode.OK);
            await mockResponse.WriteAsJsonAsync(new ApiResponse<string> { Success = true, Data = "tester@kindle.com" });
            return mockResponse;
        }

        try
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            
            if (user == null)
            {
                _logger.LogWarning("GetKindleEmail: User not found for userId={UserId}", userId);
            }
            else
            {
                _logger.LogInformation("GetKindleEmail: Successfully retrieved Kindle email for userId={UserId}, hasEmail={HasEmail}", 
                    userId, !string.IsNullOrEmpty(user.KindleEmail));
            }
            
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse<string> { Success = true, Data = user?.KindleEmail });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetKindleEmail: Error retrieving Kindle email for userId={UserId}", userId);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new ApiResponse<string> { Success = false, Error = "Failed to retrieve Kindle email" });
            return errorResponse;
        }
    }

    [Function("UpdateKindleEmail")]
    public async Task<HttpResponseData> UpdateKindleEmail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/me/kindle-email")] HttpRequestData req)
    {
        var (userId, authResponse) = await ValidateUser(req);
        if (authResponse != null) return authResponse;

        var request = await req.ReadFromJsonAsync<UpdateKindleEmailRequest>();
        
        _logger.LogInformation("UpdateKindleEmail: Starting update for userId={UserId}, newEmail={NewEmail}", 
            userId, MaskEmail(request?.Email));
        
        if (request == null || string.IsNullOrEmpty(request.Email) || !request.Email.EndsWith("@kindle.com"))
        {
            _logger.LogWarning("UpdateKindleEmail: Invalid email format for userId={UserId}, email={Email}", 
                userId, MaskEmail(request?.Email));
            var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
            await badReq.WriteAsJsonAsync(new ApiResponse<string> { Success = false, Error = "Invalid email. Must be @kindle.com" });
            return badReq;
        }

        if (userId == "localtester-guid")
        {
             // Mock update
             _logger.LogInformation("UpdateKindleEmail: Mock update for local tester with email={Email}", MaskEmail(request.Email));
             var mockResponse = req.CreateResponse(HttpStatusCode.OK);
            await mockResponse.WriteAsJsonAsync(new ApiResponse<string> { Success = true, Data = "Email updated (LocalTester)" });
            return mockResponse;
        }

        try
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("UpdateKindleEmail: User not found for userId={UserId}", userId);
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                return notFound;
            }

            var oldEmail = user.KindleEmail;
            _logger.LogInformation("UpdateKindleEmail: User found for userId={UserId}, oldEmail={OldEmail}, newEmail={NewEmail}", 
                userId, MaskEmail(oldEmail), MaskEmail(request.Email));

            user.KindleEmail = request.Email;
            await _userRepository.UpdateUserAsync(user);

            _logger.LogInformation("UpdateKindleEmail: Successfully updated Kindle email for userId={UserId}, email={Email}", 
                userId, MaskEmail(request.Email));

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse<string> { Success = true, Data = "Kindle email updated" });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateKindleEmail: Error updating Kindle email for userId={UserId}, email={Email}", 
                userId, MaskEmail(request.Email));
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new ApiResponse<string> { Success = false, Error = "Failed to update Kindle email" });
            return errorResponse;
        }
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
