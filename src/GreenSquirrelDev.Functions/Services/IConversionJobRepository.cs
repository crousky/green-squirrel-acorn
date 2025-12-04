using GreenSquirrelDev.Shared.Models;

namespace GreenSquirrelDev.Functions.Services;

public interface IConversionJobRepository
{
    Task<ConversionJob> CreateJobAsync(ConversionJob job);
    Task<ConversionJob> UpdateJobAsync(ConversionJob job);
    Task<ConversionJob?> GetJobByIdAsync(string id, string userId);
    Task<IEnumerable<ConversionJob>> GetJobsByUserIdAsync(string userId, int limit = 50);
}
