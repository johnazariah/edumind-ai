# Aspire Testing Guide - Phase 3

**Date:** October 16, 2025  
**Branch:** `feature/aspire-migration`  
**Status:** Testing in Progress

## Overview

This guide helps you test the .NET Aspire integration for EduMind.AI. The goal is to verify that all services start correctly, communicate with each other, and that the Aspire dashboard provides useful observability.

## Quick Start

### 1. Start the AppHost

```bash
cd /workspaces/edumind-ai
dotnet run --project src/EduMind.AppHost --launch-profile http
```

### 2. Access the Aspire Dashboard

Open your browser to:
- **HTTP:** http://localhost:15056
- **HTTPS:** https://localhost:17126

The Aspire dashboard provides:
- Real-time service status
- Distributed tracing
- Metrics and logging
- Resource management

## Testing Checklist

### ✅ Phase 3.1: Verify Services Start

**Expected Behavior:**
All services should start automatically when you run the AppHost.

| Service | Port | Status | Notes |
|---------|------|--------|-------|
| **Aspire Dashboard** | 15056 (HTTP)<br>17126 (HTTPS) | ⏳ Pending | Main orchestration UI |
| **PostgreSQL** | 45627 | ⏳ Pending | Database container |
| **Redis** | 42467 | ⏳ Pending | Cache container |
| **OLLAMA** | 11434 | ⏳ Pending | LLM container (optional) |
| **Web API** | 5103 (HTTP)<br>7026 (HTTPS) | ⏳ Pending | REST API backend |
| **Dashboard** | 5183 (HTTP)<br>7156 (HTTPS) | ⏳ Pending | Admin Blazor app |
| **StudentApp** | 5049 (HTTP)<br>7073 (HTTPS) | ⏳ Pending | Student Blazor app |

**How to Test:**
1. Check the Aspire dashboard at http://localhost:15056
2. Look for all services in the "Resources" tab
3. Verify each service shows as "Running" with a green status

**Troubleshooting:**
- If PostgreSQL fails: Check Docker is running
- If Redis fails: Check Docker is running
- If OLLAMA fails: This is optional, can be ignored
- If apps fail: Check the logs in the Aspire dashboard

### ✅ Phase 3.2: Test Aspire Dashboard Features

**Features to Explore:**

#### A. Resources View
- [ ] Can see all 7 resources listed
- [ ] Status indicators are visible
- [ ] Can view logs for each resource
- [ ] Can see environment variables

#### B. Traces View
- [ ] Can see distributed traces
- [ ] Traces show service-to-service calls
- [ ] Can filter traces by service
- [ ] Can view trace details with timing

#### C. Metrics View
- [ ] Can see HTTP request metrics
- [ ] Can see database query metrics
- [ ] Can see custom application metrics
- [ ] Graphs are rendering correctly

#### D. Logs View
- [ ] Can see logs from all services
- [ ] Can filter by service name
- [ ] Can filter by log level
- [ ] Logs are timestamped correctly

**How to Test:**
1. Navigate through each tab in the Aspire dashboard
2. Make some API requests (see Phase 3.3)
3. Verify traces, metrics, and logs appear
4. Test filtering and search functionality

### ✅ Phase 3.3: Test API Endpoints

**Test the Web API is accessible:**

```bash
# Health check
curl http://localhost:5103/health

# Swagger UI
open http://localhost:5103/swagger

# Test endpoint (if available)
curl http://localhost:5103/api/v1/health
```

**Expected Results:**
- Health check returns 200 OK
- Swagger UI loads successfully
- API endpoints are accessible

**How to Test:**
1. Open http://localhost:5103/swagger in browser
2. Verify Swagger UI loads
3. Try executing a GET endpoint
4. Check the Aspire dashboard for the request trace

### ✅ Phase 3.4: Test Database Connectivity

**Verify EF Migrations Ran:**

```bash
# Check if database exists and has tables
docker exec -it $(docker ps -q -f name=postgres) psql -U edumind_user -d edumind_dev -c "\dt"
```

**Expected Results:**
- Tables exist: courses, questions, student_responses, etc.
- Migrations table shows applied migrations

**How to Test:**
1. Run the command above
2. Verify you see the expected tables
3. Check Aspire dashboard logs for migration messages

### ✅ Phase 3.5: Test Service Discovery

**Verify Services Can Find Each Other:**

The Dashboard and StudentApp should be able to call the Web API automatically through Aspire's service discovery.

**How to Test:**
1. Open Dashboard: http://localhost:5183
2. If it loads data, service discovery is working
3. Check Aspire traces for Dashboard → Web API calls

### ✅ Phase 3.6: Test OpenTelemetry Integration

**Verify Distributed Tracing:**

**How to Test:**
1. Make an API request: `curl http://localhost:5103/swagger`
2. Go to Aspire dashboard → Traces tab
3. Find the trace for your request
4. Click on it to see the span details
5. Verify you see:
   - HTTP request span
   - Database query spans (if any)
   - Service name and timestamps

**Expected Behavior:**
- Traces appear within seconds
- All spans are connected
- Timing information is accurate

### ✅ Phase 3.7: Test Logging

**Verify Unified Logging:**

**How to Test:**
1. Go to Aspire dashboard → Logs tab
2. Filter by "webapi"
3. Look for "EduMind.AI Web API started successfully"
4. Test other filters (log level, timestamp)

**Expected Behavior:**
- Logs from all services appear
- Serilog formatting is preserved
- Can search and filter effectively

### ✅ Phase 3.8: Test Resource Management

**Verify Container Management:**

**How to Test:**
1. In Aspire dashboard, find PostgreSQL resource
2. Click on it to see details
3. Check:
   - Container status
   - Port mappings
   - Environment variables
   - Volume mounts
