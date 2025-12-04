using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace GreenSquirrelDev.Functions.Services;

public class EpubService : IEpubService
{
    private readonly ILogger<EpubService> _logger;
    private readonly HttpClient _httpClient;

    // Elements to remove during cleaning
    private static readonly string[] ElementsToRemove = new[]
    {
        "script", "style", "nav", "header", "footer", "aside", "form",
        "iframe", "noscript", "svg", "canvas", "video", "audio"
    };

    // Class patterns that indicate non-content elements
    private static readonly string[] ClassPatternsToRemove = new[]
    {
        "nav", "menu", "sidebar", "footer", "header", "comment", "share",
        "social", "advertisement", "ad-", "ads-", "promo", "related",
        "newsletter", "subscribe", "popup", "modal", "cookie", "banner"
    };

    public EpubService(ILogger<EpubService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<EpubResult> GenerateEpubAsync(EpubInput input)
    {
        try
        {
            // Parse and clean HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(input.Html);

            // Extract and clean article content
            var articleContent = ExtractArticleContent(doc);
            var cleanedHtml = CleanHtml(articleContent);

            // Download images if requested
            var images = new Dictionary<string, byte[]>();
            if (input.IncludeImages)
            {
                images = await DownloadImagesAsync(cleanedHtml, input.Url);
                cleanedHtml = UpdateImageReferences(cleanedHtml, images);
            }

            // Calculate word count
            var textContent = Regex.Replace(cleanedHtml, "<[^>]+>", " ");
            var wordCount = textContent.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

            // Generate EPUB
            var epubData = CreateEpub(input.Title, input.Author ?? "Unknown", cleanedHtml, images, input.Url);
            var fileName = SanitizeFileName(input.Title) + ".epub";

            return new EpubResult
            {
                Success = true,
                EpubData = epubData,
                FileName = fileName,
                WordCount = wordCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating EPUB for {Url}", input.Url);
            return new EpubResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private HtmlNode ExtractArticleContent(HtmlDocument doc)
    {
        // Try to find article content using common patterns
        var articleSelectors = new[]
        {
            "//article",
            "//*[@role='main']",
            "//*[@id='content']",
            "//*[@id='main-content']",
            "//*[@class='post-content']",
            "//*[@class='article-content']",
            "//*[@class='entry-content']",
            "//*[contains(@class, 'article-body')]",
            "//*[contains(@class, 'post-body')]",
            "//main",
            "//body"
        };

        foreach (var selector in articleSelectors)
        {
            var node = doc.DocumentNode.SelectSingleNode(selector);
            if (node != null && !string.IsNullOrWhiteSpace(node.InnerText))
            {
                return node;
            }
        }

        return doc.DocumentNode.SelectSingleNode("//body") ?? doc.DocumentNode;
    }

    private string CleanHtml(HtmlNode content)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(content.OuterHtml);

        // Remove unwanted elements
        foreach (var tag in ElementsToRemove)
        {
            var nodes = doc.DocumentNode.SelectNodes($"//{tag}");
            if (nodes != null)
            {
                foreach (var node in nodes.ToList())
                {
                    node.Remove();
                }
            }
        }

        // Remove elements with unwanted class patterns
        foreach (var pattern in ClassPatternsToRemove)
        {
            var nodes = doc.DocumentNode.SelectNodes($"//*[contains(@class, '{pattern}')]");
            if (nodes != null)
            {
                foreach (var node in nodes.ToList())
                {
                    node.Remove();
                }
            }

            nodes = doc.DocumentNode.SelectNodes($"//*[contains(@id, '{pattern}')]");
            if (nodes != null)
            {
                foreach (var node in nodes.ToList())
                {
                    node.Remove();
                }
            }
        }

        // Remove empty paragraphs and divs
        var emptyNodes = doc.DocumentNode.SelectNodes("//p[not(normalize-space())] | //div[not(normalize-space()) and not(.//img)]");
        if (emptyNodes != null)
        {
            foreach (var node in emptyNodes.ToList())
            {
                node.Remove();
            }
        }

        // Remove all attributes except src, alt, href on allowed tags
        CleanAttributes(doc.DocumentNode);

        return doc.DocumentNode.InnerHtml;
    }

    private void CleanAttributes(HtmlNode node)
    {
        var allowedAttributes = new Dictionary<string, string[]>
        {
            { "img", new[] { "src", "alt" } },
            { "a", new[] { "href" } }
        };

        foreach (var child in node.Descendants().ToList())
        {
            if (child.NodeType == HtmlNodeType.Element)
            {
                var tagName = child.Name.ToLower();
                var allowedAttrs = allowedAttributes.ContainsKey(tagName) ? allowedAttributes[tagName] : Array.Empty<string>();

                var attrsToRemove = child.Attributes
                    .Where(a => !allowedAttrs.Contains(a.Name.ToLower()))
                    .ToList();

                foreach (var attr in attrsToRemove)
                {
                    child.Attributes.Remove(attr);
                }
            }
        }
    }

    private async Task<Dictionary<string, byte[]>> DownloadImagesAsync(string html, string baseUrl)
    {
        var images = new Dictionary<string, byte[]>();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var imgNodes = doc.DocumentNode.SelectNodes("//img[@src]");
        if (imgNodes == null) return images;

        var baseUri = new Uri(baseUrl);
        var imageCount = 0;

        foreach (var img in imgNodes.Take(20)) // Limit to 20 images
        {
            try
            {
                var src = img.GetAttributeValue("src", "");
                if (string.IsNullOrEmpty(src)) continue;

                // Skip data URIs
                if (src.StartsWith("data:")) continue;

                // Resolve relative URLs
                Uri imageUri;
                if (!Uri.TryCreate(src, UriKind.Absolute, out imageUri!))
                {
                    imageUri = new Uri(baseUri, src);
                }

                // Only download http/https images
                if (imageUri.Scheme != "http" && imageUri.Scheme != "https") continue;

                var imageData = await _httpClient.GetByteArrayAsync(imageUri);
                var extension = GetImageExtension(imageUri.AbsolutePath);
                var fileName = $"image{++imageCount}{extension}";

                images[src] = imageData;
                _logger.LogDebug("Downloaded image: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download image");
            }
        }

        return images;
    }

    private string UpdateImageReferences(string html, Dictionary<string, byte[]> images)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var imgNodes = doc.DocumentNode.SelectNodes("//img[@src]");
        if (imgNodes == null) return html;

        var imageIndex = 0;
        foreach (var img in imgNodes)
        {
            var src = img.GetAttributeValue("src", "");
            if (images.ContainsKey(src))
            {
                var extension = GetImageExtension(src);
                img.SetAttributeValue("src", $"images/image{++imageIndex}{extension}");
            }
            else
            {
                // Remove images we couldn't download
                img.Remove();
            }
        }

        return doc.DocumentNode.InnerHtml;
    }

    private string GetImageExtension(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".jpg" or ".jpeg" => ".jpg",
            ".png" => ".png",
            ".gif" => ".gif",
            ".webp" => ".webp",
            _ => ".jpg"
        };
    }

    private byte[] CreateEpub(string title, string author, string content, Dictionary<string, byte[]> images, string sourceUrl)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // mimetype (must be first and uncompressed)
            var mimetypeEntry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
            using (var writer = new StreamWriter(mimetypeEntry.Open()))
            {
                writer.Write("application/epub+zip");
            }

            // META-INF/container.xml
            var containerEntry = archive.CreateEntry("META-INF/container.xml");
            using (var writer = new StreamWriter(containerEntry.Open()))
            {
                writer.Write(GetContainerXml());
            }

            // OEBPS/content.opf
            var opfEntry = archive.CreateEntry("OEBPS/content.opf");
            using (var writer = new StreamWriter(opfEntry.Open()))
            {
                writer.Write(GetContentOpf(title, author, images));
            }

            // OEBPS/toc.ncx
            var ncxEntry = archive.CreateEntry("OEBPS/toc.ncx");
            using (var writer = new StreamWriter(ncxEntry.Open()))
            {
                writer.Write(GetTocNcx(title));
            }

            // OEBPS/nav.xhtml (EPUB 3 navigation)
            var navEntry = archive.CreateEntry("OEBPS/nav.xhtml");
            using (var writer = new StreamWriter(navEntry.Open()))
            {
                writer.Write(GetNavXhtml(title));
            }

            // OEBPS/styles.css
            var cssEntry = archive.CreateEntry("OEBPS/styles.css");
            using (var writer = new StreamWriter(cssEntry.Open()))
            {
                writer.Write(GetStylesCss());
            }

            // OEBPS/chapter1.xhtml
            var chapterEntry = archive.CreateEntry("OEBPS/chapter1.xhtml");
            using (var writer = new StreamWriter(chapterEntry.Open()))
            {
                writer.Write(GetChapterXhtml(title, author, content, sourceUrl));
            }

            // Add images
            var imageIndex = 0;
            foreach (var kvp in images)
            {
                var extension = GetImageExtension(kvp.Key);
                var imagePath = $"OEBPS/images/image{++imageIndex}{extension}";
                var imageEntry = archive.CreateEntry(imagePath);
                using (var stream = imageEntry.Open())
                {
                    stream.Write(kvp.Value, 0, kvp.Value.Length);
                }
            }
        }

        return memoryStream.ToArray();
    }

    private string GetContainerXml()
    {
        return @"<?xml version=""1.0"" encoding=""UTF-8""?>
<container version=""1.0"" xmlns=""urn:oasis:names:tc:opendocument:xmlns:container"">
  <rootfiles>
    <rootfile full-path=""OEBPS/content.opf"" media-type=""application/oebps-package+xml""/>
  </rootfiles>
</container>";
    }

    private string GetContentOpf(string title, string author, Dictionary<string, byte[]> images)
    {
        var imageManifest = new StringBuilder();
        var imageIndex = 0;
        foreach (var kvp in images)
        {
            var extension = GetImageExtension(kvp.Key);
            var mediaType = extension switch
            {
                ".jpg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
            imageIndex++;
            imageManifest.AppendLine($@"    <item id=""image{imageIndex}"" href=""images/image{imageIndex}{extension}"" media-type=""{mediaType}""/>");
        }

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<package xmlns=""http://www.idpf.org/2007/opf"" version=""3.0"" unique-identifier=""BookId"">
  <metadata xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:opf=""http://www.idpf.org/2007/opf"">
    <dc:identifier id=""BookId"">{Guid.NewGuid()}</dc:identifier>
    <dc:title>{EscapeXml(title)}</dc:title>
    <dc:creator>{EscapeXml(author)}</dc:creator>
    <dc:publisher>HiveReader by Green Squirrel Dev</dc:publisher>
    <dc:language>en</dc:language>
    <dc:date>{DateTime.UtcNow:yyyy-MM-dd}</dc:date>
    <meta property=""dcterms:modified"">{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</meta>
  </metadata>
  <manifest>
    <item id=""nav"" href=""nav.xhtml"" media-type=""application/xhtml+xml"" properties=""nav""/>
    <item id=""ncx"" href=""toc.ncx"" media-type=""application/x-dtbncx+xml""/>
    <item id=""css"" href=""styles.css"" media-type=""text/css""/>
    <item id=""chapter1"" href=""chapter1.xhtml"" media-type=""application/xhtml+xml""/>
{imageManifest}  </manifest>
  <spine toc=""ncx"">
    <itemref idref=""chapter1""/>
  </spine>
</package>";
    }

    private string GetTocNcx(string title)
    {
        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<ncx xmlns=""http://www.daisy.org/z3986/2005/ncx/"" version=""2005-1"">
  <head>
    <meta name=""dtb:uid"" content=""{Guid.NewGuid()}""/>
    <meta name=""dtb:depth"" content=""1""/>
    <meta name=""dtb:totalPageCount"" content=""0""/>
    <meta name=""dtb:maxPageNumber"" content=""0""/>
  </head>
  <docTitle>
    <text>{EscapeXml(title)}</text>
  </docTitle>
  <navMap>
    <navPoint id=""navpoint1"" playOrder=""1"">
      <navLabel>
        <text>{EscapeXml(title)}</text>
      </navLabel>
      <content src=""chapter1.xhtml""/>
    </navPoint>
  </navMap>
</ncx>";
    }

    private string GetNavXhtml(string title)
    {
        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE html>
<html xmlns=""http://www.w3.org/1999/xhtml"" xmlns:epub=""http://www.idpf.org/2007/ops"">
<head>
  <title>Table of Contents</title>
</head>
<body>
  <nav epub:type=""toc"">
    <h1>Table of Contents</h1>
    <ol>
      <li><a href=""chapter1.xhtml"">{EscapeXml(title)}</a></li>
    </ol>
  </nav>
</body>
</html>";
    }

    private string GetStylesCss()
    {
        return @"body {
  font-family: Georgia, serif;
  line-height: 1.6;
  margin: 1em;
  padding: 0;
}

h1, h2, h3, h4, h5, h6 {
  font-family: Arial, sans-serif;
  line-height: 1.3;
  margin-top: 1.5em;
  margin-bottom: 0.5em;
}

h1 { font-size: 1.8em; }
h2 { font-size: 1.5em; }
h3 { font-size: 1.3em; }

p {
  margin: 0.8em 0;
  text-align: justify;
}

img {
  max-width: 100%;
  height: auto;
  display: block;
  margin: 1em auto;
}

a {
  color: #0066cc;
  text-decoration: none;
}

blockquote {
  margin: 1em 2em;
  padding-left: 1em;
  border-left: 3px solid #ccc;
  font-style: italic;
}

pre, code {
  font-family: monospace;
  background-color: #f5f5f5;
  padding: 0.2em 0.4em;
  font-size: 0.9em;
}

pre {
  padding: 1em;
  overflow-x: auto;
  white-space: pre-wrap;
}

.source-info {
  font-size: 0.85em;
  color: #666;
  margin-top: 2em;
  padding-top: 1em;
  border-top: 1px solid #ccc;
}";
    }

    private string GetChapterXhtml(string title, string author, string content, string sourceUrl)
    {
        // Ensure content is valid XHTML
        var cleanContent = CleanForXhtml(content);

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE html>
<html xmlns=""http://www.w3.org/1999/xhtml"">
<head>
  <title>{EscapeXml(title)}</title>
  <link rel=""stylesheet"" type=""text/css"" href=""styles.css""/>
</head>
<body>
  <h1>{EscapeXml(title)}</h1>
  <p><em>By {EscapeXml(author)}</em></p>
  <hr/>
  {cleanContent}
  <div class=""source-info"">
    <p>Source: <a href=""{EscapeXml(sourceUrl)}"">{EscapeXml(sourceUrl)}</a></p>
    <p>Saved via HiveReader on {DateTime.UtcNow:MMMM d, yyyy}</p>
  </div>
</body>
</html>";
    }

    private string CleanForXhtml(string html)
    {
        // Load with HtmlAgilityPack and output as XHTML
        var doc = new HtmlDocument();
        doc.OptionOutputAsXml = true;
        doc.LoadHtml(html);

        // Ensure all tags are properly closed
        foreach (var node in doc.DocumentNode.Descendants().ToList())
        {
            if (node.NodeType == HtmlNodeType.Element)
            {
                // Convert self-closing tags
                if (node.Name == "br" || node.Name == "hr" || node.Name == "img")
                {
                    // These are handled by HtmlAgilityPack's XML output
                }
            }
        }

        using var writer = new StringWriter();
        doc.Save(writer);
        var result = writer.ToString();

        // Remove XML declaration if present (already in main document)
        result = Regex.Replace(result, @"<\?xml[^>]*\?>", "");

        return result.Trim();
    }

    private static string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        sanitized = Regex.Replace(sanitized, @"\s+", "-");
        sanitized = sanitized.Trim('-');

        if (sanitized.Length > 100)
        {
            sanitized = sanitized.Substring(0, 100);
        }

        return string.IsNullOrEmpty(sanitized) ? "article" : sanitized;
    }
}
