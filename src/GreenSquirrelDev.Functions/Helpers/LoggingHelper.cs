namespace GreenSquirrelDev.Functions.Helpers;

/// <summary>
/// Helper methods for logging to protect PII (Personally Identifiable Information)
/// </summary>
public static class LoggingHelper
{
    /// <summary>
    /// Masks an email address to protect PII while maintaining debuggability.
    /// Shows first 2 chars and last char of local part, masks the rest.
    /// Example: "user@example.com" becomes "us***r@example.com"
    /// </summary>
    /// <param name="email">Email address to mask</param>
    /// <returns>Masked email address</returns>
    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return "null";
        
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "***@***";
        
        var localPart = email[..atIndex];
        var domain = email[atIndex..];
        
        // Show first 2 chars and last char of local part, mask the rest
        if (localPart.Length <= 3)
            return $"{localPart[0]}***{domain}";
        
        return $"{localPart[..2]}***{localPart[^1]}{domain}";
    }
}
