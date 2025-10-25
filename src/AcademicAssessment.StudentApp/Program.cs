using AcademicAssessment.StudentApp.Components;
using Aspire.StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add Aspire service discovery for Redis cache
builder.AddRedisClient("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure HTTP client for API calls with Aspire service discovery
builder.Services.AddHttpClient("ApiClient", client =>
{
    // Service name - Aspire will resolve to actual endpoint
    client.BaseAddress = new Uri("http://webapi");
})
.AddServiceDiscovery();

// Register a default HttpClient using the configured ApiClient
builder.Services.AddScoped<HttpClient>(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    return httpClientFactory.CreateClient("ApiClient");
});

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
// Comment this line out for local development
// app.MapDefaultEndpoints();

app.Run();
