# Observability and Monitoring Strategy

**Created:** October 15, 2025  
**Status:** Implementing  
**Version:** 1.0

## Overview

Comprehensive observability strategy for EduMind.AI using OpenTelemetry, Prometheus, and Serilog to track application health, performance metrics, and identify bottlenecks in LLM calls, agent orchestration, and database operations.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     EduMind.AI Web API                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ OpenTelemetry│  │  Prometheus  │  │   Serilog    │     │
│  │   Tracing    │  │   Metrics    │  │   Logging    │     │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘     │
│         │                  │                  │              │
└─────────┼──────────────────┼──────────────────┼─────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
    ┌──────────┐       ┌──────────┐      ┌──────────┐
    │  Jaeger  │       │ Grafana  │      │ Log Files│
    │  (Future)│       │Dashboard │      │ Console  │
    └──────────┘       └──────────┘      └──────────┘
```

## Components

### 1. OpenTelemetry - Distributed Tracing

**Purpose:** Track request flow through the system, identify slow operations

**What We Track:**

- HTTP requests (ASP.NET Core)
- HTTP client calls (OLLAMA, Azure OpenAI)
- Database queries (PostgreSQL via Npgsql)
- Custom spans for:
  - LLM evaluations (per agent)
  - Agent orchestration decisions
  - Student progress calculations
  - Cache operations

**Configuration:**

```csharp
services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddSource("EduMind.Agents")
        .AddSource("EduMind.LLM")
        .AddConsoleExporter());
```

**Key Spans:**

- `agent.evaluate` - Individual agent evaluation
- `orchestrator.assign_task` - Task routing decision
- `llm.call` - LLM API call (OLLAMA/Azure)
- `analytics.calculate` - Performance calculation

### 2. Prometheus - Metrics Collection

**Purpose:** Track quantitative metrics over time, identify trends

**Metrics We Track:**

#### HTTP Metrics (Built-in)

- `http_server_request_duration_seconds` - Request latency
- `http_server_active_requests` - Current active requests
- `http_server_request_total` - Total request count by endpoint

#### Custom Application Metrics

**LLM Performance:**

```
# Histogram: LLM call duration by provider
llm_call_duration_seconds{provider="ollama|azure|stub", agent="math|physics|..."}

# Counter: Total LLM calls
llm_calls_total{provider, agent, status="success|error"}

# Gauge: Current queue length
llm_queue_length{provider}
```

**Agent Orchestration:**

```
# Histogram: Agent evaluation time
agent_evaluation_duration_seconds{agent, subject}

# Counter: Tasks assigned
agent_tasks_assigned_total{agent, priority="high|medium|low"}

# Gauge: Active agents
agent_active_count{agent}
```

**Database Performance:**

```
# Histogram: Query duration
db_query_duration_seconds{operation="select|insert|update"}

# Counter: Query count
db_queries_total{operation, status="success|error"}
```

**Cache Performance:**

```
# Counter: Cache operations
cache_operations_total{operation="hit|miss|set", cache="redis"}

# Gauge: Cache size
cache_size_bytes{cache="redis"}
```

**Student Analytics:**

```
# Histogram: Analytics calculation time
analytics_calculation_duration_seconds{type="performance|progress|weak_areas"}

# Counter: Analytics requests
analytics_requests_total{student_id, type}
```

### 3. Serilog - Structured Logging

**Purpose:** Contextual logging with structured data for debugging and auditing

**Log Levels:**

- **Verbose:** Detailed tracing (disabled in production)
- **Debug:** Development diagnostics
- **Information:** General flow (API calls, agent decisions)
- **Warning:** Unexpected but recoverable (slow LLM, cache miss)
- **Error:** Failures (LLM timeout, DB connection)
- **Fatal:** Application crash

**Enrichers:**

- `MachineName` - Server identification
- `EnvironmentName` - Dev/Staging/Production
- `ThreadId` - Concurrency tracking
- `RequestId` - Correlation across logs

**Sinks:**

- Console (Development)
- File (Always, with rolling)
- Future: Application Insights, Seq, Elasticsearch

**Structured Log Examples:**

```csharp
// LLM Call
Log.Information("LLM evaluation started for {Agent} with {Provider}", 
    agentName, providerName);