4. Try viewing logs for the container

### ✅ Phase 3.9: Test Graceful Shutdown

**Verify Services Stop Cleanly:**

**How to Test:**
1. Press `Ctrl+C` in the terminal running the AppHost
2. Watch the Aspire dashboard
3. Verify all services stop gracefully
4. Check no containers are left running:
   ```bash
   docker ps
   ```

**Expected Behavior:**
- All services stop within 30 seconds
- No errors in the logs
- Containers are removed

### ✅ Phase 3.10: Compare with Docker Compose

**Performance Comparison:**

| Metric | Docker Compose | Aspire | Winner |
|--------|----------------|--------|--------|
| **Startup Time** | ~30 seconds | ⏳ Testing | TBD |
| **Terminal Windows** | 4+ | 1 | ✅ Aspire |
| **Unified Logs** | No | Yes | ✅ Aspire |
| **Distributed Tracing** | Manual setup | Built-in | ✅ Aspire |
| **Service Discovery** | Manual | Automatic | ✅ Aspire |
| **Development UX** | Good | ⏳ Testing | TBD |

**How to Test:**
1. Note Aspire startup time
2. Try debugging across services
3. Compare log viewing experience
4. Test making changes and restarting

## Known Issues & Workarounds

### Issue 1: Browser Doesn't Auto-Open
**Problem:** Aspire dashboard doesn't open automatically  
**Workaround:** Manually open http://localhost:15056

### Issue 2: OLLAMA Container Slow to Start
**Problem:** OLLAMA takes 30+ seconds to download model  
**Workaround:** This is normal, wait for it or disable if not needed

### Issue 3: PostgreSQL Permission Errors
**Problem:** Database migrations fail with permission errors  
**Workaround:** Check Docker volume permissions

## Success Criteria

Phase 3 is **COMPLETE** when:

- [ ] ✅ All 7 services start successfully
- [ ] ✅ Aspire dashboard is accessible and functional
- [ ] ✅ Distributed tracing shows service calls
- [ ] ✅ Logs are visible and searchable
- [ ] ✅ Metrics are being collected
- [ ] ✅ API endpoints are accessible
- [ ] ✅ Database migrations complete
- [ ] ✅ Service discovery works
- [ ] ✅ Can debug across services
- [ ] ✅ Performance is acceptable

## Performance Benchmarks

Record these metrics for comparison:

- **Cold Start Time:** ______ seconds (first run)
- **Warm Start Time:** ______ seconds (subsequent runs)
- **Memory Usage:** ______ MB (total for all services)
- **CPU Usage:** ______ % (average)
- **Dashboard Responsiveness:** ______ (fast/slow)

## Next Steps After Phase 3

Once testing is complete:

1. **Document Issues:** Note any problems in GitHub Issues
2. **Update Configuration:** Fix any configuration issues found
3. **Proceed to Phase 4:** Clean up legacy configuration
4. **Update Documentation:** Add screenshots to README

## Screenshots to Capture

For documentation, capture:

1. Aspire dashboard main view with all services
2. Traces view showing distributed trace
3. Logs view with multiple services
4. Metrics dashboard
5. Resource details for Web API
6. Swagger UI of the API

## Useful Commands

```bash
# Start AppHost
dotnet run --project src/EduMind.AppHost

# View AppHost logs
dotnet run --project src/EduMind.AppHost --verbosity detailed

# Check running containers
docker ps

# Check Docker logs
docker logs <container-id>

# Stop all containers
docker stop $(docker ps -q)

# Remove all containers
docker rm $(docker ps -a -q)

# Check port usage
netstat -tulpn | grep LISTEN
```

## Troubleshooting

### Services Won't Start

**Symptoms:** Services show as "Failed" in dashboard

**Checks:**
1. Docker is running: `docker ps`
2. Ports are available: `netstat -tulpn | grep <port>`
3. Check logs in Aspire dashboard
4. Look for errors in terminal output

**Solutions:**
- Stop conflicting services
- Free up required ports
- Restart Docker
- Check firewall settings

### Dashboard Not Accessible

**Symptoms:** Cannot access http://localhost:15056

**Checks:**
1. AppHost is running
2. Port 15056 is not in use by another process
3. Browser is not blocking localhost

**Solutions:**
- Try HTTPS: https://localhost:17126
- Check AppHost logs for actual port
- Restart AppHost

### No Traces Appearing

**Symptoms:** Traces tab is empty

**Checks:**
1. OpenTelemetry is configured (should be automatic)
2. Services are making HTTP requests
3. OTLP endpoint is correct

**Solutions:**
- Make some API requests
- Check service logs for OTLP errors
- Verify ServiceDefaults is added to all apps

### Database Connection Errors

**Symptoms:** API fails to connect to PostgreSQL

**Checks:**
1. PostgreSQL container is running
2. Connection string is correct (should be automatic)
3. Migrations completed

**Solutions:**
- Check PostgreSQL logs in dashboard
- Verify connection string environment variable
- Manually run migrations

## Additional Resources

- [Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Aspire Dashboard Guide](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard)
- [OpenTelemetry .NET](https://open telemetry.io/docs/languages/net/)
- [EduMind.AI Aspire Analysis](./ASPIRE_ANALYSIS.md)
- [EduMind.AI Aspire Migration Log](./ASPIRE_MIGRATION_LOG.md)

---

**Ready to Start Testing?**

Run the AppHost and open the Aspire dashboard to begin! 🚀

```bash
cd /workspaces/edumind-ai
dotnet run --project src/EduMind.AppHost --launch-profile http
```

Then open: **http://localhost:15056**
