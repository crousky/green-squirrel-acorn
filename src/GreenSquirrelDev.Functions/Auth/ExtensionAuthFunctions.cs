using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GreenSquirrelDev.Functions.Services;
using GreenSquirrelDev.Shared.DTOs;
using GreenSquirrelDev.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace GreenSquirrelDev.Functions.Auth;

public class ExtensionAuthFunctions
{
    private readonly ILogger<ExtensionAuthFunctions> _logger;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtService _jwtService;
    private readonly IUserRepository _userRepository;

    // In-memory storage for session tokens (in production, use Redis or Cosmos DB)
    private static readonly Dictionary<string, ExtensionAuthSession> _sessions = new();

    public ExtensionAuthFunctions(
        ILogger<ExtensionAuthFunctions> logger,
        IGoogleAuthService googleAuthService,
        IJwtService jwtService,
        IUserRepository userRepository)
    {
        _logger = logger;
        _googleAuthService = googleAuthService;
        _jwtService = jwtService;
        _userRepository = userRepository;
    }

    [Function("ExtensionAuthInitiate")]
    public async Task<HttpResponseData> InitiateExtensionAuth(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/extension/initiate")] HttpRequestData req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<ExtensionAuthInitiateRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null || string.IsNullOrEmpty(request.ExtensionId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Extension ID is required."
                });
                return badResponse;
            }

            // Generate session token
            var sessionToken = GenerateSessionToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(10); // 10-minute session window

            _sessions[sessionToken] = new ExtensionAuthSession
            {
                ExtensionId = request.ExtensionId,
                ExpiresAt = expiresAt,
                CallbackUrl = request.CallbackUrl
            };

            var authUrl = $"https://greensquirrel.dev/auth/extension?sessionToken={sessionToken}&extensionId={request.ExtensionId}";

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ExtensionAuthInitiateResponse
            {
                SessionToken = sessionToken,
                AuthUrl = authUrl,
                ExpiresAt = expiresAt
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating extension auth");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new ApiResponse<string>
            {
                Success = false,
                Error = "An error occurred."
            });
            return errorResponse;
        }
    }

    [Function("ExtensionAuthComplete")]
    public async Task<HttpResponseData> CompleteExtensionAuth(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/extension/complete")] HttpRequestData req)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<ExtensionAuthCompleteRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null || string.IsNullOrEmpty(request.SessionToken) || string.IsNullOrEmpty(request.IdToken))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Session token and ID token are required."
                });
                return badResponse;
            }

            // Validate session token
            if (!_sessions.TryGetValue(request.SessionToken, out var session) || session.ExpiresAt < DateTime.UtcNow)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Invalid or expired session token."
                });
                return unauthorizedResponse;
            }

            // Validate Google ID token
            var payload = await _googleAuthService.ValidateIdTokenAsync(request.IdToken);
            if (payload == null)
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Invalid Google ID token."
                });
                return unauthorizedResponse;
            }

            // Get or create user
            var existingUser = await _userRepository.GetUserByGoogleIdAsync(payload.Subject);
            User user;

            if (existingUser == null)
            {
                user = new User
                {
                    GoogleUserId = payload.Subject,
                    Email = payload.Email,
                    DisplayName = payload.Name,
                    ProfilePictureUrl = payload.Picture ?? string.Empty
                };
                user = await _userRepository.CreateUserAsync(user);
            }
            else
            {
                existingUser.LastLoginAt = DateTime.UtcNow;
                user = await _userRepository.UpdateUserAsync(existingUser);
            }

            // Generate extension-specific token
            var extensionToken = _jwtService.GenerateToken(user.Id, user.Email, user.DisplayName);

            // Store extension token hash in user record
            var tokenHash = HashToken(extensionToken);
            user.ExtensionTokens.Add(new ExtensionToken
            {
                ExtensionId = session.ExtensionId,
                TokenHash = tokenHash,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            });
            await _userRepository.UpdateUserAsync(user);

            // Clean up session
            _sessions.Remove(request.SessionToken);

            var authResponse = new AuthResponse
            {
                Token = extensionToken,
                RefreshToken = _jwtService.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                User = new UserProfileDTO
                {
                    Id = user.Id,
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(authResponse);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing extension auth");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new ApiResponse<string>
            {
                Success = false,
                Error = "An error occurred."
            });
            return errorResponse;
        }
    }

    private static string GenerateSessionToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
}

internal class ExtensionAuthSession
{
    public string ExtensionId { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string CallbackUrl { get; set; } = string.Empty;
}
