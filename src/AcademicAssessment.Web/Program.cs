using System.Reflection;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

// ============================================================
// SERILOG CONFIGURATION (Early initialization)
// ============================================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/edumind-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("Starting EduMind.AI Web API");

    var builder = WebApplication.CreateBuilder(args);

    // ============================================================
    // ASPIRE SERVICE DEFAULTS - OpenTelemetry, Service Discovery, Health Checks
    // ============================================================
    builder.AddServiceDefaults();

    // ============================================================
    // LOGGING - Use Serilog
    // ============================================================
    builder.Host.UseSerilog();

    // ============================================================
    // CORS CONFIGURATION
    // ============================================================
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DevelopmentCors", policy =>
        {
            policy
                .WithOrigins(
                    "https://localhost:5001",
                    "https://localhost:5002",
                    "https://localhost:5003",
                    "http://localhost:5000",
                    "http://localhost:5001",
                    "http://localhost:5002",
                    "http://localhost:5003"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials() // Required for SignalR
                .SetIsOriginAllowedToAllowWildcardSubdomains();
        });

        options.AddPolicy("ProductionCors", policy =>
        {
            // Configure production origins from configuration
            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? Array.Empty<string>();

            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .SetIsOriginAllowedToAllowWildcardSubdomains();
        });
    });

    // ============================================================
    // API VERSIONING
    // ============================================================
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Api-Version"),
            new MediaTypeApiVersionReader("version")
        );
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // ============================================================
    // AUTHENTICATION & AUTHORIZATION
    // ============================================================
    var authEnabled = builder.Configuration.GetValue<bool>("Authentication:Enabled");

    if (authEnabled && !builder.Environment.IsDevelopment())
    {
        // Production: Azure AD B2C with JWT Bearer authentication
        builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAdB2C"));

        builder.Services.AddAuthorization(options =>
        {
            // Role-based policies
            options.AddPolicy("StudentPolicy", policy => policy.RequireRole("Student"));
            options.AddPolicy("TeacherPolicy", policy => policy.RequireRole("Teacher"));
            options.AddPolicy("SchoolAdminPolicy", policy => policy.RequireRole("SchoolAdmin"));
            options.AddPolicy("CourseAdminPolicy", policy => policy.RequireRole("CourseAdmin"));
            options.AddPolicy("BusinessAdminPolicy", policy => policy.RequireRole("BusinessAdmin"));
            options.AddPolicy("SystemAdminPolicy", policy => policy.RequireRole("SystemAdmin"));

            // Combined policies
            options.AddPolicy("AdminPolicy", policy => policy.RequireRole("SchoolAdmin", "BusinessAdmin", "SystemAdmin"));
            options.AddPolicy("EducatorPolicy", policy => policy.RequireRole("Teacher", "SchoolAdmin", "CourseAdmin"));
            options.AddPolicy("AllUsersPolicy", policy => policy.RequireAuthenticatedUser());
        });
    }
    else
    {
        // Development: Test JWT authentication with the same policies as production
        builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        builder.Services.AddAuthorization(options =>
        {
            // Same policies as production for consistent testing
            options.AddPolicy("StudentPolicy", policy => policy.RequireRole("Student"));
            options.AddPolicy("TeacherPolicy", policy => policy.RequireRole("Teacher"));
            options.AddPolicy("SchoolAdminPolicy", policy => policy.RequireRole("SchoolAdmin"));
            options.AddPolicy("CourseAdminPolicy", policy => policy.RequireRole("CourseAdmin"));
            options.AddPolicy("BusinessAdminPolicy", policy => policy.RequireRole("BusinessAdmin"));
            options.AddPolicy("SystemAdminPolicy", policy => policy.RequireRole("SystemAdmin"));

            // Combined policies
            options.AddPolicy("AdminPolicy", policy => policy.RequireRole("SchoolAdmin", "BusinessAdmin", "SystemAdmin"));
            options.AddPolicy("EducatorPolicy", policy => policy.RequireRole("Teacher", "SchoolAdmin", "CourseAdmin"));
            options.AddPolicy("AllUsersPolicy", policy => policy.RequireAuthenticatedUser());
        });
    }

    // HTTP Context Accessor for TenantContext
    builder.Services.AddHttpContextAccessor();

    // ============================================================
    // CONTROLLERS & SIGNALR
    // ============================================================
    builder.Services.AddControllers()
        .AddNewtonsoftJson(options =>
        {
            // Use Newtonsoft.Json to workaround .NET 9 test compatibility issue with PipeWriter.UnflushedBytes
            // See: https://github.com/dotnet/aspnetcore/issues/52187
            options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
        });
    builder.Services.AddSignalR();

    // ============================================================
    // HEALTH CHECKS
    // ============================================================
    // Use Aspire-provided connection strings: "edumind" for PostgreSQL and "cache" for Redis
    // Fall back to DefaultConnection/Redis for local development
    var connectionString = builder.Configuration.GetConnectionString("edumind")
        ?? builder.Configuration.GetConnectionString("DefaultConnection");
    var redisConnection = builder.Configuration.GetConnectionString("cache")
        ?? builder.Configuration.GetConnectionString("Redis");

    // WORKAROUND: In Azure Container Apps, Aspire generates connection strings with short hostnames
    // ("postgres", "cache") but Azure Container Apps requires full internal FQDNs for service-to-service communication.
    // Detect if running in Azure and patch the connection strings with proper FQDNs.
    var azureContainerAppsDomain = builder.Configuration["AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN"];
    Log.Information("Azure Container Apps Domain: {Domain}", azureContainerAppsDomain ?? "(not set)");
    Log.Information("Original PostgreSQL connection string present: {HasConnection}", !string.IsNullOrEmpty(connectionString));
    Log.Information("Original Redis connection string present: {HasConnection}", !string.IsNullOrEmpty(redisConnection));

    if (!string.IsNullOrEmpty(azureContainerAppsDomain) && !string.IsNullOrEmpty(connectionString))
    {
        Log.Information("Checking PostgreSQL connection string for hostname patching...");
        // PostgreSQL: Replace "Host=postgres" with "Host=postgres.internal.{domain}"
        // Use regex to match "Host=postgres" followed by any delimiter (;, space, end of string, etc.)
        if (System.Text.RegularExpressions.Regex.IsMatch(connectionString, @"Host=postgres(?=[;\s,]|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            var internalFqdn = $"postgres.internal.{azureContainerAppsDomain}";
            connectionString = System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"Host=postgres(?=[;\s,]|$)",
                $"Host={internalFqdn}",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Log.Information("✅ Patched PostgreSQL hostname to use Azure Container Apps internal FQDN: {Fqdn}", internalFqdn);
        }
        else
        {
            Log.Warning("❌ PostgreSQL connection string pattern did not match for patching");
        }
    }
    if (!string.IsNullOrEmpty(azureContainerAppsDomain) && !string.IsNullOrEmpty(redisConnection))
    {
        Log.Information("Checking Redis connection string for hostname patching...");
        // Redis: Replace "cache:" or "cache" at start of connection string with "cache.internal.{domain}"
        var internalFqdn = $"cache.internal.{azureContainerAppsDomain}";
        if (redisConnection.StartsWith("cache:"))
        {
            redisConnection = redisConnection.Replace("cache:", $"{internalFqdn}:");
            Log.Information("✅ Patched Redis hostname to use Azure Container Apps internal FQDN: {Fqdn}", internalFqdn);
        }
        else if (redisConnection.StartsWith("cache,") || redisConnection == "cache")
        {
            redisConnection = System.Text.RegularExpressions.Regex.Replace(
                redisConnection,
                @"^cache(?=[,]|$)",
                internalFqdn,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            Log.Information("✅ Patched Redis hostname to use Azure Container Apps internal FQDN: {Fqdn}", internalFqdn);
        }
        else
        {
            Log.Warning("❌ Redis connection string pattern did not match for patching: '{RedisStart}'", redisConnection.Substring(0, Math.Min(20, redisConnection.Length)));
        }
    }

    // Debug logging for connection strings (without exposing passwords)
    Log.Information("PostgreSQL connection string configured: {HasConnection}", !string.IsNullOrEmpty(connectionString));
    if (!string.IsNullOrEmpty(connectionString))
    {
        // Log host portion only (no password)
        var hostMatch = System.Text.RegularExpressions.Regex.Match(connectionString, @"Host=([^;]+)");
        if (hostMatch.Success)
            Log.Information("PostgreSQL Host: {Host}", hostMatch.Groups[1].Value);
    }
    Log.Information("Redis connection string configured: {HasConnection}", !string.IsNullOrEmpty(redisConnection));
    if (!string.IsNullOrEmpty(redisConnection))
    {
        // Log host portion only (no password)
        var hostMatch = System.Text.RegularExpressions.Regex.Match(redisConnection, @"^([^,]+)");
        if (hostMatch.Success)
            Log.Information("Redis Host: {Host}", hostMatch.Groups[1].Value);
    }

    builder.Services.AddHealthChecks()
        .AddNpgSql(
            connectionString ?? "Host=localhost;Database=edumind_dev;Username=edumind_user;Password=edumind_dev_password",
            name: "postgresql",
            tags: new[] { "db", "postgresql", "ready" })
        .AddRedis(
            redisConnection ?? "localhost:6379",
            name: "redis",
            tags: new[] { "cache", "redis", "ready" });    // ============================================================
    // SWAGGER/OPENAPI CONFIGURATION
    // ============================================================
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = "EduMind.AI API",
            Description = "Academic Test Preparation Multi-Agent System API - Provides endpoints for student analytics, adaptive assessments, and real-time progress tracking.",
            Contact = new OpenApiContact
            {
                Name = "EduMind.AI Support",
                Email = "support@edumind.ai",
                Url = new Uri("https://edumind.ai/support")
            },
            License = new OpenApiLicense
            {
                Name = "Proprietary License",
                Url = new Uri("https://edumind.ai/license")
            }
        });

        // Add XML documentation
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        }

        // Add JWT Bearer authorization
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        // Add API versioning support
        options.OperationFilter<SwaggerDefaultValues>();

        // Group by controller name
        options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "Default" });
        options.DocInclusionPredicate((docName, apiDesc) => true);
    });

    // ============================================================
    // DATABASE CONTEXT
    // ============================================================
    builder.Services.AddDbContext<AcademicAssessment.Infrastructure.Data.AcademicContext>(options =>
    {
        // Use Aspire-provided connection string "edumind", fall back to AcademicDatabase or default
        var connectionString = builder.Configuration.GetConnectionString("edumind")
            ?? builder.Configuration.GetConnectionString("AcademicDatabase")
            ?? "Host=localhost;Database=edumind_dev;Username=edumind_user;Password=edumind_dev_password";
        options.UseNpgsql(connectionString);
    });

    // ============================================================
    // APPLICATION SERVICES
    // ============================================================

    // Tenant Context - Production vs Development
    if (authEnabled && !builder.Environment.IsDevelopment())
    {
        // Production: JWT-based tenant context
        builder.Services.AddScoped<AcademicAssessment.Core.Interfaces.ITenantContext, AcademicAssessment.Infrastructure.Context.TenantContextJwt>();
    }
    else
    {
        // Development: Stub tenant context
        builder.Services.AddScoped<AcademicAssessment.Core.Interfaces.ITenantContext, AcademicAssessment.Web.Services.TenantContextDevelopment>();
    }

    // Analytics Service
    builder.Services.AddScoped<AcademicAssessment.Core.Interfaces.IStudentAnalyticsService, AcademicAssessment.Analytics.Services.StudentAnalyticsService>();

    // Real repository implementations (Infrastructure layer)
    builder.Services.AddScoped<AcademicAssessment.Core.Interfaces.IStudentRepository, AcademicAssessment.Infrastructure.Repositories.StudentRepository>();
    builder.Services.AddScoped<AcademicAssessment.Core.Interfaces.IStudentAssessmentRepository, AcademicAssessment.Infrastructure.Repositories.StudentAssessmentRepository>();
    builder.Services.AddScoped<AcademicAssessment.Core.Interfaces.IStudentResponseRepository, AcademicAssessment.Infrastructure.Repositories.StudentResponseRepository>();
    builder.Services.AddScoped<AcademicAssessment.Core.Interfaces.IQuestionRepository, AcademicAssessment.Infrastructure.Repositories.QuestionRepository>();
    builder.Services.AddScoped<AcademicAssessment.Core.Interfaces.IAssessmentRepository, AcademicAssessment.Infrastructure.Repositories.AssessmentRepository>();

    // Stub repositories for development (DEPRECATED - remove after database is fully integrated)
    // builder.Services.AddScoped<AcademicAssessment.Core.Interfaces.IStudentAssessmentRepository, AcademicAssessment.Web.Services.StubStudentAssessmentRepository>();
    // builder.Services.AddScoped<AcademicAssessment.Core.Interfaces.IStudentResponseRepository, AcademicAssessment.Web.Services.StubStudentResponseRepository>();
    // builder.Services.AddScoped<AcademicAssessment.Core.Interfaces.IQuestionRepository, AcademicAssessment.Web.Services.StubQuestionRepository>();
    // builder.Services.AddScoped<AcademicAssessment.Core.Interfaces.IAssessmentRepository, AcademicAssessment.Web.Services.StubAssessmentRepository>();

    // ============================================================
    // A2A AGENT INFRASTRUCTURE (Phase 1, 2, 3 & 4)
    // ============================================================

    // Task service for agent-to-agent communication
    builder.Services.AddSingleton<AcademicAssessment.Agents.Shared.Interfaces.ITaskService, AcademicAssessment.Agents.Shared.Services.TaskService>();

    // SignalR for real-time agent progress updates
    builder.Services.AddSignalR();

    // LLM Service for AI-powered assessment features (Phase 4)
    // Configure based on appsettings.json LLM:Provider setting
    var llmProvider = builder.Configuration["LLM:Provider"] ?? "Stub";
    switch (llmProvider.ToLowerInvariant())
    {
        case "ollama":
            builder.Services.AddScoped<AcademicAssessment.Core.Interfaces.ILLMService, AcademicAssessment.Infrastructure.ExternalServices.OllamaService>();
            Log.Information("LLM Service configured: OllamaService (local AI, zero cost)");
            break;

        case "azureopenai":
            // TODO: Implement when Azure.AI.OpenAI SDK v2.0 is stable
            Log.Warning("Azure OpenAI not yet implemented, falling back to StubLLMService");
            builder.Services.AddScoped<AcademicAssessment.Core.Interfaces.ILLMService, AcademicAssessment.Infrastructure.ExternalServices.StubLLMService>();
            break;

        case "stub":
        default:
            builder.Services.AddScoped<AcademicAssessment.Core.Interfaces.ILLMService, AcademicAssessment.Infrastructure.ExternalServices.StubLLMService>();
            Log.Information("LLM Service configured: StubLLMService (mock mode for testing)");
            break;
    }

    // Student Progress Orchestrator (Phase 2)
    // Changed from Singleton to Scoped because it depends on scoped repositories
    builder.Services.AddScoped<AcademicAssessment.Orchestration.StudentProgressOrchestrator>();

    // Orchestration Metrics Service (Day 5 - Real-time Monitoring)
    builder.Services.AddSingleton<AcademicAssessment.Web.Services.IOrchestrationMetricsService, AcademicAssessment.Web.Services.OrchestrationMetricsService>();

    // Mathematics Assessment Agent (Phase 3 & 4)
    // Now with LLM-enhanced semantic evaluation
    // Changed to Scoped because it depends on scoped repositories
    builder.Services.AddScoped<AcademicAssessment.Agents.Mathematics.MathematicsAssessmentAgent>(sp =>
    {
        var taskService = sp.GetRequiredService<AcademicAssessment.Agents.Shared.Interfaces.ITaskService>();
        var questionRepository = sp.GetRequiredService<AcademicAssessment.Core.Interfaces.IQuestionRepository>();
        var responseRepository = sp.GetRequiredService<AcademicAssessment.Core.Interfaces.IStudentResponseRepository>();
        var assessmentRepository = sp.GetRequiredService<AcademicAssessment.Core.Interfaces.IAssessmentRepository>();
        var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AcademicAssessment.Agents.Mathematics.MathematicsAssessmentAgent>>();
        var llmService = sp.GetRequiredService<AcademicAssessment.Core.Interfaces.ILLMService>();
        return new AcademicAssessment.Agents.Mathematics.MathematicsAssessmentAgent(
            taskService,
            questionRepository,
            responseRepository,
            assessmentRepository,
            logger,
            llmService
        );
    });

    // Physics Assessment Agent (Phase 5)
    // OLLAMA-enhanced semantic evaluation for physics concepts
    builder.Services.AddScoped<AcademicAssessment.Agents.Physics.PhysicsAssessmentAgent>(sp =>
    {
        var llmService = sp.GetRequiredService<AcademicAssessment.Core.Interfaces.ILLMService>();
        return new AcademicAssessment.Agents.Physics.PhysicsAssessmentAgent(llmService);
    });

    // Chemistry Assessment Agent (Phase 5)
    // OLLAMA-enhanced semantic evaluation for chemistry formulas and reactions
    builder.Services.AddScoped<AcademicAssessment.Agents.Chemistry.ChemistryAssessmentAgent>(sp =>
    {
        var llmService = sp.GetRequiredService<AcademicAssessment.Core.Interfaces.ILLMService>();
        return new AcademicAssessment.Agents.Chemistry.ChemistryAssessmentAgent(llmService);
    });

    // Biology Assessment Agent (Phase 5)
    // OLLAMA-enhanced semantic evaluation for biology concepts
    builder.Services.AddScoped<AcademicAssessment.Agents.Biology.BiologyAssessmentAgent>(sp =>
    {
        var llmService = sp.GetRequiredService<AcademicAssessment.Core.Interfaces.ILLMService>();
        return new AcademicAssessment.Agents.Biology.BiologyAssessmentAgent(llmService);
    });

    // English Assessment Agent (Phase 5)
    // OLLAMA-enhanced semantic evaluation - especially powerful for essay evaluation
    builder.Services.AddScoped<AcademicAssessment.Agents.English.EnglishAssessmentAgent>(sp =>
    {
        var llmService = sp.GetRequiredService<AcademicAssessment.Core.Interfaces.ILLMService>();
        return new AcademicAssessment.Agents.English.EnglishAssessmentAgent(llmService);
    });

    Log.Information("A2A Agent infrastructure, orchestrator, and 5 LLM-enhanced subject agents configured (Math, Physics, Chemistry, Biology, English)");

    var app = builder.Build();

    // ============================================================
    // MIDDLEWARE PIPELINE
    // ============================================================

    // Request logging (before any other middleware)
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        };
    });

    // CORS - Must be before routing
    if (app.Environment.IsDevelopment())
    {
        app.UseCors("DevelopmentCors");
    }
    else
    {
        app.UseCors("ProductionCors");
    }

    // Swagger - Development only
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "EduMind.AI API v1");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "EduMind.AI API Documentation";
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
            options.EnableValidator();
        });

        Log.Information("Swagger UI available at: https://localhost:{Port}/swagger",
            builder.Configuration["ASPNETCORE_HTTPS_PORT"] ?? "5001");
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // ============================================================
    // STATIC FILES (for monitoring dashboard)
    // ============================================================
    app.UseStaticFiles();

    // ============================================================
    // HEALTH CHECK ENDPOINTS
    // Note: Aspire's MapDefaultEndpoints() already maps health check endpoints
    // Commenting out custom mappings to avoid ambiguous match errors
    // ============================================================

    /*
    // Basic health check - returns 200 OK if the application is running
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                timestamp = DateTime.UtcNow,
                duration = report.TotalDuration,
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration,
                    exception = e.Value.Exception?.Message,
                    data = e.Value.Data
                })
            }, new JsonSerializerOptions { WriteIndented = true });
            await context.Response.WriteAsync(result);
        }
    }).WithTags("Health Checks")
      .WithOpenApi(operation =>
      {
          operation.Summary = "Comprehensive health check";
          operation.Description = "Returns detailed health status including database and cache connectivity";
          return operation;
      });

    // Readiness check - for Kubernetes readiness probe
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                timestamp = DateTime.UtcNow
            });
            await context.Response.WriteAsync(result);
        }
    }).WithTags("Health Checks")
      .WithOpenApi(operation =>
      {
          operation.Summary = "Readiness probe";
          operation.Description = "Kubernetes readiness probe - checks if the application is ready to serve traffic";
          return operation;
      });

    // Liveness check - for Kubernetes liveness probe
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false, // No checks, just returns if the app is responsive
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow
            }));
        }
    }).WithTags("Health Checks")
      .WithOpenApi(operation =>
      {
          operation.Summary = "Liveness probe";
          operation.Description = "Kubernetes liveness probe - checks if the application is running";
          return operation;
      });
    */

    // ============================================================
    // CONTROLLERS & SIGNALR HUBS
    // ============================================================
    app.MapControllers();

    // A2A Agent Progress Hub - Real-time updates from agents
    app.MapHub<AcademicAssessment.Web.Hubs.AgentProgressHub>("/hubs/agent-progress");

    // Orchestration Monitoring Hub - Real-time orchestration metrics (Day 5)
    app.MapHub<AcademicAssessment.Web.Hubs.OrchestrationHub>("/hubs/orchestration");

    // app.MapHub<AssessmentHub>("/hubs/assessment");
    // app.MapHub<ProgressTrackingHub>("/hubs/progress");

    // ============================================================
    // EXAMPLE ENDPOINT (To be removed when real controllers are added)
    // ============================================================
    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    app.MapGet("/api/v1/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .WithTags("Example")
    .WithOpenApi(operation =>
    {
        operation.Summary = "Get weather forecast";
        operation.Description = "Example endpoint - Returns a 5-day weather forecast (to be removed)";
        return operation;
    });

    // ============================================================
    // INITIALIZE A2A AGENTS (Phase 2 & 3)
    // ============================================================
    Log.Information("Initializing A2A agents...");

    // Get TaskService for agent registration
    var taskService = app.Services.GetRequiredService<AcademicAssessment.Agents.Shared.Interfaces.ITaskService>();

    // Initialize Student Progress Orchestrator (using a scope since it's registered as Scoped)
    using (var scope = app.Services.CreateScope())
    {
        var orchestrator = scope.ServiceProvider.GetRequiredService<AcademicAssessment.Orchestration.StudentProgressOrchestrator>();
        await orchestrator.InitializeAsync();
        Log.Information("Student Progress Orchestrator initialized: {AgentId}", orchestrator.AgentCard.AgentId);
    }

    // Initialize Mathematics Assessment Agent (Phase 3) - using scope since agents are now Scoped
    using (var scope = app.Services.CreateScope())
    {
        var mathAgent = scope.ServiceProvider.GetRequiredService<AcademicAssessment.Agents.Mathematics.MathematicsAssessmentAgent>();
        await mathAgent.InitializeAsync();

        // Register math agent handler with TaskService
        if (taskService is AcademicAssessment.Agents.Shared.Services.TaskService ts)
        {
            ts.RegisterHandler(mathAgent.AgentCard.AgentId, mathAgent.ExecuteTaskAsync);
            Log.Information("Mathematics Assessment Agent initialized and registered: {AgentId}", mathAgent.AgentCard.AgentId);
        }
    }

    // TODO: Initialize additional subject agents (Phase 5)
    // - PhysicsAssessmentAgent
    // - ChemistryAssessmentAgent
    // - BiologyAssessmentAgent
    // - EnglishAssessmentAgent

    // ============================================================
    // START ORCHESTRATION METRICS MONITORING (Day 5)
    // ============================================================
    Log.Information("Starting orchestration metrics monitoring...");
    var metricsService = app.Services.GetRequiredService<AcademicAssessment.Web.Services.IOrchestrationMetricsService>();
    metricsService.StartMonitoring(intervalSeconds: 5);
    Log.Information("Orchestration metrics monitoring started (5s interval)");

    Log.Information("EduMind.AI Web API started successfully");
    Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);
    Log.Information("Listening on: {Urls}", string.Join(", ", builder.Configuration.GetSection("Urls").Get<string[]>() ?? new[] { "https://localhost:5001" }));

    // Map Aspire default endpoints (health checks, OpenTelemetry, etc.)
    app.MapDefaultEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// ============================================================
// DTOs
// ============================================================
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// ============================================================
// SWAGGER HELPERS
// ============================================================
public class SwaggerDefaultValues : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(Microsoft.OpenApi.Models.OpenApiOperation operation, Swashbuckle.AspNetCore.SwaggerGen.OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        // Mark deprecated operations
        if (apiDescription.IsDeprecated())
        {
            operation.Deprecated = true;
        }

        // Clean up response types
        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
            if (operation.Responses.TryGetValue(responseKey, out var response))
            {
                foreach (var contentType in response.Content.Keys.ToList())
                {
                    if (responseType.ApiResponseFormats.All(x => x.MediaType != contentType))
                    {
                        response.Content.Remove(contentType);
                    }
                }
            }
        }

        // Set parameter descriptions and requirements
        if (operation.Parameters != null)
        {
            foreach (var parameter in operation.Parameters)
            {
                var description = apiDescription.ParameterDescriptions
                    .FirstOrDefault(p => p.Name == parameter.Name);

                if (description != null)
                {
                    parameter.Description ??= description.ModelMetadata?.Description;
                    parameter.Required |= description.IsRequired;
                }
            }
        }
    }
}

// Make the Program class accessible to integration tests
public partial class Program { }
