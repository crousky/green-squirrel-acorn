using GreenSquirrelDev.Shared.Models;
using Microsoft.Azure.Cosmos;

namespace GreenSquirrelDev.Functions.Services;

public class ConversionJobRepository : IConversionJobRepository
{
    private readonly Container _container;

    public ConversionJobRepository(ICosmosDbService cosmosDbService)
    {
        _container = cosmosDbService.GetConversionJobsContainer();
    }

    public async Task<ConversionJob> CreateJobAsync(ConversionJob job)
    {
        job.Id = Guid.NewGuid().ToString();
        job.CreatedAt = DateTime.UtcNow;
        job.PartitionKey = job.UserId; // Partition by user

        var response = await _container.CreateItemAsync(job, new PartitionKey(job.PartitionKey));
        return response.Resource;
    }

    public async Task<ConversionJob> UpdateJobAsync(ConversionJob job)
    {
        var response = await _container.ReplaceItemAsync(job, job.Id, new PartitionKey(job.PartitionKey));
        return response.Resource;
    }

    public async Task<ConversionJob?> GetJobByIdAsync(string id, string userId)
    {
        try
        {
            var response = await _container.ReadItemAsync<ConversionJob>(id, new PartitionKey(userId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<ConversionJob>> GetJobsByUserIdAsync(string userId, int limit = 50)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.userId = @userId ORDER BY c.createdAt DESC OFFSET 0 LIMIT @limit")
            .WithParameter("@userId", userId)
            .WithParameter("@limit", limit);

        var iterator = _container.GetItemQueryIterator<ConversionJob>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(userId)
        });

        var results = new List<ConversionJob>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }
}
