# Story 011: Implement Comprehensive Monitoring & Alerting

**Priority:** P2 - Enhancement  
**Status:** Ready for Implementation  
**Effort:** Medium (1 week)  
**Dependencies:** None


**GitHub Issue:** https://github.com/johnazariah/edumind-ai/issues/16

---

## Problem Statement

No production monitoring, alerting, or observability. Cannot detect or respond to issues proactively.

**Current State:**

- Basic console logging only
- No performance metrics
- No health dashboards
- No alerting on errors or degraded performance
- Blind to production issues until users report them

**Business Impact:** Service outages go undetected, poor user experience, cannot meet SLA commitments.

---

## Goals & Success Criteria

1. **Application Insights integration** for telemetry, logs, metrics, traces
2. **Custom metrics**: LLM latency, API response times, error rates, cache hit rates
3. **Alerts**: High error rate (>5%), slow API responses (>3s), service unavailability
4. **Dashboards**: System health, performance trends, user activity
5. **Log Analytics** queries for troubleshooting

**Success Criteria:**

- [ ] All services emit telemetry to Application Insights
- [ ] Custom metrics tracked (LLM latency, API times, errors)
- [ ] 5 critical alerts configured (email/SMS)
- [ ] Operations dashboard shows real-time health
- [ ] Mean time to detection (MTTD) <5 minutes

---

## Technical Approach

### Azure Application Insights Setup

**Services to Monitor:**

- AcademicAssessment.Web (API)
- AcademicAssessment.Dashboard (Blazor)
- AcademicAssessment.StudentApp (Blazor)
- AcademicAssessment.Agents (LLM orchestration)

### Custom Metrics

```csharp
// LLM Performance
telemetryClient.TrackMetric("LLM.InferenceLatency", duration.TotalSeconds);
telemetryClient.TrackMetric("LLM.TokensGenerated", tokenCount);

// API Performance
telemetryClient.TrackMetric("API.ResponseTime", duration.TotalMilliseconds);
telemetryClient.TrackMetric("API.ErrorRate", errorCount / totalRequests);

// Cache Performance
telemetryClient.TrackMetric("Cache.HitRate", hits / (hits + misses));
```

### Alert Rules

| Alert | Condition | Action |
|-------|-----------|--------|
| High Error Rate | >5% in 5 minutes | Email + SMS |
| Slow API | P95 > 3s for 10 minutes | Email |
| Service Down | Availability < 99% | SMS |
| LLM Timeout | >10s for 5 calls | Email |
| Database Connection | >50 active connections | Email |

---

## Task Decomposition

### Task 1: Add Application Insights SDK

- **Files to Modify:**
  - All `.csproj` files
  - `appsettings.json` (add instrumentation key)
  - `Program.cs` (configure telemetry)
- **Package:** `Microsoft.ApplicationInsights.AspNetCore`
- **Acceptance:** Telemetry flows to Application Insights

### Task 2: Implement Custom Metrics

- **Files to Create:**
  - `src/AcademicAssessment.Infrastructure/Telemetry/MetricsService.cs`
- **Track:** LLM latency, API response times, cache performance
- **Acceptance:** Custom metrics visible in Azure Portal

### Task 3: Configure Alert Rules

- **Location:** Azure Portal → Application Insights → Alerts
- **Create:** 5 critical alert rules (see table above)
- **Acceptance:** Alerts fire when conditions met (test with load)

### Task 4: Create Operations Dashboard

- **Tool:** Azure Dashboard or Application Insights Workbook
- **Widgets:** Error rate, API latency (P50/P95), LLM performance, active users
- **Acceptance:** Dashboard shows real-time system health

### Task 5: Add Health Check Endpoints

- **Files to Create:**
  - `src/AcademicAssessment.Web/HealthChecks/DatabaseHealthCheck.cs`
  - `src/AcademicAssessment.Web/HealthChecks/RedisHealthCheck.cs`
  - `src/AcademicAssessment.Web/HealthChecks/LlmHealthCheck.cs`
- **Endpoint:** `GET /health`
- **Acceptance:** Returns 200 if healthy, 503 if degraded

### Task 6: Document Runbooks

- **Files to Create:**
  - `docs/operations/MONITORING_RUNBOOK.md`
- **Content:** How to respond to each alert, troubleshooting steps
- **Acceptance:** On-call engineers can follow runbook

---

## Acceptance Criteria

- [ ] Application Insights integrated
- [ ] Custom metrics tracked
- [ ] 5 alert rules configured
- [ ] Operations dashboard deployed
- [ ] Health check endpoints functional
- [ ] MTTD <5 minutes (tested with simulated outage)

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot
