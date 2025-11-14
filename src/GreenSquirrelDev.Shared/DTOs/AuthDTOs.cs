namespace GreenSquirrelDev.Shared.DTOs;

public class GoogleAuthRequest
{
    public string IdToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserProfileDTO User { get; set; } = new();
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class ExtensionAuthInitiateRequest
{
    public string ExtensionId { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
}

public class ExtensionAuthInitiateResponse
{
    public string SessionToken { get; set; } = string.Empty;
    public string AuthUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class ExtensionAuthCompleteRequest
{
    public string SessionToken { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
}

public class UserProfileDTO
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
}
