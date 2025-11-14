using GreenSquirrelDev.Shared.Models;

namespace GreenSquirrelDev.Client.Services;

public interface IProjectService
{
    Task<List<Project>> GetProjectsAsync();
    Task<Project?> GetProjectByIdAsync(string id);
}
