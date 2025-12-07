using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Xml.Linq;

namespace GreenSquirrelDev.Functions.Services;

public interface IEpubService
{
    Task<byte[]> GenerateEpubAsync(string htmlContent, string title, string author);
    string SanitizeFilename(string filename);
}

public class EpubService : IEpubService
{
    private readonly ILogger<EpubService> _logger;

    public EpubService(ILogger<EpubService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> GenerateEpubAsync(string htmlContent, string title, string author)
    {
        _logger.LogInformation($"Generating EPUB for {title}");
        
        // Clean HTML content - strip out everything except main article content
        var cleanedHtml = CleanHtmlContent(htmlContent);
        
        using (var memoryStream = new MemoryStream())
        {
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                // mimetype (must be first, uncompressed)
                var mimetypeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
                using (var stream = mimetypeEntry.Open())
                using (var writer = new StreamWriter(stream, Encoding.ASCII))
                {
                    await writer.WriteAsync("application/epub+zip");
                }

                // container.xml
                var containerEntry = archive.CreateEntry("META-INF/container.xml");
                using (var stream = containerEntry.Open())
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    await writer.WriteAsync(
                        "<?xml version=\"1.0\"?>" +
                        "<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">" +
                        "<rootfiles>" +
                        "<rootfile full-path=\"OEBPS/content.opf\" media-type=\"application/oebps-package+xml\"/>" +
                        "</rootfiles>" +
                        "</container>");
                }

                // OEBPS/content.opf
                var opfEntry = archive.CreateEntry("OEBPS/content.opf");
                using (var stream = opfEntry.Open())
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    var contentOpf = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" unique-identifier=""BookId"" version=""3.0"">
    <metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"">
        <dc:title>{title}</dc:title>
        <dc:creator>{author}</dc:creator>
        <dc:language>en</dc:language>
        <dc:identifier id=""BookId"">urn:uuid:{Guid.NewGuid()}</dc:identifier>
        <meta property=""dcterms:modified"">{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</meta>
    </metadata>
    <manifest>
        <item id=""toc"" href=""toc.xhtml"" media-type=""application/xhtml+xml"" properties=""nav""/>
        <item id=""content"" href=""content.xhtml"" media-type=""application/xhtml+xml""/>
    </manifest>
    <spine>
        <itemref idref=""content""/>
    </spine>
</package>";
                    await writer.WriteAsync(contentOpf);
                }

                // OEBPS/toc.xhtml
                var tocEntry = archive.CreateEntry("OEBPS/toc.xhtml");
                using (var stream = tocEntry.Open())
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                   var toc = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE html>
<html xmlns=""http://www.w3.org/1999/xhtml"" xmlns:epub=""http://www.idpf.org/2007/ops"">
<head><title>{title}</title></head>
<body>
<nav epub:type=""toc"">
    <ol>
        <li><a href=""content.xhtml"">Start</a></li>
    </ol>
</nav>
</body>
</html>";
                    await writer.WriteAsync(toc);
                }

                // OEBPS/content.xhtml
                var contentEntry = archive.CreateEntry("OEBPS/content.xhtml");
                using (var stream = contentEntry.Open())
                using (var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    // Basic cleanup of the HTML to ensure it's valid XHTML for EPUB
                    // In a real app we'd need robust parsing.
                    // Here we wrap in standard XHTML structure
                    var xhtml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE html>
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
    <title>{title}</title>
    <style>
        img {{ max-width: 100%; }}
        body {{ font-family: serif; }}
    </style>
</head>
<body>
    <h1>{title}</h1>
    <p><em>By {author}</em></p>
    <hr/>
    {cleanedHtml}
</body>
</html>";
                   await writer.WriteAsync(xhtml);
                }
            }
            return memoryStream.ToArray();
        }
    }

    private string CleanHtmlContent(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return string.Empty;

        // Remove script tags and their content
        htmlContent = Regex.Replace(htmlContent, @"<script[^>]*>.*?</script>", string.Empty, 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Remove style tags and their content
        htmlContent = Regex.Replace(htmlContent, @"<style[^>]*>.*?</style>", string.Empty, 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Remove common non-content elements by class/id patterns
        var nonContentPatterns = new[]
        {
            @"<nav[^>]*>.*?</nav>",
            @"<header[^>]*>.*?</header>",
            @"<footer[^>]*>.*?</footer>",
            @"<aside[^>]*>.*?</aside>",
            @"<div[^>]*class=[""'][^""']*nav[^""']*[""'][^>]*>.*?</div>",
            @"<div[^>]*class=[""'][^""']*menu[^""']*[""'][^>]*>.*?</div>",
            @"<div[^>]*class=[""'][^""']*sidebar[^""']*[""'][^>]*>.*?</div>",
            @"<div[^>]*class=[""'][^""']*ad[^""']*[""'][^>]*>.*?</div>",
            @"<div[^>]*class=[""'][^""']*comment[^""']*[""'][^>]*>.*?</div>",
            @"<div[^>]*class=[""'][^""']*share[^""']*[""'][^>]*>.*?</div>",
            @"<div[^>]*class=[""'][^""']*social[^""']*[""'][^>]*>.*?</div>",
            @"<div[^>]*id=[""'][^""']*nav[^""']*[""'][^>]*>.*?</div>",
            @"<div[^>]*id=[""'][^""']*menu[^""']*[""'][^>]*>.*?</div>",
            @"<div[^>]*id=[""'][^""']*sidebar[^""']*[""'][^>]*>.*?</div>",
            @"<div[^>]*id=[""'][^""']*comment[^""']*[""'][^>]*>.*?</div>",
            @"<iframe[^>]*>.*?</iframe>",
            @"<noscript[^>]*>.*?</noscript>",
        };

        foreach (var pattern in nonContentPatterns)
        {
            htmlContent = Regex.Replace(htmlContent, pattern, string.Empty, 
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        // Remove inline onclick, onload, etc. event handlers
        htmlContent = Regex.Replace(htmlContent, @"\s+on\w+\s*=\s*[""'][^""']*[""']", string.Empty, 
            RegexOptions.IgnoreCase);

        // Clean up attributes from images - keep only src, alt, title, and width/height
        htmlContent = Regex.Replace(htmlContent, 
            @"<img([^>]*?)>", 
            match => CleanImageTag(match.Value), 
            RegexOptions.IgnoreCase);

        // Remove empty tags
        htmlContent = Regex.Replace(htmlContent, @"<(\w+)[^>]*>\s*</\1>", string.Empty, 
            RegexOptions.IgnoreCase);

        // Remove excessive whitespace
        htmlContent = Regex.Replace(htmlContent, @"\s{2,}", " ");
        htmlContent = Regex.Replace(htmlContent, @">\s+<", "><");

        return htmlContent.Trim();
    }

    private string CleanImageTag(string imgTag)
    {
        // Extract only essential attributes: src, alt, title, width, height
        var src = Regex.Match(imgTag, @"src\s*=\s*[""']([^""']*)[""']", RegexOptions.IgnoreCase);
        var alt = Regex.Match(imgTag, @"alt\s*=\s*[""']([^""']*)[""']", RegexOptions.IgnoreCase);
        var title = Regex.Match(imgTag, @"title\s*=\s*[""']([^""']*)[""']", RegexOptions.IgnoreCase);
        var width = Regex.Match(imgTag, @"width\s*=\s*[""']?(\d+)[""']?", RegexOptions.IgnoreCase);
        var height = Regex.Match(imgTag, @"height\s*=\s*[""']?(\d+)[""']?", RegexOptions.IgnoreCase);

        if (!src.Success)
            return string.Empty; // No src, skip the image

        // Filter out tracking pixels, ads, and non-article images
        var srcValue = src.Groups[1].Value.ToLower();
        if (srcValue.Contains("tracking") || 
            srcValue.Contains("analytics") || 
            srcValue.Contains("pixel") ||
            srcValue.Contains("/ads/") ||
            srcValue.Contains("ad.") ||
            (width.Success && int.TryParse(width.Groups[1].Value, out int w) && w < 50) ||
            (height.Success && int.TryParse(height.Groups[1].Value, out int h) && h < 50))
        {
            return string.Empty; // Skip tracking pixels and small images
        }

        // Rebuild clean image tag
        var cleanImg = new StringBuilder("<img");
        cleanImg.Append($" src=\"{src.Groups[1].Value}\"");
        
        if (alt.Success)
            cleanImg.Append($" alt=\"{alt.Groups[1].Value}\"");
        
        if (title.Success)
            cleanImg.Append($" title=\"{title.Groups[1].Value}\"");
        
        if (width.Success && int.TryParse(width.Groups[1].Value, out int widthVal) && widthVal >= 50)
            cleanImg.Append($" width=\"{widthVal}\"");
        
        if (height.Success && int.TryParse(height.Groups[1].Value, out int heightVal) && heightVal >= 50)
            cleanImg.Append($" height=\"{heightVal}\"");

        cleanImg.Append("/>");
        return cleanImg.ToString();
    }

    public string SanitizeFilename(string filename)
    {
        // Remove non-alphanumeric characters except spaces
        var sanitized = Regex.Replace(filename, @"[^a-zA-Z0-9\s]", string.Empty);
        
        // Replace spaces with underscores
        sanitized = Regex.Replace(sanitized, @"\s+", "_");
        
        // Remove multiple consecutive underscores
        sanitized = Regex.Replace(sanitized, @"_{2,}", "_");
        
        // Trim underscores from start and end
        sanitized = sanitized.Trim('_');
        
        // Limit length
        if (sanitized.Length > 50)
            sanitized = sanitized.Substring(0, 50).TrimEnd('_');
        
        return string.IsNullOrWhiteSpace(sanitized) ? "article" : sanitized;
    }
}
