using Google.Apis.Auth;

namespace GreenSquirrelDev.Functions.Services;

public interface IGoogleAuthService
{
    Task<GoogleJsonWebSignature.Payload?> ValidateIdTokenAsync(string idToken);
}
