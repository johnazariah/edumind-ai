# Playbook: Troubleshoot Unhealthy Services

**Scenario:** Services show "Unhealthy" status or fail health checks  
**Time Required:** 15-30 minutes  
**Difficulty:** Intermediate  
**Applies To:** Both local and Azure deployments

---

## Quick Diagnosis (5 minutes)

### Step 1: Check Health Endpoint

**Local:**

```bash
# Check Web API health
curl http://localhost:5103/health

# Check detailed health
curl http://localhost:5103/health/ready
```

**Azure:**

```bash
# Get Web API URL
export API_URL=$(azd show --environment dev --output json | jq -r '.services.webapi.endpoint')

# Check health
curl $API_URL/health

# Detailed health with component status
curl $API_URL/health/ready
```

**Expected responses:**

- **Healthy:** `"Healthy"` (status 200)
- **Degraded:** JSON with partially unhealthy components (status 200)
- **Unhealthy:** JSON with error details (status 503)

### Step 2: Identify Failed Component

Parse the health check response to find which component is failing:

**Example unhealthy response:**

```json
{
  "status": "Unhealthy",
  "results": {
    "PostgreSQL": {
      "status": "Unhealthy",
      "description": "Connection timeout",
      "exception": "Npgsql.NpgsqlException: Connection refused"
    },
    "Redis": {
      "status": "Healthy",
      "description": null
    },
    "Agents": {
      "status": "Healthy",
      "description": null
    }
  }
}
```

**Quick interpretation:**

