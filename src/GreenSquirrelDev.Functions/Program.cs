using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GreenSquirrelDev.Functions.Services;
using GreenSquirrelDev.Functions.Configuration;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
              .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Configuration
        var configuration = context.Configuration;
        services.Configure<CosmosDbSettings>(configuration.GetSection("CosmosDb"));
        services.Configure<GoogleAuthSettings>(configuration.GetSection("Google"));
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        // Services
        services.AddSingleton<ICosmosDbService, CosmosDbService>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
        services.AddSingleton<IUserRepository, UserRepository>();

        // HTTP Client
        services.AddHttpClient();
    })
    .Build();

host.Run();
