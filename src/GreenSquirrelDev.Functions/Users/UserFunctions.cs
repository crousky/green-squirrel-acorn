using System.Net;
using System.Text.Json;
using GreenSquirrelDev.Functions.Services;
using GreenSquirrelDev.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace GreenSquirrelDev.Functions.Users;

public class UserFunctions
{
    private readonly ILogger<UserFunctions> _logger;
    private readonly IJwtService _jwtService;
    private readonly IUserRepository _userRepository;

    public UserFunctions(
        ILogger<UserFunctions> logger,
        IJwtService jwtService,
        IUserRepository userRepository)
    {
        _logger = logger;
        _jwtService = jwtService;
        _userRepository = userRepository;
    }

    [Function("GetCurrentUser")]
    public async Task<HttpResponseData> GetCurrentUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/me")] HttpRequestData req)
    {
        try
        {
            // Get token from Authorization header
            var authHeader = req.Headers.GetValues("Authorization").FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Authorization header is missing or invalid."
                });
                return unauthorizedResponse;
            }

            var token = authHeader.Substring("Bearer ".Length);
            var userId = _jwtService.GetUserIdFromToken(token);

            if (string.IsNullOrEmpty(userId))
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Invalid or expired token."
                });
                return unauthorizedResponse;
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
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
            _logger.LogError(ex, "Error getting current user");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new ApiResponse<string>
            {
                Success = false,
                Error = "An error occurred."
            });
            return errorResponse;
        }
    }

    [Function("UpdateCurrentUser")]
    public async Task<HttpResponseData> UpdateCurrentUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/me")] HttpRequestData req)
    {
        try
        {
            // Get token from Authorization header
            var authHeader = req.Headers.GetValues("Authorization").FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Authorization header is missing or invalid."
                });
                return unauthorizedResponse;
            }

            var token = authHeader.Substring("Bearer ".Length);
            var userId = _jwtService.GetUserIdFromToken(token);

            if (string.IsNullOrEmpty(userId))
            {
                var unauthorizedResponse = req.CreateResponse(HttpStatusCode.Unauthorized);
                await unauthorizedResponse.WriteAsJsonAsync(new ApiResponse<string>
                {
                    Success = false,
                    Error = "Invalid or expired token."
                });
                return unauthorizedResponse;
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
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

            // Read update request
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updateData = JsonSerializer.Deserialize<UserProfileDTO>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (updateData != null)
            {
                // Update allowed fields
                if (!string.IsNullOrEmpty(updateData.DisplayName))
                    user.DisplayName = updateData.DisplayName;
            }

            var updatedUser = await _userRepository.UpdateUserAsync(user);

            var userProfile = new UserProfileDTO
            {
                Id = updatedUser.Id,
                Email = updatedUser.Email,
                DisplayName = updatedUser.DisplayName,
                ProfilePictureUrl = updatedUser.ProfilePictureUrl,
                CreatedAt = updatedUser.CreatedAt,
                LastLoginAt = updatedUser.LastLoginAt
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
            _logger.LogError(ex, "Error updating current user");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new ApiResponse<string>
            {
                Success = false,
                Error = "An error occurred."
            });
            return errorResponse;
        }
    }
}
