using GreenSquirrelDev.Shared.DTOs;

namespace GreenSquirrelDev.Client.Services;

public interface IAuthService
{
    Task<AuthResponse?> SignInWithGoogleAsync();
    Task SignOutAsync();
    Task<UserProfileDTO?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
    string? GetToken();
    Task<UserProfileDTO?> UpdateProfileAsync(string displayName);
    void ClearCache();
}
