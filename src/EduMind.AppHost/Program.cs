var builder = DistributedApplication.CreateBuilder(args);

// Debug: Log environment information
Console.WriteLine($"AppHost Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
Console.WriteLine($"DOTNET_ENVIRONMENT: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}");

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var edumindDb = postgres.AddDatabase("edumind");

// Add Redis cache
var redis = builder.AddRedis("cache")
    .WithDataVolume();

// Add OLLAMA (conditional based on environment)
// In Testing/CI, Ollama is pre-installed on the system and we'll use http://localhost:11434 directly
// In Development, use containerized Ollama for isolation
IResourceBuilder<ContainerResource>? ollamaContainer = null;

if (builder.Environment.EnvironmentName != "Testing")
{
    // Use containerized Ollama (for local development)
    ollamaContainer = builder.AddContainer("ollama", "ollama/ollama")
        .WithBindMount("./ollama-data", "/root/.ollama")
        .WithHttpEndpoint(port: 11434, targetPort: 11434, name: "ollama");
}

// Add the Web API (primary backend)
var webApiBuilder = builder.AddProject<Projects.AcademicAssessment_Web>("webapi")
    .WithExternalHttpEndpoints()  // Make publicly accessible
    .WithReference(edumindDb)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

// Configure Ollama connection based on environment
if (ollamaContainer != null)
{
    // In Development mode, wait for containerized Ollama and use its endpoint
    webApiBuilder.WaitFor(ollamaContainer)
        .WithEnvironment("Ollama__BaseUrl", ollamaContainer.GetEndpoint("ollama"));
}
else
{
    // In Testing mode, use system-installed Ollama
    webApiBuilder.WithEnvironment("Ollama__BaseUrl", "http://localhost:11434");
}

var webApi = webApiBuilder;

// Add the Dashboard (Admin interface)
builder.AddProject<Projects.AcademicAssessment_Dashboard>("dashboard")
    .WithExternalHttpEndpoints()  // Make publicly accessible
    .WithReference(webApi)
    .WithReference(edumindDb)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

// Add the Student App (Student interface)
builder.AddProject<Projects.AcademicAssessment_StudentApp>("studentapp")
    .WithExternalHttpEndpoints()  // Make publicly accessible
    .WithReference(webApi)
    .WithReference(edumindDb)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

builder.Build().Run();
