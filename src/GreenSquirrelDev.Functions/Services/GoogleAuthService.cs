using Google.Apis.Auth;
using GreenSquirrelDev.Functions.Configuration;
using Microsoft.Extensions.Options;

namespace GreenSquirrelDev.Functions.Services;

public class GoogleAuthService : IGoogleAuthService
{
    private readonly GoogleAuthSettings _googleSettings;

    public GoogleAuthService(IOptions<GoogleAuthSettings> googleSettings)
    {
        _googleSettings = googleSettings.Value;
    }

    public async Task<GoogleJsonWebSignature.Payload?> ValidateIdTokenAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleSettings.ClientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return payload;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
