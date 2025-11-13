using System.Security.Claims;

namespace GreenSquirrelDev.Functions.Services;

public interface IJwtService
{
    string GenerateToken(string userId, string email, string displayName);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    string? GetUserIdFromToken(string token);
}
