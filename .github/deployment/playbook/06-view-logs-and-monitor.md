# Playbook: View Logs and Monitor

**Scenario:** Need to view application logs, diagnose issues, or monitor performance  
**Time Required:** 5-15 minutes  
**Difficulty:** Beginner to Intermediate

---

## Prerequisites

- [ ] Aspire running (for local)
- [ ] Azure CLI authenticated (for Azure)
- [ ] Application Insights configured (for Azure)

---

## Local Development Logs

### Aspire Dashboard (Recommended)

**Step 1: Open Aspire Dashboard**

When you run Aspire, it automatically opens the dashboard. URL is shown in terminal:

```bash
dotnet run --project src/EduMind.AppHost/EduMind.AppHost.csproj

# Output includes:
# Dashboard running at: http://localhost:18888
```

**Step 2: Navigate to Logs**

1. Open dashboard URL in browser
2. Click on service name (e.g., "webapi")
3. Select "Logs" tab

**Features:**

- **Real-time streaming** - Logs update automatically
- **Filtering** - Search by keyword
- **Log levels** - Filter by Information, Warning, Error, etc.
- **Structured logging** - JSON-formatted logs with properties
- **Color coding** - Different colors for different services

**Step 3: Filter Logs**

- **By keyword:** Type search term in filter box
- **By log level:** Select from dropdown (All, Trace, Debug, Information, Warning, Error, Critical)
- **By service:** Click on specific service to see only its logs

### Console Logs (Alternative)

If you prefer terminal logs:

```bash
# Run Aspire with verbose logging
dotnet run --project src/EduMind.AppHost/EduMind.AppHost.csproj --verbosity detailed
```

**Output includes:**

- Color-coded logs per service
- Timestamps
- Log levels
- Messages

**Less searchable than dashboard, but useful for quick checks.**

### Docker Container Logs

For PostgreSQL, Redis, or other containerized services:

```bash
# List containers
docker ps

# View PostgreSQL logs
docker logs $(docker ps --filter "name=postgres" --format "{{.ID}}") --tail 100 --follow

# View Redis logs
docker logs $(docker ps --filter "name=redis" --format "{{.ID}}") --tail 100 --follow

# View all logs from a specific container
docker logs <container-id> --tail 100 --follow
```

### Application Logs (File-Based)

Not typically used in EduMind.AI, but if file logging is configured:

```bash
# Check appsettings for file paths
cat src/AcademicAssessment.Web/appsettings.Development.json | jq '.Logging'

# Example file log location
tail -f /var/log/edumind/application.log
```

---

## Azure Container Apps Logs

### Azure CLI (Quick Access)

**View Recent Logs:**

```bash
# Web API logs (last 100 lines)
az containerapp logs show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --tail 100

# Student App logs
az containerapp logs show \
  --name ca-studentapp-dev \
  --resource-group rg-dev \
  --tail 100

# Dashboard logs
az containerapp logs show \
  --name ca-dashboard-dev \
  --resource-group rg-dev \
  --tail 100
```

**Follow Logs in Real-Time:**

```bash
# Stream logs (like tail -f)
az containerapp logs show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --follow

# Press Ctrl+C to stop streaming
```

**Filter by Time:**

```bash
# Logs from last 30 minutes
az containerapp logs show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --tail 1000 | \
  grep "$(date -u -d '30 minutes ago' '+%Y-%m-%dT%H:%M')"
```

**Specific Revision Logs:**

```bash
# List revisions
az containerapp revision list \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --output table

# Get logs from specific revision
az containerapp logs show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --revision ca-webapi-dev--abc1234
```

### Azure Portal (Interactive)

**Step 1: Navigate to Container App**

1. Open Azure Portal: https://portal.azure.com
2. Go to **Resource Groups** → `rg-dev`
3. Click on Container App (e.g., `ca-webapi-dev`)

**Step 2: View Logs**

1. In left menu, select **Monitoring** → **Log stream**
2. Select replica (if multiple replicas running)
3. Logs stream in real-time

**Features:**

- Real-time streaming
- Multiple replicas side-by-side
- Color-coded log levels
- Download logs as file

**Step 3: Advanced Queries (Log Analytics)**

