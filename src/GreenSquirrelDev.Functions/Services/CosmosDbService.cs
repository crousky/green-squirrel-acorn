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
        if (!string.IsNullOrEmpty(_settings.ConnectionString) && _settings.ConnectionString != "Mock")
        {
            _cosmosClient = new CosmosClient(_settings.ConnectionString);
            _database = _cosmosClient.GetDatabase(_settings.DatabaseName);
        }
    }

    public Container GetUsersContainer()
    {
        if (_database == null) throw new InvalidOperationException("CosmosDB is not configured.");
        return _database.GetContainer(_settings.UsersContainer);
    }

    public Container GetProjectsContainer()
    {
        if (_database == null) throw new InvalidOperationException("CosmosDB is not configured.");
        return _database.GetContainer(_settings.ProjectsContainer);
    }
}
