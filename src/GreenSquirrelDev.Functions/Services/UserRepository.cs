using GreenSquirrelDev.Shared.Models;
using Microsoft.Azure.Cosmos;

namespace GreenSquirrelDev.Functions.Services;

public class UserRepository : IUserRepository
{
    private readonly Container _container;

    public UserRepository(ICosmosDbService cosmosDbService)
    {
        _container = cosmosDbService.GetUsersContainer();
    }

    public async Task<User?> GetUserByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<User>(id, new PartitionKey("user"));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<User?> GetUserByGoogleIdAsync(string googleUserId)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.googleUserId = @googleUserId")
            .WithParameter("@googleUserId", googleUserId);

        var iterator = _container.GetItemQueryIterator<User>(query);

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            var user = response.FirstOrDefault();
            if (user != null)
                return user;
        }

        return null;
    }

    public async Task<User> CreateUserAsync(User user)
    {
        user.Id = Guid.NewGuid().ToString();
        user.CreatedAt = DateTime.UtcNow;
        user.LastLoginAt = DateTime.UtcNow;
        user.PartitionKey = "user";

        var response = await _container.CreateItemAsync(user, new PartitionKey(user.PartitionKey));
        return response.Resource;
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        user.LastLoginAt = DateTime.UtcNow;
        var response = await _container.ReplaceItemAsync(user, user.Id, new PartitionKey(user.PartitionKey));
        return response.Resource;
    }

    public async Task DeleteUserAsync(string id)
    {
        await _container.DeleteItemAsync<User>(id, new PartitionKey("user"));
    }
}