// Agent Decision
Log.Information("Task routed to {Agent} for {Student} with priority {Priority}", 
    agentName, studentId, priority);

// Performance Warning
Log.Warning("Slow LLM response: {Duration}ms from {Provider} for {Agent}", 
    duration, provider, agent);

// Error
Log.Error(ex, "LLM call failed for {Agent} after {Retries} retries", 
    agentName, retryCount);
```

## Metrics Dashboard (Grafana)

### Dashboard Panels

#### 1. System Health Overview

- HTTP request rate (requests/sec)
- Error rate (%)
- Average response time
- Active connections

#### 2. LLM Performance

- LLM call duration (p50, p95, p99)
- LLM calls per minute by provider
- OLLAMA vs Azure OpenAI comparison
- LLM error rate

#### 3. Agent Orchestration

- Task assignment rate
- Agent utilization (%)
- Task queue depth
- Average agent evaluation time

#### 4. Database Performance

- Query duration (p95)
- Queries per second
- Connection pool utilization
- Slow query count (>1s)

#### 5. Cache Performance

- Cache hit rate (%)
- Redis operations/sec
- Cache memory usage
- Cache eviction rate

#### 6. Student Analytics

- Analytics requests/minute
- Calculation time (p95)
- Concurrent analytics operations

## Implementation Plan

### Phase 1: Core Instrumentation ✅ (Current)

- [x] Add OpenTelemetry packages
- [x] Add Prometheus exporter
- [x] Add Serilog packages
- [ ] Configure Program.cs with basic setup
- [ ] Add custom activity sources
- [ ] Expose `/metrics` endpoint

### Phase 2: Custom Metrics

- [ ] Implement LLM metrics in ILLMService implementations
- [ ] Add agent orchestration metrics
- [ ] Add database query metrics
- [ ] Add cache performance metrics

### Phase 3: Enhanced Logging

- [ ] Add structured logging to all LLM calls
- [ ] Log agent decision-making
- [ ] Log performance warnings (>1s operations)
- [ ] Add correlation IDs across services

### Phase 4: Visualization

- [ ] Set up Grafana container
- [ ] Create dashboard templates
- [ ] Configure alerting rules
- [ ] Document dashboard usage

### Phase 5: Production Integration

- [ ] Add Application Insights exporter (Azure)
- [ ] Configure log aggregation
- [ ] Set up alerting (email/Slack)
- [ ] Create runbook for common issues

## Configuration

### appsettings.json

```json
{
  "Observability": {
    "ServiceName": "EduMind.AI",
    "ServiceVersion": "1.0.0",
    "EnableTracing": true,
    "EnableMetrics": true,
    "Metrics": {
      "PrometheusEndpoint": "/metrics"
    },
    "Tracing": {
      "SamplingRatio": 1.0,
      "ExportToConsole": true
    }
  },
  
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
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
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/edumind-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "fileSizeLimitBytes": 10485760,
          "rollOnFileSizeLimit": true,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] [{RequestId}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId", "WithEnvironmentName" ]
  }
}
```

### Environment Variables

```bash
# Development
ASPNETCORE_ENVIRONMENT=Development
OBSERVABILITY__ENABLETRACING=true
OBSERVABILITY__ENABLEMETRICS=true

