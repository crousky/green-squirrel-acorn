using System.Net.Http.Json;
using System.Text.Json;
using GreenSquirrelDev.Shared.DTOs;
using Microsoft.JSInterop;

namespace GreenSquirrelDev.Client.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "authToken";
    private const string RefreshTokenKey = "refreshToken";

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public async Task<AuthResponse?> SignInWithGoogleAsync()
    {
        try
        {
            // This will trigger Google Sign-In flow via JavaScript interop
            var idToken = await _jsRuntime.InvokeAsync<string>("googleSignIn");

            if (string.IsNullOrEmpty(idToken))
                return null;

            var request = new GoogleAuthRequest { IdToken = idToken };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/google", request);

            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse != null)
                {
                    await SaveTokensAsync(authResponse.Token, authResponse.RefreshToken);
                    return authResponse;
                }
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task SignOutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
    }

    public async Task<UserProfileDTO?> GetCurrentUserAsync()
    {
        var token = await GetStoredTokenAsync();
        if (string.IsNullOrEmpty(token))
            return null;

        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetFromJsonAsync<ApiResponse<UserProfileDTO>>("/api/users/me");
            return response?.Data;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetStoredTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public string? GetToken()
    {
        // Synchronous version - only use when already loaded
        return null; // Will be populated by GetStoredTokenAsync
    }

    private async Task SaveTokensAsync(string token, string refreshToken)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, refreshToken);
    }

    private async Task<string?> GetStoredTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
        }
        catch
        {
            return null;
        }
    }
}
