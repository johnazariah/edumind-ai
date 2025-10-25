# ADR-080: Application Insights for Observability

**Status:** ✅ Accepted  
**Date:** October 2025  
**Context:** Production Observability and Monitoring

## Context

Production deployment required comprehensive observability for:

- Distributed tracing across microservices
- Performance monitoring (API latency, LLM calls)
- Error tracking and diagnostics
- Custom metrics (assessment completions, agent performance)
- User behavior analytics
- Availability monitoring

Local development used Aspire dashboard, but production needed a managed solution.

## Decision

Selected **Azure Application Insights** with **OpenTelemetry** integration for production observability.

## Rationale

1. **Azure Native**: Seamless integration with Container Apps
2. **OpenTelemetry**: Standard instrumentation (portable to other backends)
3. **Distributed Tracing**: Automatic correlation across services
4. **Performance Monitoring**: APM with dependency tracking
5. **Log Aggregation**: Centralized logging from all containers
6. **Kusto Queries**: Powerful KQL for log analysis
7. **Alerts & Dashboards**: Built-in alerting and visualization

## Consequences

### Positive

- **End-to-end visibility**: Trace requests from Blazor → API → LLM → Database
- **Performance insights**: Identify slow queries, LLM timeouts
- **Proactive alerts**: Notify on errors, performance degradation
- **Cost insights**: Track LLM API costs via custom metrics
- **Debugging**: Find root cause of production issues faster
- **Compliance**: Audit logs for FERPA/GDPR requirements

### Negative

- **Cost**: ~$2.30/GB ingested (~$30-100/month at scale)
- **Data retention**: 90 days default (archive for longer retention)
- **Learning curve**: KQL query language
- **Sampling**: High-volume traces sampled (adaptive sampling)
- **Latency**: Small overhead (~1-2ms per request)

### Risks Mitigated

- OpenTelemetry allows migration to other backends (Jaeger, Zipkin)
- Sampling configured to reduce costs while maintaining visibility
- PII filtering prevents logging sensitive student data
- Configured data retention policies

## Implementation

**OpenTelemetry Configuration** (src/EduMind.ServiceDefaults/Extensions.cs):

```csharp
public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
{
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("EduMind.Agents")
                .AddSource("EduMind.LLM")
                .AddSource("EduMind.Orchestration");
            
            // Export to Application Insights
            if (builder.Environment.IsProduction())
            {
                tracing.AddAzureMonitorTraceExporter(options =>
                {
                    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
                });
            }
            else
            {
                tracing.AddConsoleExporter();  // Local development
            }
        })
        .WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("EduMind.Agents")
                .AddMeter("EduMind.LLM");
            
            if (builder.Environment.IsProduction())
            {
                metrics.AddAzureMonitorMetricExporter(options =>
                {
                    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
                });
            }
        });
    
    return builder;
}
```

**Custom Instrumentation**:

```csharp
using var activity = ActivitySource.StartActivity("LLM.Generate");
activity?.SetTag("llm.provider", "azure-openai");
activity?.SetTag("llm.model", "gpt-4o");
activity?.SetTag("llm.prompt_tokens", promptTokens);

try
{
    var response = await _llmClient.GenerateAsync(prompt);
    activity?.SetTag("llm.completion_tokens", completionTokens);
    activity?.SetTag("llm.status", "success");
    return response;
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    throw;
}
```

**Custom Metrics**:

```csharp
public class AgentMetrics
{
    private static readonly Meter Meter = new("EduMind.Agents");
    
    private static readonly Counter<long> AssessmentsGenerated = 
        Meter.CreateCounter<long>("assessments.generated", "assessments");
    
    private static readonly Histogram<double> AgentDuration = 
        Meter.CreateHistogram<double>("agent.duration", "seconds");
    
    public static void RecordAssessmentGenerated(Subject subject)
    {
        AssessmentsGenerated.Add(1, new KeyValuePair<string, object>("subject", subject.ToString()));
    }
    
    public static void RecordAgentDuration(Subject subject, double durationSeconds)
    {
        AgentDuration.Record(durationSeconds, new KeyValuePair<string, object>("subject", subject.ToString()));
    }
}
```

## Kusto Query Examples

**Find slow API requests**:

```kusto
requests
| where timestamp > ago(1h)
| where duration > 1000  // > 1 second
| project timestamp, name, duration, resultCode
| order by duration desc
| take 50
```

**Track LLM performance**:

```kusto
traces
| where customDimensions.llm_provider == "azure-openai"
| summarize 
    avg_duration = avg(todouble(customDimensions.llm_duration)),
    count = count()
    by bin(timestamp, 5m), agent = tostring(customDimensions.agent)
| render timechart
```

**Error rate monitoring**:

```kusto
requests
| where timestamp > ago(24h)
| summarize 
    total = count(),
    errors = countif(success == false),
    error_rate = 100.0 * countif(success == false) / count()
    by bin(timestamp, 1h)
| render timechart
```

**Top expensive LLM calls**:

```kusto
customMetrics
| where name == "llm.cost"
| summarize total_cost = sum(value) by agent = tostring(customDimensions.agent)
| order by total_cost desc
```

## Dashboards

**System Health Dashboard**:

- Request rate (requests/sec)
- Error rate (%)
- P50/P95/P99 latency
- Dependency health (PostgreSQL, Redis, LLM)
- Container health (CPU, memory)

**Agent Performance Dashboard**:

- Assessments generated by subject
- Agent evaluation time (P50/P95)
- LLM call duration by provider
- Question generation success rate

**Cost Tracking Dashboard**:

- Azure OpenAI API costs by agent
- Database IOPS and storage costs
- Redis memory usage
- Container Apps compute costs

## Alerting Rules

**Critical Alerts** (PagerDuty):

- Error rate > 5% for 5 minutes
- P95 latency > 2 seconds for 10 minutes
- LLM timeout rate > 10% for 5 minutes
- Database connection failures

**Warning Alerts** (Email):

- Error rate > 1% for 15 minutes
- P95 latency > 1 second for 30 minutes
- Redis memory > 80%
- PostgreSQL CPU > 70%

## Sampling Strategy

**Adaptive Sampling** (reduces costs):

- Keep 100% of errors and exceptions
- Sample successful requests:
  - High traffic (>1000 req/min): 10% sampling
  - Medium traffic (100-1000 req/min): 50% sampling
  - Low traffic (<100 req/min): 100% sampling

**Configuration**:

```json
{
  "ApplicationInsights": {
    "EnableAdaptiveSampling": true,
    "MaxTelemetryItemsPerSecond": 10,
    "SamplingSettings": {
      "IsEnabled": true,
      "MaxTelemetryItemsPerSecond": 10
    }
  }
}
```

## PII Filtering

Prevent logging sensitive data:

```csharp
services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = configuration["ApplicationInsights:ConnectionString"];
    
    // Filter PII
    options.EnableAdaptiveSampling = true;
    options.EnableAuthenticationTrackingJavaScript = false;
});

services.AddSingleton<ITelemetryInitializer, PiiFilteringInitializer>();

public class PiiFilteringInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is RequestTelemetry request)
        {
            // Remove query strings that might contain PII
            request.Url = RemoveQueryString(request.Url);
        }
    }
}
```

## Cost Management

**Monthly costs** (moderate load, 10,000 active students):

- Ingestion: ~20 GB/month = $46
- Retention (90 days): Included
- Queries: Free (< 5 GB/month)
- **Total**: ~$50/month

**Cost optimization**:

- Use sampling (reduce ingestion by 80%)
- Archive old logs to Blob Storage ($0.01/GB/month)
- Query less frequently (cache results)
- Filter unnecessary telemetry

## Alternative Considered: Self-Hosted (Grafana + Prometheus + Jaeger)

**Rejected because:**

- Requires managing infrastructure (VM, storage)
- More complex setup and maintenance
- No managed service guarantees
- Limited integration with Azure services
- Application Insights more cost-effective at small-medium scale

## Related Decisions

- ADR-007: .NET Aspire for Local Orchestration (Aspire dashboard for local)
- ADR-081: Structured Logging with Serilog
- ADR-020: Azure Container Apps (Application Insights integration)

## References

- `src/EduMind.ServiceDefaults/Extensions.cs` - OpenTelemetry configuration
- `infra/resources.bicep` - Application Insights resource
- Commit: `c80b9f1` - "feat: Add observability infrastructure"
- docs/architecture/OBSERVABILITY_STRATEGY.md
