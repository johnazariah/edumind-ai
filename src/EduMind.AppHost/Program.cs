var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var edumindDb = postgres.AddDatabase("edumind");

// Add Redis cache
var redis = builder.AddRedis("cache")
    .WithDataVolume();

// Add OLLAMA (optional for local LLM)
var ollama = builder.AddContainer("ollama", "ollama/ollama")
    .WithBindMount("./ollama-data", "/root/.ollama")
    .WithHttpEndpoint(port: 11434, targetPort: 11434, name: "ollama");

// Add the Web API (primary backend)
var webApi = builder.AddProject<Projects.AcademicAssessment_Web>("webapi")
    .WithHttpsEndpoint(port: 5001, targetPort: 8080, name: "webapi-https")
    .WithHttpEndpoint(port: 5000, targetPort: 8080, name: "webapi-http")
    .WithReference(edumindDb)
    .WithReference(redis)
    .WithEnvironment("OLLAMA__BaseUrl", ollama.GetEndpoint("ollama"))
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

// Add the Dashboard (Admin interface)
builder.AddProject<Projects.AcademicAssessment_Dashboard>("dashboard")
    .WithReference(webApi)
    .WithReference(edumindDb)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

// Add the Student App (Student interface)
builder.AddProject<Projects.AcademicAssessment_StudentApp>("studentapp")
    .WithReference(webApi)
    .WithReference(edumindDb)
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName);

builder.Build().Run();
