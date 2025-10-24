var builder = DistributedApplication.CreateBuilder(args);

// Debug: Log environment information
Console.WriteLine($"AppHost Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
Console.WriteLine($"DOTNET_ENVIRONMENT: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}");

// For local development, use existing external services
// Add PostgreSQL database reference (pointing to existing container)
var postgres = builder.AddConnectionString("postgres", "Host=localhost;Port=5432;Database=edumind_dev;Username=edumind_user;Password=password123");

// Add Redis cache reference (pointing to existing container)  
var redis = builder.AddConnectionString("cache", "localhost:6379");

// Add OLLAMA reference (pointing to existing service)
var ollama = builder.AddConnectionString("ollama", "http://localhost:11434");

// Add the Web API (primary backend)
var webApi = builder.AddProject<Projects.AcademicAssessment_Web>("webapi")
    .WithExternalHttpEndpoints()  // Make publicly accessible
    .WithReference(postgres)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithEnvironment("Ollama__BaseUrl", "http://localhost:11434");

// Add the Dashboard (Admin interface)
builder.AddProject<Projects.AcademicAssessment_Dashboard>("dashboard")
    .WithExternalHttpEndpoints()  // Make publicly accessible
    .WithReference(webApi)
    .WithReference(postgres)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

// Add the Student App (Student interface)
builder.AddProject<Projects.AcademicAssessment_StudentApp>("studentapp")
    .WithExternalHttpEndpoints()  // Make publicly accessible
    .WithReference(webApi)
    .WithReference(postgres)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

builder.Build().Run();
