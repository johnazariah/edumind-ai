using AcademicAssessment.StudentApp.Components;

var builder = WebApplication.CreateBuilder(args);

// For local development, use minimal ServiceDefaults without service discovery
// Comment this line out for Aspire orchestration
// builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure HTTP client for API calls
builder.Services.AddHttpClient("ApiClient", client =>
{
    // For local development, use localhost. In production/Aspire, this will be overridden by service discovery
    client.BaseAddress = new Uri("http://localhost:5103/");
});

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
