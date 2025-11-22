using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GreenSquirrelDev.Shared.DTOs;
using Microsoft.AspNetCore.Components;

namespace GreenSquirrelDev.Client.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly NavigationManager _navigationManager;
    private ClientPrincipal? _cachedPrincipal;

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
        _navigationManager.NavigateTo("/logout", forceLoad: true);
        return Task.CompletedTask;
    }

    public async Task<UserProfileDTO?> GetCurrentUserAsync()
    {
        var principal = await GetClientPrincipalAsync();
        if (principal == null)
            return null;

        return new UserProfileDTO
        {
            Id = principal.UserId,
            Email = principal.UserDetails,
            DisplayName = principal.UserDetails,
            Provider = principal.IdentityProvider
        };
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
