using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using GreenSquirrelDev.Functions.Services;
using GreenSquirrelDev.Shared.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace GreenSquirrelDev.Functions.HiveReader;

public class KindleEmailFunctions
{
    private readonly ILogger<KindleEmailFunctions> _logger;
    private readonly IJwtService _jwtService;
    private readonly IUserRepository _userRepository;
    private static readonly Regex KindleEmailRegex = new(@"^[a-zA-Z0-9._%+-]+@kindle\.com$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public KindleEmailFunctions(
        ILogger<KindleEmailFunctions> logger,
        IJwtService jwtService,
        IUserRepository userRepository)
    {
        _logger = logger;
        _jwtService = jwtService;
        _userRepository = userRepository;
    }

    [Function("GetKindleEmail")]
    public async Task<HttpResponseData> GetKindleEmail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users/me/kindle-email")] HttpRequestData req)
    {
        try
        {
            var userId = await ValidateAndGetUserIdAsync(req);
            if (userId == null)
            {
                return await CreateUnauthorizedResponseAsync(req);
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return await CreateNotFoundResponseAsync(req, "User not found.");
            }

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

    [Function("UpdateKindleEmail")]
    public async Task<HttpResponseData> UpdateKindleEmail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/me/kindle-email")] HttpRequestData req)
    {
        try
        {
            var userId = await ValidateAndGetUserIdAsync(req);
            if (userId == null)
            {
                return await CreateUnauthorizedResponseAsync(req);
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return await CreateNotFoundResponseAsync(req, "User not found.");
            }

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

            // Validate Kindle email format
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

    [Function("DeleteKindleEmail")]
    public async Task<HttpResponseData> DeleteKindleEmail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "users/me/kindle-email")] HttpRequestData req)
    {
        try
        {
            var userId = await ValidateAndGetUserIdAsync(req);
            if (userId == null)
            {
                return await CreateUnauthorizedResponseAsync(req);
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return await CreateNotFoundResponseAsync(req, "User not found.");
            }

            user.KindleEmail = null;
            user.KindleEmailUpdatedAt = null;

            await _userRepository.UpdateUserAsync(user);

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

    private Task<string?> ValidateAndGetUserIdAsync(HttpRequestData req)
    {
        try
        {
            var authHeader = req.Headers.GetValues("Authorization").FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                return Task.FromResult<string?>(null);
            }

            var token = authHeader.Substring("Bearer ".Length);
            var userId = _jwtService.GetUserIdFromToken(token);
            return Task.FromResult(userId);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    private static async Task<HttpResponseData> CreateUnauthorizedResponseAsync(HttpRequestData req)
    {
        var response = req.CreateResponse(HttpStatusCode.Unauthorized);
        await response.WriteAsJsonAsync(new ApiResponse<string>
        {
            Success = false,
            Error = "Authorization header is missing or invalid."
        });
        return response;
    }

    private static async Task<HttpResponseData> CreateNotFoundResponseAsync(HttpRequestData req, string message)
    {
        var response = req.CreateResponse(HttpStatusCode.NotFound);
        await response.WriteAsJsonAsync(new ApiResponse<string>
        {
            Success = false,
            Error = message
        });
        return response;
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
