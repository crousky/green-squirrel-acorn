using System.Net;
using System.Text.Json;
using GreenSquirrelDev.Functions.Services;
using GreenSquirrelDev.Shared.DTOs;
using GreenSquirrelDev.Shared.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace GreenSquirrelDev.Functions.Auth;

public class GoogleAuthFunction
{
    private readonly ILogger<GoogleAuthFunction> _logger;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IJwtService _jwtService;
    private readonly IUserRepository _userRepository;

    public GoogleAuthFunction(
        ILogger<GoogleAuthFunction> logger,
        IGoogleAuthService googleAuthService,
        IJwtService jwtService,
        IUserRepository userRepository)
    {
        _logger = logger;
        _googleAuthService = googleAuthService;
        _jwtService = jwtService;
        _userRepository = userRepository;
    }

    [Function("GoogleAuth")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/google")] HttpRequestData req)
    {
        try
        {
            // Read request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var authRequest = JsonSerializer.Deserialize<GoogleAuthRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (authRequest == null || string.IsNullOrEmpty(authRequest.IdToken))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Invalid request. ID token is required."
                });
                return badResponse;
            }

            // Validate Google ID token
            var payload = await _googleAuthService.ValidateIdTokenAsync(authRequest.IdToken);
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

            // Check if user exists
            var existingUser = await _userRepository.GetUserByGoogleIdAsync(payload.Subject);

            User user;
            if (existingUser == null)
            {
                // Create new user
                user = new User
                {
                    GoogleUserId = payload.Subject,
                    Email = payload.Email,
                    DisplayName = payload.Name,
                    ProfilePictureUrl = payload.Picture ?? string.Empty
                };
                user = await _userRepository.CreateUserAsync(user);
                _logger.LogInformation($"Created new user: {user.Id}");
            }
            else
            {
                // Update existing user
                existingUser.LastLoginAt = DateTime.UtcNow;
                existingUser.DisplayName = payload.Name;
                existingUser.ProfilePictureUrl = payload.Picture ?? existingUser.ProfilePictureUrl;
                user = await _userRepository.UpdateUserAsync(existingUser);
                _logger.LogInformation($"Updated existing user: {user.Id}");
            }

            // Generate JWT tokens
            var token = _jwtService.GenerateToken(user.Id, user.Email, user.DisplayName);
            var refreshToken = _jwtService.GenerateRefreshToken();

            var authResponse = new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(1440),
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
            _logger.LogError(ex, "Error in GoogleAuth function");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new ApiResponse<string>
            {
                Success = false,
                Error = "An error occurred during authentication."
            });
            return errorResponse;
        }
    }
}