- **PostgreSQL Unhealthy:** Database connection problem (see [PostgreSQL Issues](#postgresql-issues))
- **Redis Unhealthy:** Cache connection problem (see [Redis Issues](#redis-issues))
- **Agents Unhealthy:** LLM or Semantic Kernel problem (see [Agent Issues](#agent-issues))

---

## PostgreSQL Issues

### Symptom: "Connection refused" or "Connection timeout"

**Local Diagnosis:**

```bash
# Check if PostgreSQL container is running
docker ps | grep postgres

# If not running, check logs
docker logs $(docker ps -a --filter "name=postgres" --format "{{.ID}}") --tail 50

# Check container status
docker inspect $(docker ps --filter "name=postgres" --format "{{.ID}}") | jq '.[0].State'
```

**Local Solutions:**

**1. Container not started:**

```bash
# Restart Aspire (it will start PostgreSQL)
# Ctrl+C in Aspire terminal, then:
dotnet run --project src/EduMind.AppHost/EduMind.AppHost.csproj
```

**2. Container crashed:**

```bash
# Check why it crashed
docker logs $(docker ps -a --filter "name=postgres" --format "{{.ID}}") --tail 100

# Common cause: Port 5432 already in use
# Find process using port
lsof -i :5432  # macOS/Linux
netstat -ano | findstr :5432  # Windows

# Kill the process or change port in appsettings
```

**3. Wrong connection string:**

Check `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "AcademicDatabase": "Host=localhost;Port=5432;Database=edumind_dev;Username=edumind_user;Password=dev_password;Include Error Detail=true"
  }
}
```

Verify:

- Host is `localhost` (not `postgres`)
- Port matches Docker port mapping
- Database name, username, password are correct

**Azure Diagnosis:**

```bash
# Check PostgreSQL status
az postgres flexible-server show \
  --resource-group rg-dev \
  --name psql-dev-<suffix> \
  --query state \
  --output tsv

# Expected: "Ready"

# Check firewall rules
az postgres flexible-server firewall-rule list \
  --resource-group rg-dev \
  --name psql-dev-<suffix> \
  --output table
```

**Azure Solutions:**

**1. PostgreSQL not ready:**

```bash
# Check if server is starting
az postgres flexible-server show \
  --resource-group rg-dev \
  --name psql-dev-<suffix> \
  --query state

# If "Updating" or "Starting", wait 5 minutes
# If "Stopped", start it:
az postgres flexible-server start \
  --resource-group rg-dev \
  --name psql-dev-<suffix>
```

**2. Firewall blocking Container Apps:**

```bash
# Allow all Azure services (quick fix)
az postgres flexible-server firewall-rule create \
  --resource-group rg-dev \
  --name psql-dev-<suffix> \
  --rule-name AllowAllAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Or allow specific Container Apps subnet (more secure)
# Get subnet CIDR
az containerapp env show \
  --resource-group rg-dev \
  --name cae-dev \
  --query "properties.infrastructureSubnetId" \
  --output tsv

# Add firewall rule for that subnet
az postgres flexible-server firewall-rule create \
  --resource-group rg-dev \
  --name psql-dev-<suffix> \
  --rule-name AllowContainerAppsSubnet \
  --start-ip-address <subnet-start> \
  --end-ip-address <subnet-end>
```

**3. Wrong FQDN (known azd issue):**

This is the template variable substitution problem. Check Web API logs:

```bash
# View logs
az containerapp logs show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --tail 100 | grep -i "fqdn\|connection"

# Look for:
# - "Detected FQDN: <domain>"
# - "Patched connection string"
# - "Failed to patch connection string" (ERROR)
```

If patching failed, manually verify connection string environment variable:

```bash
# Get current connection string
az containerapp show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --query "properties.template.containers[0].env" \
  --output json | jq '.[] | select(.name=="ConnectionStrings__AcademicDatabase")'

# Should contain:
# Host=postgres.internal.<container-apps-domain>
# NOT Host=postgres;
```

If wrong, update it:

```bash
az containerapp update \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --set-env-vars "ConnectionStrings__AcademicDatabase=Host=postgres.internal.<domain>;Database=edumind;Username=edumind_admin;Password=<from-keyvault>"
```

---

## Redis Issues

### Symptom: "Connection refused" or "No such host"

**Local Diagnosis:**

```bash
# Check if Redis container is running
docker ps | grep redis

# Test connection
docker exec -it $(docker ps --filter "name=redis" --format "{{.ID}}") redis-cli ping
# Expected: "PONG"

# Check logs
docker logs $(docker ps --filter "name=redis" --format "{{.ID}}") --tail 50
```

**Local Solutions:**

**1. Container not running:**

```bash
# Restart Aspire (it will start Redis)
# Ctrl+C in Aspire terminal, then:
dotnet run --project src/EduMind.AppHost/EduMind.AppHost.csproj
```

**2. Connection string wrong:**

Check `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "cache": "localhost:6379"
  }
}
```

Verify port matches Docker port mapping (check Aspire Dashboard).

**Azure Diagnosis:**

In Azure, Redis runs as a container in Container Apps, not as a managed service.

```bash
# Check Redis container app status
az containerapp show \
  --name ca-redis-dev \
  --resource-group rg-dev \
  --query "properties.runningStatus" \
  --output tsv

# Expected: "Running"

# Check logs
az containerapp logs show \
  --name ca-redis-dev \
  --resource-group rg-dev \
  --tail 100
```

**Azure Solutions:**

**1. Redis container not running:**

```bash
# Restart Redis
az containerapp revision restart \
  --name ca-redis-dev \
  --resource-group rg-dev \
  --revision <latest-revision-name>

# Wait 30 seconds, check status
az containerapp show \
  --name ca-redis-dev \
  --resource-group rg-dev \
  --query "properties.runningStatus"
```

**2. Wrong FQDN (same issue as PostgreSQL):**

Check Web API logs for FQDN patching:

```bash
az containerapp logs show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --tail 100 | grep -i "redis\|cache"
```

If wrong, manually update connection string:

```bash
az containerapp update \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --set-env-vars "ConnectionStrings__cache=redis.internal.<domain>:6379"
```

---

## Agent Issues

### Symptom: "Agents unhealthy" or "Ollama connection failed"

**Local Diagnosis:**

```bash
# Check if Ollama is running
curl http://localhost:11434/api/tags

# Expected: JSON list of models

# Check if llama3.2:3b model is available
curl http://localhost:11434/api/tags | jq '.models[] | select(.name=="llama3.2:3b")'
```

**Local Solutions:**

**1. Ollama not running:**

**macOS/Linux:**

```bash
# Start Ollama service
ollama serve
```

**Windows:**

```bash
# Start Ollama from Start Menu or:
& "C:\Users\<YourName>\AppData\Local\Programs\Ollama\ollama.exe" serve
```

**2. Model not pulled:**

```bash
# Pull the model
ollama pull llama3.2:3b

# This takes 5-10 minutes (1.8GB download)
```

**3. Use Stub Provider (Workaround):**

If you don't need real LLM responses during development:

Edit `appsettings.Development.json`:

```json
{
  "LLM": {
    "Provider": "Stub"
  }
}
```

Restart Aspire. Health checks will now pass without Ollama.

**Azure Diagnosis:**

**Important:** Ollama does NOT run in Azure by default (Azure uses Stub provider or Azure OpenAI).

Check configuration:

```bash
# Get LLM provider setting
az containerapp show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --query "properties.template.containers[0].env" \
  --output json | jq '.[] | select(.name=="LLM__Provider")'

# Expected: "Stub" or "AzureOpenAI" (NOT "Ollama")
```

**Azure Solutions:**

**1. If accidentally set to Ollama:**

```bash
# Change to Stub provider
az containerapp update \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --set-env-vars "LLM__Provider=Stub"
```

**2. If using Azure OpenAI and it's failing:**

```bash
# Check Azure OpenAI endpoint
az containerapp show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --query "properties.template.containers[0].env" \
  --output json | jq '.[] | select(.name | startswith("AzureOpenAI"))'

# Verify:
# - AzureOpenAI__Endpoint is set
# - AzureOpenAI__ApiKey is set
# - AzureOpenAI__DeploymentName is correct

# Test endpoint from Azure Cloud Shell
curl https://<your-openai-endpoint>.openai.azure.com/openai/deployments?api-version=2023-05-15 \
  -H "api-key: <your-api-key>"
```

---

## Application Startup Issues

### Symptom: Container Apps stuck in "Provisioning" or crash loop

**Azure Diagnosis:**

```bash
# Check revision status
az containerapp revision list \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --output table

# Look for:
# - ProvisioningState: "Provisioned" (good)
# - TrafficWeight: 100 (active)
# - Replicas: >0 (running)
```

**Azure Solutions:**

**1. View startup logs:**

```bash
# Get logs from current revision
az containerapp logs show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --revision <revision-name> \
  --tail 200

# Look for:
# - Unhandled exceptions
# - Missing environment variables
# - Configuration errors
```

**2. Common startup errors:**

**Missing environment variable:**

```bash
# List all environment variables
az containerapp show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --query "properties.template.containers[0].env" \
  --output table

# Add missing variable
az containerapp update \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --set-env-vars "MISSING_VAR=value"
```

**Wrong image:**

```bash
# Check current image
az containerapp show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --query "properties.template.containers[0].image" \
  --output tsv

# Should be: <acr-name>.azurecr.io/webapi:latest

# If wrong, update:
az containerapp update \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --image <correct-image-name>
```

**Insufficient resources:**

```bash
# Check resource limits
az containerapp show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --query "properties.template.containers[0].resources"

# Increase CPU/memory if needed
az containerapp update \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --cpu 1.0 \
  --memory 2.0Gi
```

---

## Network Connectivity Issues

### Symptom: Services can't reach each other

**Local Diagnosis:**

All services should be on the same Docker network created by Aspire.

```bash
# Check networks
docker network ls

# Inspect Aspire network
docker network inspect <aspire-network-name>

# Verify all containers are connected
```

**Local Solutions:**

Usually fixed by restarting Aspire. If persistent:

```bash
# Remove stale networks
docker network prune

# Restart Docker Desktop
# Then restart Aspire
```

**Azure Diagnosis:**

```bash
# Check Container Apps Environment
az containerapp env show \
  --name cae-dev \
  --resource-group rg-dev \
  --query "properties.provisioningState"

# Expected: "Succeeded"

# Check internal DNS
az containerapp show \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --query "properties.configuration.ingress.fqdn"
```

**Azure Solutions:**

**1. Internal ingress not configured:**

```bash
# Verify internal ingress is enabled
az containerapp ingress show \
  --name ca-redis-dev \
  --resource-group rg-dev

# Should show:
# - external: false
# - targetPort: 6379

# If wrong, update:
az containerapp ingress enable \
  --name ca-redis-dev \
  --resource-group rg-dev \
  --target-port 6379 \
  --transport tcp \
  --allow-insecure false \
  --type internal
```

**2. Service discovery failing:**

This is related to the FQDN patching issue. See [Runtime FQDN Detection](../reference.md#runtime-fqdn-detection) in reference guide.

---

## Performance Issues (Slow Health Checks)

### Symptom: Health checks timeout or take >10 seconds

**Diagnosis:**

```bash
# Time the health check
time curl http://localhost:5103/health

# If >5 seconds, there's a performance issue
```

**Solutions:**

**1. Database query slow:**

```bash
# Connect to PostgreSQL
docker exec -it $(docker ps --filter "name=postgres" --format "{{.ID}}") psql -U edumind_user -d edumind_dev

# Check slow queries
SELECT pid, now() - pg_stat_activity.query_start AS duration, query 
FROM pg_stat_activity 
WHERE (now() - pg_stat_activity.query_start) > interval '5 seconds';

# Kill long-running query (if needed)
SELECT pg_terminate_backend(<pid>);
```

**2. Increase health check timeout:**

**Local:** Edit `src/AcademicAssessment.Web/Program.cs`:

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString, 
        name: "postgresql",
        timeout: TimeSpan.FromSeconds(10))  // Increase from default 5s
```

**Azure:**

```bash
# Update health probe timeout
az containerapp update \
  --name ca-webapi-dev \
  --resource-group rg-dev \
  --health-probe-timeout 10
```

**3. Too many concurrent health checks:**

If you have monitoring tools hitting `/health` frequently:

- Use `/health/live` for basic liveness (faster, no DB check)
- Use `/health/ready` only during startup/deployment

---

## Emergency Quick Fixes

### Nuclear Option 1: Restart Everything (Local)

```bash
# Stop Aspire
# Ctrl+C in Aspire terminal

# Stop all containers
docker stop $(docker ps -q)

# Remove containers
docker rm $(docker ps -aq)

# Restart Docker Desktop

# Start Aspire
dotnet run --project src/EduMind.AppHost/EduMind.AppHost.csproj
```

### Nuclear Option 2: Restart Everything (Azure)

```bash
# Restart all Container Apps
for app in webapi studentapp dashboard; do
  az containerapp revision restart \
    --name ca-$app-dev \
    --resource-group rg-dev \
    --revision $(az containerapp revision list \
      --name ca-$app-dev \
      --resource-group rg-dev \
      --query "[0].name" \
      --output tsv)
done

# Wait 2 minutes
sleep 120

# Check health
curl $(azd show --environment dev --output json | jq -r '.services.webapi.endpoint')/health
```

### Nuclear Option 3: Redeploy from Scratch (Azure)

```bash
# WARNING: This deletes all Azure resources and data!

# Take database backup first (if data is important)
pg_dump -h <postgres-fqdn> -U edumind_admin -d edumind > backup.sql

# Delete everything
azd down --purge --force --environment dev

# Redeploy
azd up --environment dev

# Restore database
psql -h <new-postgres-fqdn> -U edumind_admin -d edumind < backup.sql
```

---

## Preventive Measures

To avoid future health check issues:

1. **Monitor proactively:**

   ```bash
   # Set up health check monitoring (Azure)
   az monitor metrics alert create \
     --name "api-unhealthy" \
     --resource-group rg-dev \
     --scopes <containerapp-resource-id> \
     --condition "avg Replicas == 0" \
     --window-size 5m \
     --evaluation-frequency 1m
   ```

2. **Test health checks in CI/CD:**

   Add to `.github/workflows/deploy-azure-azd.yml`:

   ```yaml
   - name: Verify Health
     run: |
       for i in {1..30}; do
         STATUS=$(curl -s -o /dev/null -w "%{http_code}" ${{ env.API_URL }}/health)
         if [ $STATUS -eq 200 ]; then
           echo "Health check passed"
           exit 0
         fi
         echo "Attempt $i: Health check returned $STATUS, retrying..."
         sleep 10
       done
       echo "Health check failed after 30 attempts"
       exit 1
   ```

3. **Use dedicated health check endpoints:**

   - `/health/live` - Basic liveness (no dependencies)
   - `/health/ready` - Readiness with dependency checks
   - `/health` - Overall health

4. **Set appropriate timeouts:**

   - Local dev: 5-10 seconds
   - Azure staging: 15 seconds
   - Azure production: 30 seconds

---

## When to Escalate

Escalate to infrastructure team if:

- Health checks fail consistently after following all troubleshooting steps
- Database or Redis show persistent connectivity issues
- Azure resources fail to provision correctly
- FQDN detection workaround stops working after azd update
- Container Apps crash loop with no clear error in logs

---

**Status:** 🩺 Troubleshooting Complete  
**Next Playbook:** [05-rollback-deployment.md](./05-rollback-deployment.md)
