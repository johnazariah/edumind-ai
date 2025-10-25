var builder = DistributedApplication.CreateBuilder(args);

// Debug: Log environment information
Console.WriteLine($"AppHost Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
Console.WriteLine($"DOTNET_ENVIRONMENT: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}");

// PostgreSQL with Aspire service discovery
// Aspire manages the container lifecycle and connection strings automatically
var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithLifetime(ContainerLifetime.Persistent)  // Keep data between runs
    .WithEnvironment("POSTGRES_DB", "edumind_dev")
    .WithEnvironment("POSTGRES_USER", "edumind_user")
    .WithEnvironment("POSTGRES_PASSWORD", "password123")
    .AddDatabase("edumind");

// Redis cache with Aspire service discovery
var redis = builder.AddRedis("cache", port: 6379)
    .WithLifetime(ContainerLifetime.Persistent);  // Keep data between runs

// Ollama (local development only - won't deploy to Azure)
// Using AddContainer for services not yet supported by Aspire hosting packages
var ollama = builder.AddContainer("ollama", "ollama/ollama", "latest")
    .WithHttpEndpoint(port: 11434, targetPort: 11434, name: "ollama-http")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithBindMount(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ollama"), "/root/.ollama");

// Add the Web API (primary backend)
var webApi = builder.AddProject<Projects.AcademicAssessment_Web>("webapi")
    .WithExternalHttpEndpoints()  // Make publicly accessible
    .WithReference(postgres)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithEnvironment("Ollama__BaseUrl", ollama.GetEndpoint("ollama-http"));

// Add the Dashboard (Admin interface)
builder.AddProject<Projects.AcademicAssessment_Dashboard>("dashboard")
    .WithExternalHttpEndpoints()  // Make publicly accessible
    .WithReference(webApi)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

// Add the Student App (Student interface)
builder.AddProject<Projects.AcademicAssessment_StudentApp>("studentapp")
    .WithExternalHttpEndpoints()  // Make publicly accessible
    .WithReference(webApi)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

builder.Build().Run();
