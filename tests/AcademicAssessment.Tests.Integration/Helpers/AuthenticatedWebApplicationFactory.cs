using System.Text;
using AcademicAssessment.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AcademicAssessment.Tests.Integration.Helpers;

/// <summary>
/// Custom WebApplicationFactory that supports JWT authentication for integration tests
/// </summary>
public class AuthenticatedWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly string _databaseName;

    public AuthenticatedWebApplicationFactory()
    {
        // Use a unique database name for each test instance
        _databaseName = $"TestDb_{Guid.NewGuid()}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Development environment so that the stub authentication is used
        // This bypasses Azure AD B2C and uses simple JWT authentication
        builder.UseEnvironment("Development");

        // Set up test configuration
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Enabled"] = "true"
            }!);
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AcademicContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<AcademicContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // Configure JWT authentication for testing
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = JwtTokenGenerator.GetTestIssuer(),
                    ValidAudience = JwtTokenGenerator.GetTestAudience(),
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(JwtTokenGenerator.GetTestSecret()))
                };
            });

            // Build the service provider and seed the database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AcademicContext>();

            db.Database.EnsureCreated();
        });
    }    /// <summary>
         /// Seeds the database with test data
         /// </summary>
    public async Task SeedDatabaseAsync(Action<AcademicContext> seedAction)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AcademicContext>();
        seedAction(context);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Gets a scoped service from the test application
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Creates an authenticated HTTP client with a JWT token
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string jwtToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {jwtToken}");
        return client;
    }
}
