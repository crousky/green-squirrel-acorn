using System.ComponentModel.DataAnnotations;

namespace GreenSquirrelDev.Shared.DTOs;

public class ProcessArticleRequest
{
    [Required]
    public string PageHtml { get; set; } = string.Empty;

    [Required]
    public string PageTitle { get; set; } = string.Empty;

    [Required]
    public string PageUrl { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string PublishDate { get; set; } = string.Empty;
}

public class ProcessArticleResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

public class UpdateKindleEmailRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
