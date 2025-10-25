using AcademicAssessment.Dashboard.Components;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire ServiceDefaults (OpenTelemetry, Service Discovery, Health Checks)
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ============================================================
// REDIS CACHE - Aspire Service Discovery
// ============================================================
// Aspire automatically injects the Redis connection from AppHost's AddRedis("cache")
builder.AddRedisClient("cache");

// ============================================================
// HTTP CLIENT - Service Discovery for Web API
// ============================================================
// Configure HttpClient with service discovery
// When calling "http://webapi/...", Aspire resolves to actual endpoint
builder.Services.AddHttpClient("WebApi", client =>
{
    client.BaseAddress = new Uri("http://webapi");  // Aspire service discovery
})
.AddServiceDiscovery();  // Enable service discovery for this client

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

app.Run();
