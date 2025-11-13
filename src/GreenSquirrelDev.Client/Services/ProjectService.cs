using GreenSquirrelDev.Shared.Models;

namespace GreenSquirrelDev.Client.Services;

public class ProjectService : IProjectService
{
    private readonly HttpClient _httpClient;

    public ProjectService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<List<Project>> GetProjectsAsync()
    {
        // For now, return hardcoded projects as per PRD
        // Later this can be fetched from API
        var projects = new List<Project>
        {
            new Project
            {
                Id = "pace-calculator",
                Name = "Pace Calculator",
                Description = "A handy tool for runners to calculate running paces, times, and distances. Perfect for training planning and race day strategy.",
                Status = ProjectStatus.Live,
                Url = "https://pacecalculator.greensquirrel.dev",
                ThumbnailUrl = "/images/projects/pace-calculator-thumb.png",
                Screenshots = new List<string>
                {
                    "/images/projects/pace-calculator-1.png",
                    "/images/projects/pace-calculator-2.png",
                    "/images/projects/pace-calculator-3.png"
                },
                Technologies = new List<string> { "Blazor WASM", "C#", "Azure Static Web Apps" },
                LaunchedAt = new DateTime(2024, 7, 15),
                CreatedAt = new DateTime(2024, 6, 1)
            },
            new Project
            {
                Id = "hive-reader",
                Name = "HiveReader",
                Description = "Send articles from the web directly to your Kindle for distraction-free reading. Includes Chrome extension for one-click saving.",
                Status = ProjectStatus.InDevelopment,
                Url = "https://hive-reader.greensquirrel.dev",
                ThumbnailUrl = "/images/projects/hive-reader-thumb.png",
                Screenshots = new List<string>
                {
                    "/images/projects/hive-reader-1.png",
                    "/images/projects/hive-reader-2.png"
                },
                Technologies = new List<string> { "Blazor WASM", "Azure Functions", "Chrome Extension", "Kindle API" },
                HasExtension = true,
                ExtensionUrl = "#", // Will be updated when extension is published
                CreatedAt = new DateTime(2024, 10, 1)
            }
        };

        return Task.FromResult(projects);
    }

    public async Task<Project?> GetProjectByIdAsync(string id)
    {
        var projects = await GetProjectsAsync();
        return projects.FirstOrDefault(p => p.Id == id);
    }
}