1. In left menu, select **Monitoring** → **Logs**
2. Write KQL (Kusto Query Language) query
3. Click "Run"

**Example queries:**

```kql
-- All logs from last hour
ContainerAppConsoleLogs_CL
| where TimeGenerated > ago(1h)
| order by TimeGenerated desc
| take 100

-- Error logs only
ContainerAppConsoleLogs_CL
| where Log_s contains "error" or Log_s contains "exception"
| order by TimeGenerated desc
| take 50

-- Logs for specific container
ContainerAppConsoleLogs_CL
| where ContainerName_s == "webapi"
| order by TimeGenerated desc
| take 100

-- Count errors per hour
ContainerAppConsoleLogs_CL
| where Log_s contains "error"
| summarize count() by bin(TimeGenerated, 1h)
| order by TimeGenerated desc
```

---

## Application Insights Logs

### Azure CLI Queries

**Basic Query:**

```bash
# Query Application Insights
az monitor app-insights query \
  --app edumind-dev \
  --analytics-query "traces | take 50" \
  --offset 1h
```

**Query Recent Errors:**

```bash
az monitor app-insights query \
  --app edumind-dev \
  --analytics-query "
    exceptions
    | where timestamp > ago(1h)
    | order by timestamp desc
    | project timestamp, type, outerMessage, problemId
    | take 50
  "
```

**Query Requests:**

```bash
az monitor app-insights query \
  --app edumind-dev \
  --analytics-query "
    requests
    | where timestamp > ago(1h)
    | summarize count(), avg(duration) by resultCode
    | order by count_ desc
  "
```

**Query by Specific Criteria:**

```bash
# Failed requests
az monitor app-insights query \
  --app edumind-dev \
  --analytics-query "
    requests
    | where resultCode >= 400
    | where timestamp > ago(1h)
    | project timestamp, name, resultCode, duration, url
    | take 50
  "
```

### Azure Portal Queries

**Step 1: Open Application Insights**

1. Azure Portal → Resource Groups → `rg-dev`
2. Click on Application Insights resource (`ai-edumind-dev`)

**Step 2: Navigate to Logs**

1. Left menu → **Monitoring** → **Logs**
2. Close any popup tutorials
3. Write KQL query in query window

**Useful Queries:**

**1. Application Performance:**

```kql
requests
| where timestamp > ago(24h)
| summarize 
    count(),
    avg(duration),
    percentile(duration, 50),
    percentile(duration, 95),
    percentile(duration, 99)
    by bin(timestamp, 1h)
| order by timestamp desc
```

**2. Error Rate Over Time:**

```kql
requests
| where timestamp > ago(24h)
| summarize 
    total = count(),
    errors = countif(resultCode >= 400)
    by bin(timestamp, 1h)
| extend errorRate = (errors * 100.0) / total
| project timestamp, total, errors, errorRate
| order by timestamp desc
```

**3. Slow Requests:**

```kql
requests
| where timestamp > ago(1h)
| where duration > 5000  // Slower than 5 seconds
| project timestamp, name, duration, url, resultCode
| order by duration desc
| take 50
```

**4. Dependency Failures (Database, Redis):**

```kql
dependencies
| where timestamp > ago(1h)
| where success == false
| project timestamp, name, type, data, resultCode
| order by timestamp desc
| take 50
```

**5. Exception Details:**

```kql
exceptions
| where timestamp > ago(1h)
| extend exceptionType = tostring(details[0].typeName)
| extend exceptionMessage = tostring(details[0].message)
| project timestamp, exceptionType, exceptionMessage, problemId, operation_Name
| order by timestamp desc
| take 50
```

**6. User Activity:**

```kql
pageViews
| where timestamp > ago(24h)
| summarize count() by bin(timestamp, 1h), name
| order by timestamp desc
```

**7. Custom Events (Assessment Submissions):**

```kql
customEvents
| where name == "AssessmentSubmitted"
| where timestamp > ago(24h)
| extend studentId = tostring(customDimensions.studentId)
| extend assessmentId = tostring(customDimensions.assessmentId)
| project timestamp, studentId, assessmentId
| take 50
```

---

## Monitoring Dashboards

### Aspire Dashboard (Local)

**Navigate to Dashboard:**

Usually at `http://localhost:18888` or `http://localhost:15000`

**Available Views:**

1. **Resources**
   - Shows all running services
   - Status (Running, Unhealthy, Stopped)
   - Replicas count
   - Endpoints

2. **Console Logs**
   - Real-time logs for each service
   - Filter by keyword
   - Filter by log level

3. **Traces**
   - Distributed tracing (if OpenTelemetry configured)
   - Request flow through services
   - Performance bottlenecks

4. **Metrics** (if configured)
   - CPU usage
   - Memory usage
   - Request rate
   - Error rate

### Azure Portal Dashboard

**Create Custom Dashboard:**

1. Azure Portal → **Dashboard**
2. Click **+ New dashboard**
3. Name it (e.g., "EduMind.AI Dev Monitoring")
4. Click **Done customizing**

**Add Tiles:**

1. Click **Edit**
2. Drag tiles from left panel:
   - **Metrics chart** (for CPU, memory, request rate)
   - **Logs** (recent errors)
   - **Application map** (service dependencies)
   - **Failures** (error summary)

**Recommended Tiles:**

- Container Apps CPU/Memory usage
- Application Insights request rate
- Application Insights failure rate
- PostgreSQL CPU/Memory/Connections
- Redis hits/misses

**Save and Share:**

1. Click **Done customizing**
2. Click **Share** → Set permissions
3. Copy URL to share with team

### Azure Monitor Workbooks

**Pre-built Workbook for Container Apps:**

1. Azure Portal → Container Apps Environment (`cae-dev`)
2. Left menu → **Monitoring** → **Workbooks**
3. Select **Container Apps Performance** template
4. Customize as needed

**Create Custom Workbook:**

1. **Workbooks** → **+ New**
2. Add query widgets:

   **Example: Request Rate**

   ```kql
   requests
   | where timestamp > ago(24h)
   | summarize count() by bin(timestamp, 5m)
   | render timechart
   ```

   **Example: Error Rate**

   ```kql
   requests
   | summarize 
       total = count(),
       errors = countif(resultCode >= 400)
       by bin(timestamp, 5m)
   | extend errorRate = (errors * 100.0) / total
   | render timechart
   ```

3. Add parameters (environment, time range)
4. Save workbook

---

## Real-Time Monitoring Commands

### Quick Health Check

**Local:**

```bash
# Create health-check.sh script
cat > health-check.sh << 'EOF'
#!/bin/bash
echo "=== EduMind.AI Health Check ==="
echo ""
echo -n "Web API: "
curl -s http://localhost:5103/health
echo ""
echo -n "Student App (HTTP): "
curl -s -o /dev/null -w "%{http_code}" http://localhost:5049
echo ""
echo -n "Dashboard (HTTP): "
curl -s -o /dev/null -w "%{http_code}" http://localhost:5050
echo ""
echo ""
echo "=== Dependencies ==="
echo -n "PostgreSQL: "
docker ps --filter "name=postgres" --format "{{.Status}}" | grep -q "Up" && echo "Running" || echo "Stopped"
echo -n "Redis: "
docker ps --filter "name=redis" --format "{{.Status}}" | grep -q "Up" && echo "Running" || echo "Stopped"
EOF

chmod +x health-check.sh
./health-check.sh
```

**Azure:**

```bash
# Create azure-health-check.sh script
cat > azure-health-check.sh << 'EOF'
#!/bin/bash
ENV=${1:-dev}
echo "=== EduMind.AI Azure Health Check ($ENV) ==="
echo ""

# Get API URL
API_URL=$(az containerapp show \
  --name ca-webapi-$ENV \
  --resource-group rg-$ENV \
  --query "properties.configuration.ingress.fqdn" \
  --output tsv)

echo -n "Web API: "
curl -s https://$API_URL/health
echo ""

# Check all container apps
echo ""
echo "=== Container Apps Status ==="
az containerapp list \
  --resource-group rg-$ENV \
  --query "[].{Name:name, Status:properties.runningStatus, Replicas:properties.template.scale.minReplicas}" \
  --output table
EOF

chmod +x azure-health-check.sh
./azure-health-check.sh dev
```

### Watch Mode (Continuous Monitoring)

**Local - Watch Health:**

```bash
# Check health every 5 seconds
watch -n 5 'curl -s http://localhost:5103/health'
```

**Azure - Watch Container Status:**

```bash
# Watch container app status
watch -n 10 'az containerapp show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --query "properties.runningStatus" \
  --output tsv'
```

### Log Tailing with Filters

**Local - Tail Errors Only:**

```bash
# In Aspire dashboard, or use Docker
docker logs $(docker ps --filter "name=webapi" --format "{{.ID}}") \
  --follow --tail 100 | grep -i "error\|exception\|fail"
```

**Azure - Tail Errors Only:**

```bash
# Stream logs and filter for errors
az containerapp logs show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --follow | grep -i "error\|exception\|fail"
```

---

## Alerting Setup

### Application Insights Alerts

**Create Error Rate Alert:**

```bash
# Create action group first (email notification)
az monitor action-group create \
  --name email-admins \
  --resource-group rg-dev \
  --short-name EmailAdmins \
  --email-receiver name=Admin email=admin@edumind.ai

# Create metric alert for high error rate
az monitor metrics alert create \
  --name high-error-rate \
  --resource-group rg-dev \
  --scopes /subscriptions/<sub-id>/resourceGroups/rg-dev/providers/Microsoft.Insights/components/ai-edumind-dev \
  --condition "avg requests/failed > 10" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action email-admins \
  --description "Alert when error rate exceeds 10 requests/min"
```

**Create Slow Response Alert:**

```bash
az monitor metrics alert create \
  --name slow-response-time \
  --resource-group rg-dev \
  --scopes /subscriptions/<sub-id>/resourceGroups/rg-dev/providers/Microsoft.Insights/components/ai-edumind-dev \
  --condition "avg requests/duration > 5000" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action email-admins \
  --description "Alert when avg response time exceeds 5 seconds"
```

### Container Apps Alerts

**Create Replica Down Alert:**

```bash
az monitor metrics alert create \
  --name container-app-down \
  --resource-group rg-dev \
  --scopes /subscriptions/<sub-id>/resourceGroups/rg-dev/providers/Microsoft.App/containerApps/ca-webapi-dev \
  --condition "avg Replicas == 0" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action email-admins \
  --description "Alert when no replicas are running"
```

---

## Performance Monitoring

### Key Metrics to Track

**Application Performance:**

- **Request rate:** requests/minute
- **Error rate:** errors/minute or %
- **Response time:** avg, p50, p95, p99 (ms)
- **Dependency duration:** database, cache, LLM calls (ms)

**Infrastructure Performance:**

- **CPU usage:** % per container
- **Memory usage:** MB or % per container
- **Network throughput:** MB/s
- **Active connections:** count (database, cache)

### Query Performance Metrics

**Application Insights - Request Performance:**

```kql
requests
| where timestamp > ago(1h)
| summarize 
    count = count(),
    avgDuration = avg(duration),
    p50 = percentile(duration, 50),
    p95 = percentile(duration, 95),
    p99 = percentile(duration, 99)
    by name
| order by p95 desc
```

**Application Insights - Dependency Performance:**

```kql
dependencies
| where timestamp > ago(1h)
| summarize 
    count = count(),
    avgDuration = avg(duration),
    p95 = percentile(duration, 95),
    successRate = (count(success == true) * 100.0) / count()
    by name, type
| order by avgDuration desc
```

**Container Apps Metrics (Azure CLI):**

```bash
# CPU usage
az monitor metrics list \
  --resource /subscriptions/<sub-id>/resourceGroups/rg-dev/providers/Microsoft.App/containerApps/ca-webapi-dev \
  --metric "UsageNanoCores" \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%SZ) \
  --interval PT1M \
  --output table

# Memory usage
az monitor metrics list \
  --resource /subscriptions/<sub-id>/resourceGroups/rg-dev/providers/Microsoft.App/containerApps/ca-webapi-dev \
  --metric "WorkingSetBytes" \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%SZ) \
  --interval PT1M \
  --output table
```

---

**Status:** 📊 Monitoring Configured  
**Next Playbook:** [07-scale-services.md](./07-scale-services.md)
