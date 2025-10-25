var builder = DistributedApplication.CreateBuilder(args);

// Debug: Log environment information
Console.WriteLine($"AppHost Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
Console.WriteLine($"DOTNET_ENVIRONMENT: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}");

// PostgreSQL with Aspire service discovery
// Local: Aspire manages a containerized Postgres instance
// Azure: Uses Azure Database for PostgreSQL Flexible Server
var postgresPassword = builder.AddParameter("postgres-password", secret: true);

IResourceBuilder<IResourceWithConnectionString> postgres;
if (builder.ExecutionContext.IsPublishMode)
{
    // Azure deployment: Use Azure PostgreSQL Flexible Server
    postgres = builder.AddAzurePostgresFlexibleServer("postgres")
        .AddDatabase("edumind");
}
else
{
    // Local development: Use containerized Postgres
    postgres = builder.AddPostgres("postgres", password: postgresPassword, port: 5432)
        .WithLifetime(ContainerLifetime.Persistent)  // Keep data between runs
        .WithEnvironment("POSTGRES_DB", "edumind_dev")
        .WithEnvironment("POSTGRES_USER", "edumind_user")
        .AddDatabase("edumind");
}

// Redis cache with Aspire service discovery
// Local: Aspire manages a containerized Redis instance
// Azure: Uses Azure Cache for Redis
IResourceBuilder<IResourceWithConnectionString> redis;
if (builder.ExecutionContext.IsPublishMode)
{
    // Azure deployment: Use Azure Cache for Redis
    redis = builder.AddAzureRedis("cache");
}
else
{
    // Local development: Use containerized Redis
    redis = builder.AddRedis("cache", port: 6379)
        .WithLifetime(ContainerLifetime.Persistent);  // Keep data between runs
}

// AI Service Configuration
// Local: Ollama (runs in container)
// Azure: Azure OpenAI (would be configured via environment variables in production)
IResourceBuilder<ProjectResource> webApi;

if (builder.ExecutionContext.IsPublishMode)
{
    // Azure deployment: Don't include Ollama container
    // Azure OpenAI configuration would be set via Azure Container Apps environment variables
    webApi = builder.AddProject<Projects.AcademicAssessment_Web>("webapi")
        .WithExternalHttpEndpoints()  // Make publicly accessible
        .WithReference(postgres)
        .WithReference(redis)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);
    // Note: Azure OpenAI settings (endpoint, key, deployment) would be configured
    // via Azure Key Vault or Container Apps secrets in production
}
else
{
    // Local development: Use Ollama container
    var ollama = builder.AddContainer("ollama", "ollama/ollama", "latest")
        .WithHttpEndpoint(port: 11434, targetPort: 11434, name: "ollama-http")
        .WithLifetime(ContainerLifetime.Persistent)
        .WithBindMount(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ollama"), "/root/.ollama");

    // Add the Web API with Ollama reference
    webApi = builder.AddProject<Projects.AcademicAssessment_Web>("webapi")
        .WithExternalHttpEndpoints()  // Make publicly accessible
        .WithReference(postgres)
        .WithReference(redis)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
        .WithEnvironment("Ollama__BaseUrl", ollama.GetEndpoint("ollama-http"));
}

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
