using Microsoft.Azure.Cosmos;

namespace GreenSquirrelDev.Functions.Services;

public interface ICosmosDbService
{
    Container GetUsersContainer();
    Container GetProjectsContainer();
}