# Production
ASPNETCORE_ENVIRONMENT=Production
OBSERVABILITY__TRACING__SAMPLINGRATIO=0.1  # Sample 10% of requests
OBSERVABILITY__TRACING__EXPORTTOCONSOLE=false
APPLICATIONINSIGHTS__CONNECTIONSTRING=<your-connection-string>
```

## Alerting Rules

### Critical Alerts

**High Error Rate:**

```
rate(http_server_request_total{status=~"5.."}[5m]) > 0.05
```

Action: Page on-call engineer

**LLM Timeout:**

```
llm_calls_total{status="timeout"}[5m] > 10
```

Action: Check OLLAMA/Azure OpenAI status

**Database Connection Pool Exhausted:**

```
db_connection_pool_available == 0
```

Action: Restart application or scale up

### Warning Alerts

**Slow LLM Response:**

```
histogram_quantile(0.95, llm_call_duration_seconds) > 60
```

Action: Investigate OLLAMA performance

**High Cache Miss Rate:**

```
rate(cache_operations_total{operation="miss"}[5m]) > 0.5
```

Action: Review caching strategy

**Slow Analytics:**

```
histogram_quantile(0.95, analytics_calculation_duration_seconds) > 5
```

Action: Optimize queries or add indexes

## Key Performance Indicators (KPIs)

| Metric | Target | Warning | Critical |
|--------|--------|---------|----------|
| API Response Time (p95) | < 500ms | > 1s | > 3s |
| LLM Call Time (p95) | < 30s | > 60s | > 120s |
| Error Rate | < 0.1% | > 1% | > 5% |
| Cache Hit Rate | > 80% | < 50% | < 30% |
| Database Query Time (p95) | < 100ms | > 500ms | > 1s |
| Agent Task Assignment | < 1s | > 3s | > 10s |

## Testing Observability

### Verify Metrics Endpoint

```bash
curl http://localhost:5103/metrics

# Should see output like:
# # HELP http_server_request_duration_seconds HTTP request duration
# # TYPE http_server_request_duration_seconds histogram
# http_server_request_duration_seconds_bucket{le="0.1"} 42
```

### Verify Structured Logs

```bash
# Check logs directory
tail -f logs/edumind-$(date +%Y%m%d).log

# Should see structured JSON-like output
```

### Test Custom Metrics

```bash
# Make API call
curl http://localhost:5103/api/v1/students/{id}/analytics/performance-summary

# Check metrics updated
curl http://localhost:5103/metrics | grep llm_calls_total
```

## Runbook: Common Issues

### Issue: High LLM Latency

**Symptoms:**

- `llm_call_duration_seconds` p95 > 60s
- Slow API responses
- User complaints

**Investigation:**

1. Check provider:

   ```bash
   curl http://localhost:11434/api/tags  # OLLAMA
   ```

2. Check metrics by provider:

   ```
   llm_call_duration_seconds{provider="ollama"}
   ```

3. Check logs for timeout errors:

   ```bash
   grep "LLM timeout" logs/edumind-*.log
   ```

**Resolution:**

- Switch to StubLLMService temporarily
- Restart OLLAMA service
- Scale Azure OpenAI quota
- Add caching for common evaluations

### Issue: Database Slow Queries

**Symptoms:**

- `db_query_duration_seconds` p95 > 500ms
- High CPU on PostgreSQL

**Investigation:**

1. Check slow query logs
2. Verify indexes exist
3. Check connection pool

**Resolution:**

- Add missing indexes
- Optimize N+1 queries
- Increase connection pool size

### Issue: Cache Ineffective

**Symptoms:**

- `cache_operations_total{operation="miss"}` high
- Repeated LLM calls

**Investigation:**

1. Check Redis connection
2. Verify cache keys
3. Check TTL settings

**Resolution:**

- Increase cache TTL
- Warm cache on startup
- Review cache key strategy

## Future Enhancements

### Short Term

- [ ] Add Application Insights integration
- [ ] Create Grafana dashboards
- [ ] Set up PagerDuty alerts
- [ ] Add distributed tracing across agent calls

### Medium Term

- [ ] Add custom business metrics
- [ ] Implement SLO tracking
- [ ] Add user journey tracing
- [ ] Create automated performance reports

### Long Term

- [ ] Machine learning on metrics for anomaly detection
- [ ] Automated performance optimization recommendations
- [ ] Cost analysis dashboard (LLM usage by student/course)
- [ ] Predictive scaling based on usage patterns

## Related Documentation

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [Prometheus ASP.NET Core Exporter](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Exporter.Prometheus.AspNetCore)
- [Serilog Documentation](https://serilog.net/)
- [Grafana Dashboards](https://grafana.com/grafana/dashboards/)

---

**Last Updated:** October 15, 2025  
**Review Date:** November 15, 2025  
**Maintained By:** DevOps Team
