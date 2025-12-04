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

            // Get existing user or create if authenticated but not in database
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
                _logger.LogInformation("Created new user from SWA auth during profile update: {UserId}", user.Id);
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

public class UserKindleEmailFunction
{
    private readonly ILogger<UserKindleEmailFunction> _logger;
    private readonly IUserRepository _userRepository;
    private static readonly System.Text.RegularExpressions.Regex KindleEmailRegex =
        new(@"^[a-zA-Z0-9._%+-]+@kindle\.com$",
            System.Text.RegularExpressions.RegexOptions.Compiled |
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    public UserKindleEmailFunction(
        ILogger<UserKindleEmailFunction> logger,
        IUserRepository userRepository)
    {
        _logger = logger;
        _userRepository = userRepository;
    }

    [Function("GetUserKindleEmail")]
    public async Task<HttpResponseData> GetKindleEmail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/kindle-email")] HttpRequestData req)
    {
        try
        {
            var (user, errorResponse) = await GetAuthenticatedUserAsync(req);
            if (user == null) return errorResponse!;

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse<KindleEmailDTO>
            {
                Success = true,
                Data = new KindleEmailDTO
                {
                    KindleEmail = user.KindleEmail,
                    UpdatedAt = user.KindleEmailUpdatedAt
                }
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Kindle email");
            return await CreateErrorResponseAsync(req);
        }
    }

    [Function("UpdateUserKindleEmail")]
    public async Task<HttpResponseData> UpdateKindleEmail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "user/kindle-email")] HttpRequestData req)
    {
        try
        {
            var (user, errorResponse) = await GetAuthenticatedUserAsync(req);
            if (user == null) return errorResponse!;

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updateData = JsonSerializer.Deserialize<UpdateKindleEmailRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (updateData == null || string.IsNullOrWhiteSpace(updateData.KindleEmail))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Kindle email address is required."
                });
                return badRequestResponse;
            }

            if (!KindleEmailRegex.IsMatch(updateData.KindleEmail))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Kindle email must be a valid @kindle.com address."
                });
                return badRequestResponse;
            }

            user.KindleEmail = updateData.KindleEmail.Trim().ToLowerInvariant();
            user.KindleEmailUpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateUserAsync(user);
            _logger.LogInformation("Updated Kindle email for user: {UserId}", user.Id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse<KindleEmailDTO>
            {
                Success = true,
                Data = new KindleEmailDTO
                {
                    KindleEmail = user.KindleEmail,
                    UpdatedAt = user.KindleEmailUpdatedAt
                }
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Kindle email");
            return await CreateErrorResponseAsync(req);
        }
    }

    [Function("DeleteUserKindleEmail")]
    public async Task<HttpResponseData> DeleteKindleEmail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "user/kindle-email")] HttpRequestData req)
    {
        try
        {
            var (user, errorResponse) = await GetAuthenticatedUserAsync(req);
            if (user == null) return errorResponse!;

            user.KindleEmail = null;
            user.KindleEmailUpdatedAt = null;

            await _userRepository.UpdateUserAsync(user);
            _logger.LogInformation("Deleted Kindle email for user: {UserId}", user.Id);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new ApiResponse<string>
            {
                Success = true,
                Data = "Kindle email removed successfully."
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Kindle email");
            return await CreateErrorResponseAsync(req);
        }
    }

    private async Task<(Shared.Models.User? user, HttpResponseData? errorResponse)> GetAuthenticatedUserAsync(HttpRequestData req)
    {
        var userId = req.Headers.TryGetValues("x-ms-client-principal-id", out var userIdValues)
            ? userIdValues.FirstOrDefault()
            : null;

        if (string.IsNullOrEmpty(userId))
        {
            var response = req.CreateResponse(HttpStatusCode.Unauthorized);
            await response.WriteAsJsonAsync(new ApiResponse<string>
            {
                Success = false,
                Error = "User not authenticated."
            });
            return (null, response);
        }

        var user = await _userRepository.GetUserByGoogleIdAsync(userId);
        if (user == null)
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            await response.WriteAsJsonAsync(new ApiResponse<string>
            {
                Success = false,
                Error = "User not found. Please complete your profile setup first."
            });
            return (null, response);
        }

        return (user, null);
    }

    private static async Task<HttpResponseData> CreateErrorResponseAsync(HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.InternalServerError);
        await response.WriteAsJsonAsync(new ApiResponse<string>
        {
            Success = false,
            Error = "An error occurred."
        });
        return response;
    }
}
