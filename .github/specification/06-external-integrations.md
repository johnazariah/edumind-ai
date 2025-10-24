# External Integrations

**Document Version:** 1.0  
**Last Updated:** October 24, 2025  
**System Version:** 0.2.0

---

## Table of Contents

1. [Overview](#overview)
2. [LLM Integration](#llm-integration)
3. [Azure Services](#azure-services)
4. [Authentication Providers](#authentication-providers)
5. [Monitoring and Observability](#monitoring-and-observability)

---

## 1. Overview

EduMind.AI integrates with multiple external services to provide AI-powered assessment, cloud infrastructure, authentication, and observability.

### Integration Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     EduMind.AI System                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────┐  │
│  │            LLM Providers (AI Services)              │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────┐ │  │
│  │  │   OLLAMA     │  │ Azure OpenAI │  │   Stub   │ │  │
│  │  │   (Local)    │  │   (Cloud)    │  │  (Mock)  │ │  │
│  │  └──────────────┘  └──────────────┘  └──────────┘ │  │
│  └─────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐  │
│  │        Authentication (Identity Providers)          │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────┐ │  │
│  │  │  Azure AD    │  │    Google    │  │  Apple   │ │  │
│  │  │     B2C      │  │    OAuth     │  │   OAuth  │ │  │
│  │  └──────────────┘  └──────────────┘  └──────────┘ │  │
│  └─────────────────────────────────────────────────────┘  │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐  │
│  │           Azure Cloud Services (Planned)            │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────┐ │  │
│  │  │  Key Vault   │  │ Blob Storage │  │ App      │ │  │
│  │  │   (Secrets)  │  │  (Files)     │  │ Insights │ │  │
│  │  └──────────────┘  └──────────────┘  └──────────┘ │  │
│  └─────────────────────────────────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Integration Status

| Service | Status | Environment | Purpose |
|---------|--------|-------------|---------|
| **OLLAMA** | ✅ Operational | Dev/Prod | Local LLM inference |
| **Azure AD B2C** | ✅ Configured | All | Authentication |
| **Azure OpenAI** | ⚠️ Planned | Prod | Cloud LLM fallback |
| **Azure Key Vault** | 📋 Planned | Prod | Secrets management |
| **Azure Blob Storage** | 📋 Planned | Prod | File storage |
| **Application Insights** | 📋 Planned | Prod | Telemetry |

---

## 2. LLM Integration

### 2.1 LLM Service Architecture

EduMind.AI uses a **pluggable LLM provider architecture** with three implementations:

```csharp
public interface ILLMService
{
    Task<Result<List<GeneratedQuestion>>> GenerateQuestionsAsync(...);
    Task<Result<AnswerEvaluation>> EvaluateAnswerAsync(...);
    Task<Result<string>> GenerateFeedbackAsync(...);
    Task<Result<StudyRecommendation>> GenerateStudyRecommendationsAsync(...);
}
```

**Implementations:**

1. `OllamaService` - Local AI (default)
2. `StubLLMService` - Mock for testing
3. `AzureOpenAIService` - Cloud AI (planned)

### 2.2 OLLAMA Integration (Local LLM)

#### Overview

**OLLAMA** provides zero-cost, privacy-focused AI inference using open-source models running locally.

**Benefits:**

- ✅ **Zero cost** - No API charges
- ✅ **Privacy** - Student data never leaves infrastructure
- ✅ **Low latency** - Local processing (50-200ms)
- ✅ **Offline capable** - No internet dependency
- ✅ **Customizable** - Model selection and fine-tuning

#### Configuration

**appsettings.json:**

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "ModelName": "llama3.2:3b",
    "Timeout": 120
  },
  "LLM": {
    "Provider": "Ollama"
  }
}
```

**Environment-Specific:**

| Environment | Model | Base URL | Purpose |
|-------------|-------|----------|---------|
| **Development** | `llama3.2:3b` | `localhost:11434` | Fast iteration (2GB RAM) |
| **Production** | `llama3.2:7b` | `ollama-server:11434` | Higher quality (4GB RAM) |
| **Performance** | `llama3.2:70b` | GPU server | Best quality (40GB VRAM) |

#### Implementation

**File:** `src/AcademicAssessment.Infrastructure/ExternalServices/OllamaService.cs`

```csharp
public class OllamaService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _modelName;

    public OllamaService(IConfiguration configuration, ILogger<OllamaService> logger)
    {
        _baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _modelName = configuration["Ollama:ModelName"] ?? "llama3.2:3b";
        _httpClient.Timeout = TimeSpan.FromMinutes(2);
    }

    private async Task<string> CallOllamaAsync(string prompt, CancellationToken ct)
    {
        var request = new OllamaGenerateRequest
        {
            Model = _modelName,
            Prompt = prompt,
            Stream = false
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/generate", request, ct);
        
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(ct);
        
        return result?.Response ?? string.Empty;
    }
}
```

#### API Endpoints

**OLLAMA REST API:**

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/generate` | POST | Generate text completion |
| `/api/chat` | POST | Chat-style interaction |
| `/api/tags` | GET | List available models |
| `/api/show` | POST | Get model details |

#### Model Selection

| Model | Size | RAM Required | Use Case |
|-------|------|--------------|----------|
| `llama3.2:1b` | 1.3 GB | 2 GB | Minimal testing |
| `llama3.2:3b` | 2.0 GB | 4 GB | **Development default** |
| `llama3.2:7b` | 4.7 GB | 8 GB | **Production recommended** |
| `mistral:7b` | 4.1 GB | 8 GB | Alternative (faster) |
| `llama3.2:70b` | 40 GB | 64 GB | Maximum quality (GPU) |

#### Performance Characteristics

**Benchmarks (llama3.2:3b on 8-core CPU):**

- Question generation: 5-10 seconds
- Answer evaluation: 2-3 seconds
- Feedback generation: 3-5 seconds
- Total assessment: 30-60 seconds (10 questions)

**Production Optimizations:**

- GPU acceleration (NVIDIA/AMD): 10-20x faster
- Model caching: First request slow, subsequent fast
- Batch processing: Evaluate multiple answers together

#### Installation

**Dev Container (Pre-installed):**

```bash
# Already included in .devcontainer/devcontainer.json
ollama --version  # Should show: ollama version is 0.12.5
```

**Manual Installation:**

```bash
# Linux/Mac
curl -fsSL https://ollama.com/install.sh | sh

# Pull model
ollama pull llama3.2:3b

# Start server
ollama serve
```

**Docker:**

```yaml
ollama:
  image: ollama/ollama:latest
  ports:
    - "11434:11434"
  volumes:
    - ollama-data:/root/.ollama
```

#### Error Handling

```csharp
try
{
    var result = await _ollamaService.EvaluateAnswerAsync(...);
    if (result.IsSuccess)
    {
        return result.Value;
    }
    
    // Fallback to exact match if OLLAMA fails
    _logger.LogWarning("OLLAMA unavailable, using exact match");
    return ExactMatchEvaluation(studentAnswer, correctAnswer);
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "OLLAMA server unreachable");
    throw new LLMServiceException("OLLAMA unavailable", ex);
}
```

### 2.3 StubLLMService (Mock Provider)

**Purpose:** Testing and CI/CD pipelines without AI dependency.

**File:** `src/AcademicAssessment.Infrastructure/ExternalServices/StubLLMService.cs`

**Behavior:**

- **Question Generation:** Returns template questions
- **Answer Evaluation:** Case-insensitive exact match
- **Feedback:** Template responses based on correctness
- **Zero latency:** Synchronous, immediate responses

**Configuration:**

```json
{
  "LLM": {
    "Provider": "Stub"
  }
}
```

**Use Cases:**

- ✅ Unit tests (fast, deterministic)
- ✅ CI/CD pipelines (no external dependencies)
- ✅ Local development without OLLAMA
- ✅ Load testing (no LLM bottleneck)

### 2.4 Azure OpenAI Integration (Planned)

#### Overview

**Azure OpenAI** provides cloud-based GPT-4o inference for production fallback or primary provider.

**Benefits:**

- ✅ **Highest quality** - GPT-4o accuracy
- ✅ **Scalability** - Auto-scaling to 1000+ RPS
- ✅ **Reliability** - 99.9% SLA
- ✅ **Enterprise features** - Content filtering, compliance

**Trade-offs:**

- ❌ **Cost** - ~$0.01 per evaluation
- ❌ **Latency** - 500-1000ms (network overhead)
- ❌ **Privacy** - Data sent to Azure (encrypted)

#### Configuration (Future)

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://edumind-openai.openai.azure.com/",
    "DeploymentName": "gpt-4o",
    "ApiVersion": "2024-02-01",
    "MaxTokens": 2000,
    "Temperature": 0.7
  },
  "LLM": {
    "Provider": "AzureOpenAI"
  }
}
```

#### Authentication

**Development:**

```csharp
var credential = new AzureKeyCredential(configuration["AzureOpenAI:ApiKey"]);
var client = new OpenAIClient(endpoint, credential);
```

**Production (Managed Identity):**

```csharp
var credential = new DefaultAzureCredential();
var client = new OpenAIClient(endpoint, credential);
```

#### Cost Management

**Pricing (GPT-4o, October 2025):**

- Input: $5 per 1M tokens
- Output: $15 per 1M tokens
- Average evaluation: 500 input + 200 output tokens = $0.0055

**Monthly Cost Estimates:**

| Scenario | Assessments/Month | Cost/Month |
|----------|-------------------|------------|
| Small school (100 students) | 4,000 | $22 |
| Medium school (500 students) | 20,000 | $110 |
| Large school (1000 students) | 40,000 | $220 |
| Self-service (10K users) | 100,000 | $550 |

**Cost Optimization:**

- Use OLLAMA for 90% of requests
- Fallback to Azure for complex cases only
- Cache LLM responses (15-minute TTL)
- Batch evaluation requests

### 2.5 Hybrid LLM Strategy (Recommended for Production)

**Architecture:**

```
┌─────────────────────────────────────────────┐
│         HybridLLMService (Facade)           │
├─────────────────────────────────────────────┤
│                                             │
│  Try Primary (OLLAMA)                       │
│       ↓                                     │
│  [Success?] → Return result                 │
│       ↓ No                                  │
│  [Retry 3x with backoff]                    │
│       ↓ Still failing                       │
│  Fallback to Secondary (Azure OpenAI)       │
│       ↓                                     │
│  Return result                              │
│                                             │
└─────────────────────────────────────────────┘
```

**Benefits:**

- **95% cost savings** (OLLAMA handles most requests)
- **Reliability** (Azure fallback for OLLAMA outages)
- **Best-of-both** (privacy + quality assurance)

**Configuration:**

```json
{
  "LLM": {
    "Provider": "Hybrid",
    "Primary": "Ollama",
    "Fallback": "AzureOpenAI",
    "FallbackThreshold": 3,
    "RetryDelayMs": 1000
  }
}
```

---

## 3. Azure Services

### 3.1 Azure PostgreSQL Flexible Server

**Status:** ✅ Operational (Development & Production)

**Configuration:**

- **Tier:** General Purpose
- **Compute:** 2-4 vCores (auto-scale)
- **Storage:** 128 GB (auto-grow)
- **Backup:** 35-day PITR, geo-redundant
- **High Availability:** Zone-redundant
- **Version:** PostgreSQL 16

**Connection:**

```json
{
  "ConnectionStrings": {
    "AcademicDatabase": "Host=edumind-postgres.postgres.database.azure.com;Port=5432;Database=edumind_selfservice;Username=edumindadmin;Password=***;SSL Mode=Require"
  }
}
```

**See:** 05-data-storage.md for complete details.

### 3.2 Azure Cache for Redis

**Status:** ✅ Operational (Development & Production)

**Configuration:**

- **Tier:** Standard C1 (1 GB)
- **Persistence:** RDB snapshots (15 min)
- **High Availability:** Zone-redundant
- **SSL:** Required (TLS 1.2+)
- **Access:** Private endpoint

**Connection:**

```json
{
  "ConnectionStrings": {
    "Redis": "edumind-cache.redis.cache.windows.net:6380,password=***,ssl=True,abortConnect=False"
  }
}
```

**See:** 05-data-storage.md for caching strategy.

### 3.3 Azure Key Vault (Planned)

**Status:** 📋 Planned

**Purpose:** Secure secrets management for production.

**Secrets to Store:**

- PostgreSQL connection strings (per school)
- Redis connection strings
- Azure OpenAI API keys
- Azure AD B2C client secrets
- Encryption keys

**Access Pattern:**

```csharp
// Retrieve secret at runtime
var client = new SecretClient(
    new Uri("https://edumind-keyvault.vault.azure.net/"),
    new DefaultAzureCredential());

var secret = await client.GetSecretAsync("PostgreSQL-School-00001");
var connectionString = secret.Value.Value;
```

**Benefits:**

- ✅ Secrets never in source code
- ✅ Automatic rotation support
- ✅ Audit logging of access
- ✅ RBAC for secret access

### 3.4 Azure Blob Storage (Planned)

**Status:** 📋 Planned

**Purpose:** Store student uploads and question attachments.

**Use Cases:**

- Question images/diagrams
- Student essay uploads
- Assessment PDFs
- Backup exports

**Storage Structure:**

```
edumind-storage/
├── questions/
│   └── {questionId}/
│       ├── diagram.png
│       └── reference.pdf
├── student-uploads/
│   └── {schoolId}/
│       └── {studentId}/
│           └── {assessmentId}/
│               └── essay.pdf
└── exports/
    └── database-backups/
        └── {date}/
```

**Access Control:**

- Questions: Public read, admin write
- Student uploads: Student read/write, teacher read
- Exports: Admin only

### 3.5 Application Insights (Planned)

**Status:** 📋 Planned

**Purpose:** Centralized telemetry, logging, and monitoring.

**Telemetry Types:**

- Request tracking (API calls)
- Dependency tracking (PostgreSQL, Redis, OLLAMA)
- Exception logging
- Custom events (assessment started, completed)
- Performance counters

**Configuration:**

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=***;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/"
  }
}
```

**Integration:**

```csharp
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["ApplicationInsights:ConnectionString"]);
```

---

## 4. Authentication Providers

### 4.1 Azure AD B2C

**Status:** ✅ Configured

**Purpose:** Primary authentication provider for all users.

**Configuration:**

```json
{
  "AzureAdB2C": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "edumindai.onmicrosoft.com",
    "TenantId": "common",
    "ClientId": "{client-id}",
    "SignUpSignInPolicyId": "B2C_1_susi_google",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

**Supported Identity Providers:**

- ✅ Email/Password (local accounts)
- ✅ Google OAuth
- ✅ Apple OAuth (planned)
- ✅ Microsoft Account (planned)

**User Flows:**

| Flow | Policy ID | Purpose |
|------|-----------|---------|
| Sign Up/Sign In | `B2C_1_susi_google` | Primary authentication |
| Password Reset | `B2C_1_password_reset` | Self-service password recovery |
| Profile Edit | `B2C_1_profile_edit` | Update user profile |

**Claims Mapping:**

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAdB2C"))
    .AddMicrosoftIdentityWebApiCallsWebApi(configuration.GetSection("AzureAdB2C"));

// Custom claims
services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.NameClaimType = "name";
    options.TokenValidationParameters.RoleClaimType = "extension_Role";
});
```

**See:** 07-security-privacy.md for complete authentication details.

### 4.2 Google OAuth

**Status:** ✅ Operational (via Azure AD B2C)

**Configuration:**

- Integrated into Azure AD B2C user flow
- Google Developer Console project: `edumind-ai`
- Scopes: `openid`, `profile`, `email`

### 4.3 Apple OAuth (Planned)

**Status:** 📋 Planned

**Purpose:** iOS/macOS native authentication.

**Requirements:**

- Apple Developer account
- App ID configuration
- Private key for Sign in with Apple

---

## 5. Monitoring and Observability

### 5.1 OpenTelemetry

**Status:** ✅ Operational (via .NET Aspire)

**Instrumentation:**

```csharp
// Automatic tracing for:
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddRedisInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation());
```

**Exported To:**

- Console (development)
- OTLP endpoint (production)
- Application Insights (future)

### 5.2 Health Checks

**Status:** ✅ Operational

**Endpoints:**

- `/health` - Basic health check
- `/health/ready` - Readiness probe (K8s)
- `/health/live` - Liveness probe (K8s)

**Checks:**

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres")
    .AddRedis(redisConnection, name: "redis")
    .AddUrlGroup(new Uri("http://localhost:11434/api/tags"), name: "ollama");
```

### 5.3 Structured Logging

**Status:** ✅ Operational (Serilog)

**Configuration:**

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/edumind-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

**Log Levels:**

- **Debug:** Development diagnostics
- **Information:** Normal operations (default)
- **Warning:** Recoverable issues (OLLAMA fallback)
- **Error:** Failures requiring attention
- **Critical:** System-wide failures

---

## Related Documentation

- **04-application-components.md** - Infrastructure component implementation
- **05-data-storage.md** - PostgreSQL and Redis configuration
- **07-security-privacy.md** - Authentication and authorization details
- **08-observability.md** - Monitoring and logging strategies
- **12-performance.md** - LLM performance benchmarks

---

**Integration Status:** 70% Complete (Core services operational, Azure services planned)  
**LLM Provider:** OLLAMA (llama3.2:3b)  
**Authentication:** Azure AD B2C + Google OAuth
