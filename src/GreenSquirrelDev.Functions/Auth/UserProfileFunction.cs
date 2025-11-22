using System.Net;
using System.Text.Json;
using GreenSquirrelDev.Functions.Services;
using GreenSquirrelDev.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace GreenSquirrelDev.Functions.Auth;

public class UserProfileFunction
{
    private readonly ILogger<UserProfileFunction> _logger;
    private readonly IUserRepository _userRepository;

    public UserProfileFunction(
        ILogger<UserProfileFunction> logger,
        IUserRepository userRepository)
    {
        _logger = logger;
        _userRepository = userRepository;
    }

    [Function("GetUserProfile")]
    public async Task<HttpResponseData> GetProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/profile")] HttpRequestData req)
    {
        try
        {
            // Get user ID from Azure SWA auth header (x-ms-client-principal-id)
            var userId = req.Headers.TryGetValues("x-ms-client-principal-id", out var userIdValues)
                ? userIdValues.FirstOrDefault()
                : null;

            if (string.IsNullOrEmpty(userId))
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = false,
                    Error = "User not authenticated."
                });
                return unauthorizedResponse;
            }

            // Azure SWA uses the identity provider's user ID as the principal ID for Google auth
            var user = await _userRepository.GetUserByGoogleIdAsync(userId);

            if (user == null)
            {
                // User authenticated but not in our database yet - create them
                var name = req.Headers.TryGetValues("x-ms-client-principal-name", out var nameValues)
                    ? nameValues.FirstOrDefault() ?? string.Empty
                    : string.Empty;

                user = new Shared.Models.User
                {
                    GoogleUserId = userId,
                    Email = name, // SWA provides email as the principal name for Google
                    DisplayName = name.Split('@')[0], // Default display name from email
                    ProfilePictureUrl = string.Empty
                };
                user = await _userRepository.CreateUserAsync(user);
                _logger.LogInformation("Created new user from SWA auth: {UserId}", user.Id);
            }

            var userProfile = new UserProfileDTO
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse<UserProfileDTO>
            {
                Success = true,
                Data = userProfile
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetUserProfile function");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new ApiResponse<string>
            {
                Success = false,
                Error = "An error occurred while fetching user profile."
            });
            return errorResponse;
        }
    }

    [Function("UpdateUserProfile")]
    public async Task<HttpResponseData> UpdateProfile(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "user/profile")] HttpRequestData req)
    {
        try
        {
            // Get user ID from Azure SWA auth header
            var userId = req.Headers.TryGetValues("x-ms-client-principal-id", out var userIdValues)
                ? userIdValues.FirstOrDefault()
                : null;

            if (string.IsNullOrEmpty(userId))
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = false,
                    Error = "User not authenticated."
                });
                return unauthorizedResponse;
            }

            // Read request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updateRequest = JsonSerializer.Deserialize<UpdateProfileRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (updateRequest == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Invalid request body."
                });
                return badResponse;
            }

            // Get existing user
            var user = await _userRepository.GetUserByGoogleIdAsync(userId);

            if (user == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = false,
                    Error = "User not found."
                });
                return notFoundResponse;
            }

            // Update display name if provided
            if (!string.IsNullOrWhiteSpace(updateRequest.DisplayName))
            {
                user.DisplayName = updateRequest.DisplayName.Trim();
            }

            user = await _userRepository.UpdateUserAsync(user);
            _logger.LogInformation("Updated user profile: {UserId}", user.Id);

            var userProfile = new UserProfileDTO
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                ProfilePictureUrl = user.ProfilePictureUrl,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse<UserProfileDTO>
            {
                Success = true,
                Data = userProfile
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateUserProfile function");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new ApiResponse<string>
            {
                Success = false,
                Error = "An error occurred while updating user profile."
            });
            return errorResponse;
        }
    }
}

public class UpdateProfileRequest
{
    public string? DisplayName { get; set; }
}
