# System Health API Reference

**Version:** 1.0  
**Base Path:** `/health`, `/alive`  
**Framework:** .NET Aspire Service Defaults + Custom Health Checks  
**Source:** `src/EduMind.ServiceDefaults/Extensions.cs`, `src/AcademicAssessment.Web/Program.cs`

## Table of Contents

- [Overview](#overview)
- [Authentication](#authentication)
- [Endpoints](#endpoints)
  - [1. Comprehensive Health Check](#1-comprehensive-health-check)
  - [2. Liveness Probe](#2-liveness-probe)
- [Health Check Components](#health-check-components)
- [Health Status Levels](#health-status-levels)
- [Response Formats](#response-formats)
- [Usage Examples](#usage-examples)
- [Implementation Status](#implementation-status)

---

## Overview

The System Health API provides health monitoring endpoints for application liveness and readiness checks. These endpoints are used by Kubernetes/container orchestrators for automated health monitoring and traffic management.

**Key Features:**

- **Liveness probes**: Verifies application process is running and responsive
- **Readiness probes**: Checks all dependencies (database, cache) are operational
- **Detailed health reports**: Includes status, duration, and error information for each check
- **Aspire integration**: Leverages .NET Aspire Service Defaults for standardized health checks
- **Development-only exposure**: Health endpoints only enabled in development environment (security best practice)

**Content Type:** `application/json`

---

## Authentication

**No authentication required** for health check endpoints.

Health endpoints are intentionally unauthenticated to allow:

- Kubernetes liveness/readiness probes to function without secrets
- Load balancers to perform health checks
- Monitoring systems to verify application availability

⚠️ **Security Note**: Health endpoints are **only exposed in development environments**. In production, these endpoints are disabled to prevent information disclosure. See `.NET Aspire` documentation for production health check strategies.

---

## Endpoints

### 1. Comprehensive Health Check

**Endpoint:** `GET /health`

**Description:** Returns comprehensive health status including all registered health checks (database, cache, application).

**Availability:** Development environment only

**Response:** `200 OK` (Healthy), `503 Service Unavailable` (Unhealthy/Degraded)

```json
{
  "status": "Healthy",
  "timestamp": "2025-01-20T16:45:32.1234567Z",
  "duration": "00:00:00.0234567",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0001234",
      "exception": null,
      "data": {}
    },
    {
      "name": "postgresql",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0123456",
      "exception": null,
      "data": {}
    },
    {
      "name": "redis",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0056789",
      "exception": null,
      "data": {}
    }
  ]
}
```

**Response Fields:**

- `status` (string) - Overall health status: "Healthy", "Degraded", or "Unhealthy"
- `timestamp` (DateTimeOffset) - ISO 8601 timestamp when check was performed
- `duration` (TimeSpan) - Total duration for all health checks
- `checks` (array) - Individual health check results
  - `name` (string) - Health check identifier ("self", "postgresql", "redis")
  - `status` (string) - Check status: "Healthy", "Degraded", or "Unhealthy"
  - `description` (string, nullable) - Human-readable description or error message
  - `duration` (TimeSpan) - Duration of this specific check
  - `exception` (string, nullable) - Exception message if check failed
  - `data` (object) - Additional check-specific metadata

**Health Check Logic:**

- Returns **200 OK** if overall status is "Healthy"
- Returns **503 Service Unavailable** if overall status is "Degraded" or "Unhealthy"
- Overall status determined by worst individual check status:
  - All Healthy → Overall Healthy
  - Any Degraded → Overall Degraded
  - Any Unhealthy → Overall Unhealthy

**Registered Health Checks:**

1. **self**: Application liveness (always healthy if process is running)
2. **postgresql**: Database connectivity check
   - Tests connection to PostgreSQL database
   - Failure status: **Unhealthy** (critical dependency)
   - Tags: `db`, `postgresql`, `ready`
3. **redis**: Cache connectivity check
   - Tests connection to Redis cache
   - Failure status: **Degraded** (non-critical dependency, application can function without cache)
   - Tags: `cache`, `redis`

**Use Cases:**

- Kubernetes readiness probe (wait for all dependencies before accepting traffic)
- Monitoring dashboard health status
- Deployment validation (verify all services are operational)
- Troubleshooting connectivity issues

---

### 2. Liveness Probe

**Endpoint:** `GET /alive`

**Description:** Returns minimal liveness status to verify application process is running and responsive. Does not check external dependencies.

**Availability:** Development environment only

**Response:** `200 OK`

```json
{
  "status": "Healthy",
  "timestamp": "2025-01-20T16:45:32.1234567Z"
}
```

**Response Fields:**

- `status` (string) - Always "Healthy" if application responds
- `timestamp` (DateTimeOffset) - ISO 8601 timestamp when check was performed

**Health Check Logic:**

- Only checks tagged with `"live"` are evaluated (currently only "self" check)
- **Always returns 200 OK** if application process is running
- Does **not** check database, cache, or other external dependencies
- Extremely fast (<1ms) since no I/O operations performed

**Use Cases:**

- Kubernetes liveness probe (restart pod if unresponsive)
- Basic uptime monitoring
- Verify application process hasn't crashed or deadlocked
- High-frequency health checks (every 5-10 seconds) without external dependency overhead

**Liveness vs Readiness:**

- **Liveness** (`/alive`): "Is the app running?" → If no, restart it
- **Readiness** (`/health`): "Is the app ready to serve traffic?" → If no, stop sending traffic but don't restart

---

## Health Check Components

### 1. Self Check

**Name:** `self`  
**Tags:** `live`  
**Description:** Verifies application process is responsive

**Implementation:**

```csharp
.AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
```

**Always returns:** `Healthy`

**Purpose:** Basic liveness check without external dependencies

---

### 2. PostgreSQL Check

**Name:** `postgresql`  
**Tags:** `db`, `postgresql`, `ready`  
**Description:** Tests PostgreSQL database connectivity

**Implementation:**

```csharp
.AddNpgSql(
    connectionString,
    name: "postgresql",
    failureStatus: HealthStatus.Unhealthy,
    tags: new[] { "db", "postgresql", "ready" })
```

**Connection String:** Configured via environment variable or appsettings.json

- Development: `Host=localhost;Database=edumind_dev;Username=edumind_user;Password=edumind_dev_password`
- Production: Retrieved from Azure Key Vault or environment variables

**Failure Status:** `Unhealthy` (critical dependency)

**Checks:**

- Database server reachable
- Credentials valid
- Database exists and accessible
- Connection pool not exhausted

**Common Failures:**

- Network connectivity issues
- Invalid credentials
- Database server down
- Connection timeout
- Too many connections

---

### 3. Redis Check

**Name:** `redis`  
**Tags:** `cache`, `redis`  
**Description:** Tests Redis cache connectivity

**Implementation:**

```csharp
.AddRedis(
    redisConnection,
    name: "redis",
    failureStatus: HealthStatus.Degraded,
    tags: new[] { "cache", "redis" })
```

**Connection String:** Configured via environment variable or appsettings.json

- Development: `localhost:6379`
- Production: Azure Cache for Redis connection string

**Failure Status:** `Degraded` (non-critical dependency)

**Design Decision:** Redis failures mark system as "Degraded" rather than "Unhealthy" because:

- Application can function without cache (performance impact only)
- Assessment sessions are persisted in database (Redis is secondary cache)
- Graceful degradation preferred over complete failure

**Checks:**

- Redis server reachable
- Authentication successful (if configured)
- PING command responds
- Connection not stale

**Common Failures:**

- Network connectivity issues
- Redis server restart
- Memory full (Redis OOM)
- Authentication failure
- Connection timeout

---

## Health Status Levels

### Healthy

**HTTP Status:** 200 OK  
**Meaning:** All systems operational, ready to accept traffic  
**Action:** None required

**Conditions:**

- Application process running
- PostgreSQL database accessible
- Redis cache accessible (or not configured)
- All critical dependencies operational

---

### Degraded

**HTTP Status:** 503 Service Unavailable  
**Meaning:** Application operational but with reduced functionality  
**Action:** Investigate non-critical dependency failures

**Conditions:**

- Application process running
- PostgreSQL database accessible
- Redis cache **not accessible** (performance impact, but functional)

**Impact:**

- Assessment sessions function normally (database-backed)
- Analytics queries slower (no caching)
- Agent orchestration performance reduced
- Kubernetes may route traffic away from degraded pods

---

### Unhealthy

**HTTP Status:** 503 Service Unavailable  
**Meaning:** Critical failure, unable to serve requests  
**Action:** Immediate investigation required

**Conditions:**

- PostgreSQL database **not accessible** (critical failure)
- Application unable to process requests
- Data persistence unavailable

**Impact:**

- Cannot save assessment sessions
- Cannot retrieve student data
- Cannot authenticate users (RBAC stored in database)
- Kubernetes will restart pod if liveness check fails

---

## Response Formats

### Healthy Response

```json
{
  "status": "Healthy",
  "timestamp": "2025-01-20T16:45:32.1234567Z",
  "duration": "00:00:00.0234567",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0001234",
      "exception": null,
      "data": {}
    },
    {
      "name": "postgresql",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0123456",
      "exception": null,
      "data": {}
    },
    {
      "name": "redis",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0056789",
      "exception": null,
      "data": {}
    }
  ]
}
```

---

### Degraded Response (Redis Failure)

```json
{
  "status": "Degraded",
  "timestamp": "2025-01-20T16:48:15.7654321Z",
  "duration": "00:00:05.1234567",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0001234",
      "exception": null,
      "data": {}
    },
    {
      "name": "postgresql",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0123456",
      "exception": null,
      "data": {}
    },
    {
      "name": "redis",
      "status": "Degraded",
      "description": "Connection timeout",
      "duration": "00:00:05.0109877",
      "exception": "RedisConnectionException: It was not possible to connect to the redis server(s). ConnectTimeout",
      "data": {}
    }
  ]
}
```

**HTTP Status:** 503 Service Unavailable

---

### Unhealthy Response (Database Failure)

```json
{
  "status": "Unhealthy",
  "timestamp": "2025-01-20T16:50:42.9876543Z",
  "duration": "00:00:30.5678901",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0001234",
      "exception": null,
      "data": {}
    },
    {
      "name": "postgresql",
      "status": "Unhealthy",
      "description": "Failed to connect to database",
      "duration": "00:00:30.5567890",
      "exception": "NpgsqlException: Connection refused. Is the server running on host 'localhost' and accepting TCP/IP connections on port 5432?",
      "data": {}
    },
    {
      "name": "redis",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0109877",
      "exception": null,
      "data": {}
    }
  ]
}
```

**HTTP Status:** 503 Service Unavailable

---

## Usage Examples

### Example 1: Kubernetes Liveness Probe Configuration

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: edumind-web
spec:
  containers:
  - name: web-api
    image: edumind-web:latest
    livenessProbe:
      httpGet:
        path: /alive
        port: 8080
      initialDelaySeconds: 10
      periodSeconds: 10
      timeoutSeconds: 2
      failureThreshold: 3
```

**Behavior:**

- Checks `/alive` every 10 seconds
- Restarts container if 3 consecutive failures
- Fast check (~1ms) suitable for frequent polling

---

### Example 2: Kubernetes Readiness Probe Configuration

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: edumind-web
spec:
  containers:
  - name: web-api
    image: edumind-web:latest
    readinessProbe:
      httpGet:
        path: /health
        port: 8080
      initialDelaySeconds: 30
      periodSeconds: 30
      timeoutSeconds: 5
      failureThreshold: 2
```

**Behavior:**

- Checks `/health` every 30 seconds (includes DB/Redis checks)
- Removes pod from service if 2 consecutive failures
- Longer timeout (5s) to allow for DB connection time

---

### Example 3: Load Balancer Health Check

**Request:**

```http
GET /health HTTP/1.1
Host: edumind-web.azurecontainerapps.io
Accept: application/json
```

**Response:** `200 OK`

```json
{
  "status": "Healthy",
  "timestamp": "2025-01-20T16:45:32.1234567Z",
  "duration": "00:00:00.0234567",
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0001234",
      "exception": null,
      "data": {}
    },
    {
      "name": "postgresql",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0123456",
      "exception": null,
      "data": {}
    },
    {
      "name": "redis",
      "status": "Healthy",
      "description": null,
      "duration": "00:00:00.0056789",
      "exception": null,
      "data": {}
    }
  ]
}
```

---

### Example 4: Monitoring Dashboard Query

**Request:**

```bash
curl -X GET "http://localhost:8080/health" \
  -H "Accept: application/json" \
  | jq '.checks[] | select(.status != "Healthy")'
```

**Response:** (Only unhealthy/degraded checks)

```json
{
  "name": "redis",
  "status": "Degraded",
  "description": "Connection timeout",
  "duration": "00:00:05.0109877",
  "exception": "RedisConnectionException: It was not possible to connect to the redis server(s). ConnectTimeout",
  "data": {}
}
```

---

### Example 5: Quick Liveness Check

**Request:**

```bash
curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/alive
```

**Response:**

```
200
```

**Use Case:** Simple uptime monitoring script checking if application process is responsive

---

## Implementation Status

### ✅ Fully Implemented

1. **Aspire Service Defaults**
   - `AddServiceDefaults()` registers default health checks
   - `MapDefaultEndpoints()` exposes health endpoints in development
   - `AddDefaultHealthChecks()` adds "self" liveness check

2. **Health Check Endpoints**
   - `/health` - Comprehensive readiness check (all dependencies)
   - `/alive` - Minimal liveness check (tagged "live" only)
   - Development-only exposure via `app.Environment.IsDevelopment()`

3. **Dependency Health Checks**
   - PostgreSQL connectivity (`AddNpgSql`, status: Unhealthy on failure)
   - Redis connectivity (`AddRedis`, status: Degraded on failure)
   - Self check (always healthy)

4. **Response Formats**
   - JSON response with status, timestamp, duration, checks
   - HTTP status codes: 200 (Healthy), 503 (Degraded/Unhealthy)
   - Detailed check information (name, status, duration, exception, data)

5. **Tag-Based Filtering**
   - `/alive` only checks "live" tagged checks
   - `/health` checks all registered checks
   - Tags: `live`, `db`, `postgresql`, `ready`, `cache`, `redis`

### 🚧 Commented Out (Security)

**Custom Health Check Mappings in Program.cs (lines 573-650):**

- Detailed health check endpoints with custom response writers
- `/health/ready` - Readiness probe with "ready" tag filter
- `/health/live` - Liveness probe with no checks (always healthy)
- Rich JSON response formatting with check details

**Reason for Disabling:**

- Aspire's `MapDefaultEndpoints()` already provides `/health` and `/alive` endpoints
- Custom mappings caused "ambiguous match" routing errors
- Aspire approach preferred for consistency across microservices

**Future Re-enablement:**

- Uncomment and adjust routes if custom response format needed
- Consider production security implications (information disclosure)

### ⚠️ Security Considerations

**Development-Only Exposure:**

- Health endpoints **only available in development environment**
- Production deployment requires explicit configuration
- See [Aspire Health Check Security](https://aka.ms/dotnet/aspire/healthchecks) for production strategies

**Production Options:**

1. Enable health endpoints behind internal network/VNet
2. Use Azure Container Apps built-in health probes
3. Implement authenticated health endpoints for monitoring
4. Use Azure Application Insights for health monitoring

**Information Disclosure Risks:**

- Health responses reveal infrastructure details (PostgreSQL, Redis)
- Exception messages may contain sensitive connection details
- Timing information could be used for reconnaissance
- Status information reveals system architecture

### 📝 Future Enhancements

1. **Application-Specific Checks**
   - Agent service health (OLLAMA connectivity, response time)
   - Database connection pool utilization
   - Memory usage thresholds
   - Active user session count

2. **Performance Metrics**
   - Average response time per health check
   - Historical health status trends
   - Failure rate tracking
   - Dependency latency percentiles

3. **Alerting Integration**
   - PagerDuty/OpsGenie integration for health failures
   - Slack/Teams notifications for degraded status
   - Email alerts for prolonged unhealthy state

4. **Custom Health Check Writers**
   - Prometheus metrics format (`/metrics`)
   - HTML dashboard format for human viewing
   - CSV export for historical analysis

5. **Advanced Dependency Checks**
   - OLLAMA LLM service health (semantic evaluation capability)
   - Azure AD B2C authentication service availability
   - SignalR hub connectivity for real-time features
   - Multi-region database replica health

---

## Related Documentation

- **Observability**: `.github/specification/08-observability.md` (OpenTelemetry, logging, metrics)
- **Data Storage**: `.github/specification/05-data-storage.md` (PostgreSQL, Redis architecture)
- **System Architecture**: `.github/specification/02-system-architecture.md` (deployment model)
- **Security**: `.github/specification/07-security-privacy.md` (authentication, authorization)

---

**Document Status:** Complete  
**Last Updated:** 2025-01-20  
**Version:** 1.0  
**Contributors:** GitHub Copilot
