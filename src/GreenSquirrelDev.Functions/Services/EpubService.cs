using System.Text;
using Microsoft.Extensions.Logging;
// Note: In a real implementation we would use a library like VersOne.Epub or similar.
// For this task, we will simulate EPUB generation by creating a ZIP file structure or just a dummy file
// if no library is available. Given I cannot install NuGet packages easily without user input,
// I will create a basic valid EPUB structure manually using System.IO.Compression if possible,
// or mock it for now as "implementation detail" to be filled with a library later.
// However, the instructions imply I should implement it. I'll check if I can use a simple implementation.
// Update: I will write a simplified EPUB generator that creates a valid OCF container structure.

using System.IO.Compression;
using System.Xml.Linq;

namespace GreenSquirrelDev.Functions.Services;

public interface IEpubService
{
    Task<byte[]> GenerateEpubAsync(string htmlContent, string title, string author);
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
    {htmlContent}
</body>
</html>";
                   await writer.WriteAsync(xhtml);
                }
            }
            return memoryStream.ToArray();
        }
    }
}
