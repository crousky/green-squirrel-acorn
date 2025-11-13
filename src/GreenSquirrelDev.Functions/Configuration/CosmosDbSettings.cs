namespace GreenSquirrelDev.Functions.Configuration;

public class CosmosDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "GreenSquirrelDev";
    public string UsersContainer { get; set; } = "Users";
    public string ProjectsContainer { get; set; } = "Projects";
}
