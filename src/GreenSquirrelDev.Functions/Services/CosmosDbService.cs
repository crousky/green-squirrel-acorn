using GreenSquirrelDev.Functions.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace GreenSquirrelDev.Functions.Services;

public class CosmosDbService : ICosmosDbService
{
    private readonly CosmosClient _cosmosClient;
    private readonly Database _database;
    private readonly CosmosDbSettings _settings;

    public CosmosDbService(IOptions<CosmosDbSettings> settings)
    {
        _settings = settings.Value;
        _cosmosClient = new CosmosClient(_settings.ConnectionString);
        _database = _cosmosClient.GetDatabase(_settings.DatabaseName);
    }

    public Container GetUsersContainer()
    {
        return _database.GetContainer(_settings.UsersContainer);
    }

    public Container GetProjectsContainer()
    {
        return _database.GetContainer(_settings.ProjectsContainer);
    }
}
