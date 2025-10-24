# Observability Specification

> **Version:** 1.0  
> **Last Updated:** January 2025  
> **Status:** Living Document

## Table of Contents

1. [Overview](#1-overview)
2. [Architecture](#2-architecture)
3. [Distributed Tracing](#3-distributed-tracing)
4. [Metrics Collection](#4-metrics-collection)
5. [Structured Logging](#5-structured-logging)
6. [Health Checks](#6-health-checks)
7. [Alerting](#7-alerting)
8. [Performance Targets](#8-performance-targets)
9. [Implementation Details](#9-implementation-details)

---

## 1. Overview

### Purpose

This document defines the comprehensive observability strategy for EduMind.AI, ensuring system health monitoring, performance tracking, and debugging capabilities across all application components.

### Observability Pillars

| Pillar | Technology | Purpose | Status |
|--------|------------|---------|--------|
| **Traces** | OpenTelemetry | Request flow tracking | ✅ Implemented |
| **Metrics** | OpenTelemetry + Prometheus | Performance metrics | ✅ Implemented |
| **Logs** | Serilog | Structured logging | ✅ Implemented |
| **Health** | ASP.NET Core Health Checks | Service availability | ✅ Implemented |

### Observability Status

| Component | Status | Notes |
|-----------|--------|-------|
| OpenTelemetry Tracing | ✅ Operational | ASP.NET Core, HTTP client, runtime |
| OpenTelemetry Metrics | ✅ Operational | ASP.NET Core, HTTP client, runtime |
| Serilog Logging | ✅ Operational | Console + file sinks, structured |
| Health Checks | ✅ Operational | `/health`, `/alive` endpoints |
| OTLP Exporter | ⚠️ Partial | Configured, needs endpoint |
| Application Insights | 📋 Planned | Azure integration |
| Prometheus Exporter | 📋 Planned | `/metrics` endpoint |
| Custom Metrics | 📋 Planned | LLM, agents, analytics |
| Grafana Dashboards | 📋 Planned | Visualization |

### Related Documents

- [External Integrations](06-external-integrations.md) - OpenTelemetry configuration
- [System Architecture](02-system-architecture.md) - Component overview
- [Deployment Models](13-deployment-models.md) - Monitoring in deployment

---

## 2. Architecture

### Observability Stack

```
┌─────────────────────────────────────────────────────────────┐
│                     EduMind.AI Applications                  │
│  (Web API, Dashboard, Student App)                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │OpenTelemetry │  │  Prometheus  │  │   Serilog    │     │
│  │   Tracing    │  │   Metrics    │  │   Logging    │     │
│  │  Metrics     │  │  (Planned)   │  │              │     │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘     │
│         │                  │                  │              │
└─────────┼──────────────────┼──────────────────┼─────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
    ┌──────────┐       ┌──────────┐      ┌──────────┐
    │   OTLP   │       │ Grafana  │      │ Log Files│
    │ Collector│       │Dashboard │      │ Console  │
    │ (Future) │       │ (Planned)│      │ (Active) │
    └────┬─────┘       └──────────┘      └──────────┘
         │
         ▼
    ┌──────────┐
    │   Jaeger │
    │  Zipkin  │
    │App Insights
    │ (Planned)│
    └──────────┘
```

### Data Flow

1. **Application Code** generates telemetry (traces, metrics, logs)
2. **OpenTelemetry SDK** collects and batches telemetry
3. **Exporters** send data to:
   - Console (development)
   - OTLP endpoint (production, when configured)
   - Application Insights (planned)
4. **Serilog** writes structured logs to:
   - Console output (always)
   - Rolling file logs (`logs/edumind-YYYYMMDD.log`)
5. **Aspire Dashboard** aggregates all telemetry (local development)

### Integration with .NET Aspire

EduMind.AI uses **.NET Aspire** for streamlined observability:

- **Automatic instrumentation** - No manual OpenTelemetry setup per service
- **Unified dashboard** - All services visible in one place (https://localhost:17191)
- **Built-in resilience** - Polly policies integrated
- **Service discovery** - Automatic endpoint resolution

---

## 3. Distributed Tracing

### 3.1 OpenTelemetry Tracing

**Purpose:** Track request flow through the system, identify slow operations and bottlenecks.

**Implementation Location:** `src/EduMind.ServiceDefaults/Extensions.cs`

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());
```

### 3.2 Instrumented Components

#### Automatic Instrumentation

| Component | Instrumentation | What's Tracked |
|-----------|-----------------|----------------|
| **ASP.NET Core** | `AddAspNetCoreInstrumentation()` | HTTP requests, routing, middleware |
| **HTTP Client** | `AddHttpClientInstrumentation()` | Outgoing HTTP calls (OLLAMA, Azure) |
| **.NET Runtime** | `AddRuntimeInstrumentation()` (metrics) | GC, thread pool, exceptions |

#### Trace Hierarchy Example

```
Span: POST /api/v1/students/{id}/analytics/performance-summary
├─ Span: TenantContext.Initialize
├─ Span: PostgreSQL Query - SELECT Students
├─ Span: PostgreSQL Query - SELECT StudentAssessments
├─ Span: HTTP POST to OLLAMA (llama3.2:3b)
│  └─ Span: LLM Evaluation (30.2s)
├─ Span: Cache SET (Redis)
└─ Span: SignalR Hub Send
```

### 3.3 Custom Spans (Planned)

**Future implementation for fine-grained tracing:**

```csharp
using var activity = Activity.StartActivity("Agent.Evaluate");
activity?.SetTag("agent.name", "PhysicsAgent");
activity?.SetTag("student.id", studentId.ToString());
activity?.SetTag("subject", "Physics");

try
{
    var result = await EvaluateStudentAsync(studentId);
    activity?.SetStatus(ActivityStatusCode.Ok);
    return result;
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    throw;
}
```

**Custom Spans to Add:**

- `agent.evaluate` - Individual agent evaluation
- `orchestrator.route_task` - Task routing decision
- `llm.call` - LLM API call (with provider tag)
- `analytics.calculate` - Performance calculation
- `irt.estimate_ability` - IRT ability estimation
- `cache.get` / `cache.set` - Cache operations

### 3.4 Trace Exporters

#### Development (Console)

```bash
# Traces output to console in structured format
info: OpenTelemetry.Trace.TraceExporter[0]
      Activity.Id:          00-a1b2c3d4...
      Activity.SpanId:      abc123
      Activity.TraceId:     a1b2c3d4...
      Activity.ParentSpanId: def456
      Activity.ActivitySourceName: Microsoft.AspNetCore
      Activity.DisplayName: POST /api/v1/students/{id}/analytics
      Activity.Duration:     00:00:32.145
```

#### Aspire Dashboard (Development)

**URL:** https://localhost:17191

**Features:**

- Live traces across all services
- Span timeline visualization
- Dependency graph
- Performance metrics
- Log correlation

#### Production (OTLP + Application Insights - Planned)

```json
{
  "OTEL_EXPORTER_OTLP_ENDPOINT": "https://otel-collector.example.com",
  "APPLICATIONINSIGHTS_CONNECTION_STRING": "InstrumentationKey=***"
}
```

---

## 4. Metrics Collection

### 4.1 OpenTelemetry Metrics

**Purpose:** Track quantitative performance indicators over time.

**Implementation Location:** `src/EduMind.ServiceDefaults/Extensions.cs`

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation());
```

### 4.2 Built-In Metrics

#### HTTP Server Metrics (ASP.NET Core)

| Metric Name | Type | Description | Labels |
|-------------|------|-------------|--------|
| `http.server.request.duration` | Histogram | Request duration (ms) | `http.request.method`, `http.route`, `http.response.status_code` |
| `http.server.active_requests` | UpDownCounter | Currently active requests | `http.request.method`, `url.scheme` |
| `http.server.request.body.size` | Histogram | Request body size (bytes) | `http.request.method`, `http.route` |
| `http.server.response.body.size` | Histogram | Response body size (bytes) | `http.request.method`, `http.route`, `http.response.status_code` |

**Example Query (Prometheus):**

```promql
# P95 request latency
histogram_quantile(0.95, 
  rate(http_server_request_duration_bucket[5m]))

# Error rate
rate(http_server_request_duration_count{http_response_status_code=~"5.."}[5m])
  / rate(http_server_request_duration_count[5m])
```

#### HTTP Client Metrics

| Metric Name | Type | Description | Labels |
|-------------|------|-------------|--------|
| `http.client.request.duration` | Histogram | Outgoing request duration (ms) | `http.request.method`, `server.address`, `http.response.status_code` |
| `http.client.active_requests` | UpDownCounter | Active outgoing requests | `http.request.method`, `server.address` |

**Tracks:**
- OLLAMA LLM API calls (`server.address = localhost:11434`)
- Azure OpenAI calls (planned)
- Internal service calls

#### .NET Runtime Metrics

| Metric Name | Type | Description |
|-------------|------|-------------|
| `process.runtime.dotnet.gc.collections.count` | Counter | GC collections by generation |
| `process.runtime.dotnet.gc.heap.size` | UpDownCounter | Heap size by generation |
| `process.runtime.dotnet.gc.duration` | Histogram | GC pause duration |
| `process.runtime.dotnet.thread_pool.threads.count` | UpDownCounter | Thread pool size |
| `process.runtime.dotnet.exceptions.count` | Counter | Exception count |

### 4.3 Custom Metrics (Planned)

**Implementation:** Create custom meters using `System.Diagnostics.Metrics` API.

#### LLM Performance Metrics

```csharp
public class LLMMetrics
{
    private readonly Meter _meter = new("EduMind.LLM", "1.0.0");
    private readonly Histogram<double> _llmCallDuration;
    private readonly Counter<long> _llmCallsTotal;
    private readonly UpDownCounter<int> _llmQueueLength;

    public LLMMetrics()
    {
        _llmCallDuration = _meter.CreateHistogram<double>(
            "llm.call.duration",
            unit: "s",
            description: "LLM call duration in seconds");

        _llmCallsTotal = _meter.CreateCounter<long>(
            "llm.calls.total",
            description: "Total LLM calls");

        _llmQueueLength = _meter.CreateUpDownCounter<int>(
            "llm.queue.length",
            description: "Current LLM queue length");
    }

    public void RecordLLMCall(string provider, string agent, double durationSeconds, bool success)
    {
        _llmCallDuration.Record(durationSeconds, 
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("agent", agent));

        _llmCallsTotal.Add(1,
            new KeyValuePair<string, object?>("provider", provider),
            new KeyValuePair<string, object?>("agent", agent),
            new KeyValuePair<string, object?>("status", success ? "success" : "error"));
    }
}
```

**Metrics to Implement:**

| Metric | Type | Labels | Purpose |
|--------|------|--------|---------|
| `llm.call.duration` | Histogram | `provider`, `agent`, `model` | LLM response time |
| `llm.calls.total` | Counter | `provider`, `agent`, `status` | LLM call count |
| `llm.queue.length` | UpDownCounter | `provider` | Pending LLM requests |
| `agent.evaluation.duration` | Histogram | `agent`, `subject` | Agent evaluation time |
| `agent.tasks.assigned` | Counter | `agent`, `priority` | Tasks routed to agents |
| `db.query.duration` | Histogram | `operation`, `table` | Database query time |
| `cache.operations.total` | Counter | `operation`, `cache` | Cache hit/miss/set |
| `analytics.calculation.duration` | Histogram | `type` | Analytics query time |
| `assessment.completed.total` | Counter | `subject`, `grade_level` | Assessments completed |

#### Agent Orchestration Metrics

```csharp
public class OrchestrationMetrics
{
    private readonly Histogram<double> _agentEvaluationDuration;
    private readonly Counter<long> _taskAssignments;
    private readonly UpDownCounter<int> _activeAgents;

    // Record metrics
    public void RecordTaskAssignment(string agentName, string priority)
    {
        _taskAssignments.Add(1,
            new("agent", agentName),
            new("priority", priority));
    }
}
```

### 4.4 Metrics Export

#### Console (Development)

Metrics logged to console every 10 seconds (configurable).

#### Prometheus (Planned)

**Endpoint:** `/metrics`

**Configuration:**

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());

app.MapPrometheusScrapingEndpoint(); // /metrics
```

**Prometheus Scrape Config:**

```yaml
scrape_configs:
  - job_name: 'edumind-api'
    scrape_interval: 15s
    static_configs:
      - targets: ['localhost:5103']
```

#### Application Insights (Planned)

```csharp
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor();
```

---

## 5. Structured Logging

### 5.1 Serilog Configuration

**Purpose:** Capture contextual log data with structured fields for querying and analysis.

**Implementation Location:** `src/AcademicAssessment.Web/Program.cs`

```csharp
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
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] [{RequestId}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30)
    .CreateLogger();
```

### 5.2 Log Levels

| Level | Use Case | Examples |
|-------|----------|----------|
| **Verbose** | Detailed tracing | Loop iterations, variable values |
| **Debug** | Development diagnostics | Method entry/exit, internal state |
| **Information** | General flow | API requests, agent decisions, task routing |
| **Warning** | Unexpected but recoverable | Slow LLM response, cache miss, retry triggered |
| **Error** | Failures | LLM timeout, DB connection lost, unhandled exceptions |
| **Fatal** | Application crash | Unrecoverable errors causing shutdown |

**Log Level Configuration:**

| Source | Level | Reason |
|--------|-------|--------|
| Application (EduMind.*) | Debug | Full visibility during development |
| Microsoft.* | Information | Reduce noise from framework |
| Microsoft.AspNetCore | Warning | Only show important ASP.NET messages |
| System.* | Warning | Reduce noise from system libraries |
| Microsoft.EntityFrameworkCore | Warning | Only show EF Core errors (queries at Debug) |

### 5.3 Structured Logging Examples

#### Good: Structured with Properties

```csharp
_logger.LogInformation("LLM evaluation started for {Agent} with {Provider}, estimated duration {EstimatedDuration}s",
    agentName, providerName, estimatedDuration);
// Output: LLM evaluation started for PhysicsAgent with OLLAMA, estimated duration 25s
// Queryable fields: Agent="PhysicsAgent", Provider="OLLAMA", EstimatedDuration=25
```

```csharp
_logger.LogWarning("Slow LLM response: {Duration}ms from {Provider} for {Agent} (threshold: {Threshold}ms)",
    duration, provider, agent, thresholdMs);
// Output: Slow LLM response: 35000ms from OLLAMA for PhysicsAgent (threshold: 30000ms)
```

```csharp
_logger.LogError(ex, "LLM call failed for {Agent} after {Retries} retries, falling back to {FallbackProvider}",
    agentName, retryCount, fallbackProvider);
// Output: LLM call failed for PhysicsAgent after 3 retries, falling back to StubLLMService
```

#### Bad: String Interpolation (Not Queryable)

```csharp
// ❌ Don't do this - loses structure
_logger.LogInformation($"LLM evaluation started for {agentName} with {providerName}");
```

### 5.4 Log Enrichers

**Enrichers automatically add contextual data to every log entry:**

| Enricher | Source | Example Value | Purpose |
|----------|--------|---------------|---------|
| `MachineName` | Serilog | `devcontainer-123` | Identify server in multi-instance deployment |
| `EnvironmentName` | ASP.NET Core | `Development`, `Production` | Distinguish environments |
| `ThreadId` | Serilog | `42` | Track concurrency issues |
| `RequestId` | ASP.NET Core | `0HN7G...` | Correlate logs within a request |
| `SourceContext` | Serilog | `AcademicAssessment.Web.Controllers.StudentAnalyticsController` | Identify log source |

### 5.5 Log Sinks

#### Console Sink

**Status:** ✅ Active  
**Use:** Development visibility, Docker logs

```
[2025-01-20 10:15:32.456 +00:00] [INF] [AcademicAssessment.Web.Program] Starting EduMind.AI Web API
[2025-01-20 10:15:32.789 +00:00] [INF] [Microsoft.Hosting.Lifetime] Now listening on: http://[::]:8080
```

#### File Sink

**Status:** ✅ Active  
**Use:** Persistent logs for debugging, auditing

**Path:** `logs/edumind-20250120.log`  
**Rolling:** Daily  
**Retention:** 30 days  
**Size Limit:** 10 MB per file (rolls over to `.001`, `.002`, etc.)

```
2025-01-20 10:15:32.456 +00:00 [INF] [AcademicAssessment.Web.Program] [0HN7G...] Starting EduMind.AI Web API
```

#### Future Sinks

- **Application Insights** - Azure telemetry
- **Seq** - Structured log search and analysis
- **Elasticsearch** - Centralized log aggregation

### 5.6 Logging Best Practices

**DO:**
- ✅ Use structured logging with named properties: `{PropertyName}`
- ✅ Log at appropriate levels (Info for normal flow, Warning for issues)
- ✅ Include exception object: `_logger.LogError(ex, "Message")`
- ✅ Use semantic property names: `{StudentId}`, `{AgentName}`
- ✅ Log entry and exit of critical operations (LLM calls, DB queries)

**DON'T:**
- ❌ Use string interpolation: `$"Message {variable}"`
- ❌ Log sensitive data: passwords, tokens, full PII
- ❌ Over-log in tight loops (use Debug level sparingly)
- ❌ Log without context (always include relevant IDs)

---

## 6. Health Checks

### 6.1 Health Check Endpoints

**Implementation Location:** `src/EduMind.ServiceDefaults/Extensions.cs`, `src/AcademicAssessment.Web/Program.cs`

| Endpoint | Purpose | Use Case | Status |
|----------|---------|----------|--------|
| `/health` | Overall health | Manual checks, load balancer | ✅ Implemented |
| `/alive` | Liveness probe | Kubernetes liveness | ✅ Implemented |
| `/health/ready` | Readiness probe | Kubernetes readiness | ✅ Implemented (Web API) |

### 6.2 Health Check Configuration

#### Basic Health Check (ServiceDefaults)

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
```

**Result:**

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0012345"
}
```

#### Detailed Health Checks (Web API)

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: postgresConnection,
        name: "PostgreSQL",
        tags: new[] { "db", "ready" })
    .AddRedis(
        connectionString: redisConnection,
        name: "Redis",
        tags: new[] { "cache", "ready" });
```

**Result (`/health` with details):**

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "PostgreSQL": {
      "status": "Healthy",
      "duration": "00:00:00.0123456",
      "tags": ["db", "ready"]
    },
    "Redis": {
      "status": "Healthy",
      "duration": "00:00:00.0011111",
      "tags": ["cache", "ready"]
    },
    "self": {
      "status": "Healthy",
      "duration": "00:00:00.0000001",
      "tags": ["live"]
    }
  }
}
```

### 6.3 Health Check Tags

**Tags filter which checks run for each endpoint:**

| Tag | Checked By | Purpose |
|-----|------------|---------|
| `live` | `/alive` | Liveness - "Is the process running?" |
| `ready` | `/health/ready` | Readiness - "Can the service handle requests?" |
| `db` | `/health/ready` | Database connectivity |
| `cache` | `/health/ready` | Redis connectivity |

**Kubernetes Configuration:**

```yaml
livenessProbe:
  httpGet:
    path: /alive
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
```

### 6.4 Custom Health Checks (Future)

**OLLAMA Health Check:**

```csharp
public class OllamaHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("http://localhost:11434/api/tags", cancellationToken);
            
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("OLLAMA is responding")
                : HealthCheckResult.Degraded($"OLLAMA returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("OLLAMA is unreachable", ex);
        }
    }
}

// Register
builder.Services.AddHealthChecks()
    .AddCheck<OllamaHealthCheck>("OLLAMA", tags: new[] { "llm", "ready" });
```

---

## 7. Alerting

### 7.1 Alert Rules (Planned)

**Implementation:** Prometheus Alertmanager or Azure Monitor alerts.

#### Critical Alerts

**High Error Rate:**

```yaml
- alert: HighErrorRate
  expr: rate(http_server_request_duration_count{http_response_status_code=~"5.."}[5m]) > 0.05
  for: 5m
  labels:
    severity: critical
  annotations:
    summary: "High error rate detected"
    description: "Error rate is {{ $value | humanizePercentage }} (threshold: 5%)"
```

**LLM Timeout:**

```yaml
- alert: LLMTimeouts
  expr: rate(llm_calls_total{status="timeout"}[5m]) > 10
  for: 5m
  labels:
    severity: critical
  annotations:
    summary: "LLM timeouts detected"
    description: "{{ $value }} LLM timeouts in last 5 minutes"
```

**Database Connection Pool Exhausted:**

```yaml
- alert: DatabaseConnectionPoolExhausted
  expr: db_connection_pool_available == 0
  for: 1m
  labels:
    severity: critical
  annotations:
    summary: "Database connection pool exhausted"
    description: "No available database connections"
```

#### Warning Alerts

**Slow LLM Response:**

```yaml
- alert: SlowLLMResponse
  expr: histogram_quantile(0.95, rate(llm_call_duration_bucket[5m])) > 60
  for: 10m
  labels:
    severity: warning
  annotations:
    summary: "LLM response time is slow"
    description: "P95 LLM response time: {{ $value }}s (threshold: 60s)"
```

**High Cache Miss Rate:**

```yaml
- alert: HighCacheMissRate
  expr: rate(cache_operations_total{operation="miss"}[5m]) / rate(cache_operations_total[5m]) > 0.5
  for: 10m
  labels:
    severity: warning
  annotations:
    summary: "Cache miss rate is high"
    description: "Cache miss rate: {{ $value | humanizePercentage }}"
```

### 7.2 Alert Channels (Planned)

- **Email** - Critical alerts to on-call team
- **Slack** - All alerts to #edumind-alerts channel
- **PagerDuty** - Critical alerts with escalation
- **Azure Monitor** - Native Azure alerting

---

## 8. Performance Targets

### 8.1 Key Performance Indicators (KPIs)

| Metric | Target | Warning | Critical | Current |
|--------|--------|---------|----------|---------|
| **API Response Time (P95)** | < 500ms | > 1s | > 3s | TBD |
| **LLM Call Time (P95)** | < 30s | > 60s | > 120s | ~30s |
| **Error Rate** | < 0.1% | > 1% | > 5% | TBD |
| **Cache Hit Rate** | > 80% | < 50% | < 30% | TBD |
| **Database Query Time (P95)** | < 100ms | > 500ms | > 1s | TBD |
| **Agent Task Assignment** | < 1s | > 3s | > 10s | TBD |
| **Assessment Completion** | < 30min | > 45min | > 60min | TBD |

### 8.2 Latency Budgets

| Operation | Budget | Rationale |
|-----------|--------|-----------|
| Student login | 500ms | User interaction, needs to feel instant |
| Load assessment | 1s | Includes question retrieval and rendering |
| Submit answer | 200ms | Frequent operation, needs quick feedback |
| LLM evaluation | 30s | Background task, acceptable wait |
| Analytics dashboard | 2s | Complex queries, user expects slight delay |
| Generate report | 5s | Heavy computation, explicit user action |

---

## 9. Implementation Details

### 9.1 Key Files

| File Path | Purpose |
|-----------|---------|
| `src/EduMind.ServiceDefaults/Extensions.cs` | OpenTelemetry and health check configuration |
| `src/AcademicAssessment.Web/Program.cs` | Serilog configuration, health check endpoints |
| `logs/edumind-YYYYMMDD.log` | Daily rolling log files |
| `appsettings.json` | Logging and observability configuration |

### 9.2 Configuration

**appsettings.json (Observability Section):**

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/edumind-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "fileSizeLimitBytes": 10485760,
          "rollOnFileSizeLimit": true,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] [{RequestId}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId", "WithEnvironmentName"]
  }
}
```

**Environment Variables:**

```bash
# Development
ASPNETCORE_ENVIRONMENT=Development
OTEL_EXPORTER_OTLP_ENDPOINT=  # Empty = console export only

# Production (Planned)
ASPNETCORE_ENVIRONMENT=Production
OTEL_EXPORTER_OTLP_ENDPOINT=https://otel-collector.example.com
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=***
```

### 9.3 Aspire Dashboard

**Local Development URL:** https://localhost:17191

**Features:**

- **Traces** - Distributed tracing across all services
- **Metrics** - Real-time metrics and charts
- **Logs** - Structured log aggregation
- **Services** - Service discovery and health
- **Resources** - Container Apps, PostgreSQL, Redis status

**Access:**

```bash
# Start Aspire AppHost
dotnet run --project src/EduMind.AppHost

# Dashboard opens automatically at https://localhost:17191
```

### 9.4 Testing Observability

#### Verify Health Checks

```bash
# Basic health
curl http://localhost:5103/health

# Liveness
curl http://localhost:5103/alive

# Readiness (with details)
curl http://localhost:5103/health/ready
```

#### Verify Logs

```bash
# View console logs
docker logs <container-id> -f

# View file logs
tail -f logs/edumind-$(date +%Y%m%d).log

# Search logs
grep "LLM evaluation" logs/edumind-*.log
```

#### Verify Metrics (Future)

```bash
# Prometheus metrics endpoint
curl http://localhost:5103/metrics

# Should output Prometheus format:
# # HELP http_server_request_duration_seconds HTTP request duration
# # TYPE http_server_request_duration_seconds histogram
# http_server_request_duration_seconds_bucket{http_request_method="GET",http_route="/health",le="0.1"} 42
```

#### Verify Traces

```bash
# Access Aspire Dashboard
# https://localhost:17191
# Navigate to "Traces" tab
# Filter by service: AcademicAssessment.Web
# View trace details with span timeline
```

### 9.5 Troubleshooting Guide

#### Issue: No Logs in Console

**Symptoms:**
- Console output empty
- Application appears to start but no log messages

**Investigation:**
```bash
# Check Serilog configuration
grep "MinimumLevel" appsettings.json

# Verify console sink
grep "WriteTo.Console" Program.cs
```

**Resolution:**
- Ensure `builder.Host.UseSerilog()` is called
- Check log level not set too high (e.g., `Error`)
- Verify console sink configuration

#### Issue: Traces Not Appearing in Aspire Dashboard

**Symptoms:**
- Aspire Dashboard shows no traces
- Service appears healthy

**Investigation:**
```bash
# Check OpenTelemetry configuration
grep "AddServiceDefaults" Program.cs

# Verify Aspire Dashboard is running
curl https://localhost:17191
```

**Resolution:**
- Ensure `builder.AddServiceDefaults()` is called before `builder.Build()`
- Restart Aspire AppHost
- Check firewall/port binding

#### Issue: Health Check Always Unhealthy

**Symptoms:**
- `/health/ready` returns `Unhealthy`
- PostgreSQL or Redis check fails

**Investigation:**
```bash
# Check database connectivity
psql -h localhost -U edumind_user -d edumind_dev

# Check Redis connectivity
redis-cli -h localhost ping

# View detailed health check response
curl http://localhost:5103/health/ready | jq
```

**Resolution:**
- Verify connection strings in `appsettings.json`
- Ensure PostgreSQL and Redis containers are running
- Check network connectivity
- Review health check timeout settings

---

## Summary

EduMind.AI implements comprehensive observability using modern .NET technologies:

### Operational Telemetry

1. ✅ **OpenTelemetry Tracing** - Request flow tracking (ASP.NET Core, HTTP client)
2. ✅ **OpenTelemetry Metrics** - Performance metrics (HTTP, runtime)
3. ✅ **Serilog Structured Logging** - Contextual logs (console + file)
4. ✅ **Health Checks** - Service availability (`/health`, `/alive`, `/health/ready`)
5. ✅ **Aspire Dashboard** - Unified local development observability

### Planned Enhancements

1. 📋 **Prometheus Exporter** - Expose `/metrics` endpoint
2. 📋 **Custom Metrics** - LLM, agent orchestration, analytics performance
3. 📋 **Application Insights** - Azure-native telemetry
4. 📋 **Grafana Dashboards** - Production visualization
5. 📋 **Alerting** - Prometheus Alertmanager or Azure Monitor alerts
6. 📋 **Custom Spans** - Fine-grained tracing for agents and LLM calls

**Current Maturity:** Production-ready for observability in local and Azure deployments. Enhanced metrics and alerting planned for scale.

---

**Related Documentation:**
- [Observability Strategy](../../docs/architecture/OBSERVABILITY_STRATEGY.md)
- [Azure Deployment Strategy](../../docs/deployment/AZURE_DEPLOYMENT_STRATEGY.md)
- [.NET Aspire Analysis](../../docs/deployment/ASPIRE_ANALYSIS.md)

*Last Updated: January 2025*
