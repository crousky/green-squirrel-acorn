using GreenSquirrelDev.Shared.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using User = GreenSquirrelDev.Shared.Models.User;

namespace GreenSquirrelDev.Functions.Services;

public class UserRepository : IUserRepository
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ICosmosDbService cosmosDbService, ILogger<UserRepository> logger)
    {
        _cosmosDbService = cosmosDbService;
        _logger = logger;
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return "null";
        
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "***@***";
        
        var localPart = email.Substring(0, atIndex);
        var domain = email.Substring(atIndex);
        
        // Show first 2 chars and last char of local part, mask the rest
        if (localPart.Length <= 3)
            return $"{localPart[0]}***{domain}";
        
        return $"{localPart.Substring(0, 2)}***{localPart[localPart.Length - 1]}{domain}";
    }

    private Container GetContainer()
    {
        return _cosmosDbService.GetUsersContainer();
    }

    public async Task<User?> GetUserByIdAsync(string id)
    {
        _logger.LogInformation("UserRepository: Getting user by userId={UserId}", id);
        
        try
        {
            var response = await GetContainer().ReadItemAsync<User>(id, new PartitionKey("user"));
            _logger.LogInformation("UserRepository: Successfully retrieved user userId={UserId}, email={Email}, hasKindleEmail={HasKindleEmail}", 
                id, MaskEmail(response.Resource.Email), !string.IsNullOrEmpty(response.Resource.KindleEmail));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("UserRepository: User not found userId={UserId}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserRepository: Error getting user by userId={UserId}", id);
            throw;
        }
    }

    public async Task<User?> GetUserByGoogleIdAsync(string googleUserId)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.googleUserId = @googleUserId")
            .WithParameter("@googleUserId", googleUserId);

        var iterator = GetContainer().GetItemQueryIterator<User>(query);

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

        var response = await GetContainer().CreateItemAsync(user, new PartitionKey(user.PartitionKey));
        return response.Resource;
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        _logger.LogInformation("UserRepository: Updating user userId={UserId}, email={Email}, kindleEmail={KindleEmail}", 
            user.Id, MaskEmail(user.Email), MaskEmail(user.KindleEmail));
        
        try
        {
            user.LastLoginAt = DateTime.UtcNow;
            var response = await GetContainer().ReplaceItemAsync(user, user.Id, new PartitionKey(user.PartitionKey));
            
            _logger.LogInformation("UserRepository: Successfully updated user userId={UserId}, requestCharge={RequestCharge} RU", 
                user.Id, response.RequestCharge);
            
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserRepository: Error updating user userId={UserId}, email={Email}", 
                user.Id, MaskEmail(user.Email));
            throw;
        }
    }

    public async Task DeleteUserAsync(string id)
    {
        await GetContainer().DeleteItemAsync<User>(id, new PartitionKey("user"));
    }
}
