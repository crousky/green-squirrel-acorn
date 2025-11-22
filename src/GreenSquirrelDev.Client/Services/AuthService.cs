using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GreenSquirrelDev.Shared.DTOs;
using Microsoft.AspNetCore.Components;

namespace GreenSquirrelDev.Client.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly NavigationManager _navigationManager;
    private ClientPrincipal? _cachedPrincipal;
    private UserProfileDTO? _cachedUserProfile;

    public AuthService(HttpClient httpClient, NavigationManager navigationManager)
    {
        _httpClient = httpClient;
        _navigationManager = navigationManager;
    }

    public Task<AuthResponse?> SignInWithGoogleAsync()
    {
        // Navigate to Azure SWA's built-in Google auth endpoint
        _navigationManager.NavigateTo("/login", forceLoad: true);
        return Task.FromResult<AuthResponse?>(null);
    }

    public Task SignOutAsync()
    {
        _cachedPrincipal = null;
        _cachedUserProfile = null;
        _navigationManager.NavigateTo("/logout", forceLoad: true);
        return Task.CompletedTask;
    }

    public async Task<UserProfileDTO?> GetCurrentUserAsync()
    {
        var principal = await GetClientPrincipalAsync();
        if (principal == null)
            return null;

        // Return cached profile if available
        if (_cachedUserProfile != null)
            return _cachedUserProfile;

        // Fetch user profile from Cosmos DB via API
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<UserProfileDTO>>("/api/user/profile");
            if (response?.Success == true && response.Data != null)
            {
                _cachedUserProfile = response.Data;
                return _cachedUserProfile;
            }
        }
        catch
        {
            // Fall back to basic info from SWA auth
        }

        // Fallback to basic profile from SWA auth
        return new UserProfileDTO
        {
            Id = principal.UserId,
            Email = principal.UserDetails,
            DisplayName = principal.UserDetails.Split('@')[0],
            Provider = principal.IdentityProvider
        };
    }

    public async Task<UserProfileDTO?> UpdateProfileAsync(string displayName)
    {
        try
        {
            var request = new { DisplayName = displayName };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PutAsync("/api/user/profile", content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileDTO>>();
                if (result?.Success == true && result.Data != null)
                {
                    _cachedUserProfile = result.Data;
                    return _cachedUserProfile;
                }
            }
        }
        catch
        {
            // Return null on error
        }

        return null;
    }

    public void ClearCache()
    {
        _cachedPrincipal = null;
        _cachedUserProfile = null;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var principal = await GetClientPrincipalAsync();
        return principal != null;
    }

    public string? GetToken()
    {
        // Azure SWA handles authentication via cookies, no explicit token needed
        return _cachedPrincipal?.UserId;
    }

    private async Task<ClientPrincipal?> GetClientPrincipalAsync()
    {
        if (_cachedPrincipal != null)
            return _cachedPrincipal;

        try
        {
            var response = await _httpClient.GetFromJsonAsync<AuthMeResponse>("/.auth/me");
            _cachedPrincipal = response?.ClientPrincipal;
            return _cachedPrincipal;
        }
        catch
        {
            return null;
        }
    }
}

public class AuthMeResponse
{
    [JsonPropertyName("clientPrincipal")]
    public ClientPrincipal? ClientPrincipal { get; set; }
}

public class ClientPrincipal
{
    [JsonPropertyName("identityProvider")]
    public string IdentityProvider { get; set; } = string.Empty;

    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("userDetails")]
    public string UserDetails { get; set; } = string.Empty;

    [JsonPropertyName("userRoles")]
    public List<string> UserRoles { get; set; } = new();
}
