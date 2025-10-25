# Playbook: Daily Development Workflow

**Scenario:** Typical day-to-day development with local Aspire environment  
**Time Required:** 5-10 minutes per session  
**Difficulty:** Beginner

---

## Prerequisites

- [x] Completed [01-first-time-local-setup.md](./01-first-time-local-setup.md)
- [x] Local environment fully configured
- [x] Docker Desktop running

---

## Morning Startup Routine (5 minutes)

### Step 1: Start Docker Desktop

**Windows/macOS:**

1. Launch Docker Desktop from Applications/Start Menu
2. Wait for Docker icon in system tray to stabilize (no animation)
3. Verify: Docker Desktop dashboard shows "Engine running"

### Step 2: Pull Latest Changes

```bash
# Navigate to repository
cd ~/workspace/edumind-ai

# Fetch and pull latest from main
git checkout main
git pull origin main

# Check if there are new migrations or dependencies
git log --oneline -10
```

### Step 3: Update Dependencies (if needed)

**Run this if package changes detected:**

```bash
# Restore NuGet packages
dotnet restore EduMind.AI.sln

# Rebuild solution
dotnet build EduMind.AI.sln --configuration Debug
```

### Step 4: Start Aspire

```bash
# Start Aspire orchestration
dotnet run --project src/EduMind.AppHost/EduMind.AppHost.csproj
```

**Expected behavior:**

- Aspire Dashboard opens automatically in browser
- PostgreSQL starts (usually takes 10-15 seconds)
- Redis starts (usually takes 5 seconds)
- All apps start once dependencies are healthy
- Console shows URLs for all services

**Aspire Dashboard URL:** Usually `http://localhost:15000` or `http://localhost:18888`

### Step 5: Verify Health

**Quick check in terminal:**

```bash
# Wait 30 seconds after Aspire starts, then:
curl http://localhost:5103/health

# Expected: "Healthy"
```

**Or check Aspire Dashboard:**

- Navigate to Resources tab
- Verify all services show green checkmarks
- Click on webapi → Logs to see startup messages

---

## Development Cycle

### Making Code Changes

#### For API Changes (src/AcademicAssessment.Web)

1. **Stop Aspire:** Press `Ctrl+C` in Aspire terminal
2. **Make your changes** in VS Code or your IDE
3. **Rebuild specific project:**

   ```bash
   dotnet build src/AcademicAssessment.Web --configuration Debug
   ```

4. **Restart Aspire:**

   ```bash
   dotnet run --project src/EduMind.AppHost/EduMind.AppHost.csproj
   ```

**Hot Reload (Alternative):**

Aspire supports hot reload for some changes:

- File-scoped changes in controllers/services often hot-reload
- Watch console for "Hot reload succeeded" messages
- No need to restart for minor changes

#### For Blazor Changes (StudentApp/Dashboard)

1. **Keep Aspire running** (Blazor supports better hot reload)
2. **Make your changes**
3. **Save the file**
4. **Refresh browser** (or wait for auto-reload)

**If hot reload fails:**

- Restart Aspire with `Ctrl+C` and re-run
- Check console for hot reload error messages

#### For Domain Model Changes (Core/Infrastructure)

1. **Stop Aspire**
2. **Make your changes**
3. **If database schema affected, create migration:**

   ```bash
   dotnet ef migrations add <MigrationName> \
     --project src/AcademicAssessment.Infrastructure \
     --startup-project src/AcademicAssessment.Web
   ```

4. **Rebuild entire solution:**

   ```bash
   dotnet build EduMind.AI.sln --configuration Debug
   ```

5. **Apply migration (if created):**

   ```bash
   dotnet ef database update \
     --project src/AcademicAssessment.Infrastructure \
     --startup-project src/AcademicAssessment.Web
   ```

6. **Restart Aspire**

### Running Tests

#### Unit Tests (Fast - Run Often)

```bash
# Run all unit tests
dotnet test tests/AcademicAssessment.Tests.Unit \
  --configuration Debug \
  --verbosity minimal

# Run specific test class
dotnet test tests/AcademicAssessment.Tests.Unit \
  --filter "FullyQualifiedName~AssessmentRepositoryTests"

# Run with detailed output
dotnet test tests/AcademicAssessment.Tests.Unit \
  --verbosity detailed \
  --logger "console;verbosity=detailed"
```

#### Integration Tests (Slower - Run Before Commits)

**Prerequisites:** Aspire must be running

```bash
# Run all integration tests
dotnet test tests/AcademicAssessment.Tests.Integration \
  --configuration Debug \
  --verbosity normal

# Run specific integration test
dotnet test tests/AcademicAssessment.Tests.Integration \
  --filter "FullyQualifiedName~AssessmentWorkflowTests"
```

#### UI Tests (Slowest - Run Before PR)

**Prerequisites:** All services running via Aspire

```bash
# Run UI tests
dotnet test tests/AcademicAssessment.Tests.UI \
  --configuration Debug \
  --verbosity normal
```

### Debugging

#### Debugging Web API

**VS Code:**

1. Open `.vscode/launch.json` (auto-generated)
2. Select "Launch Web API" configuration
3. Set breakpoints in your code
4. Press F5 to start debugging
5. Aspire will start automatically

**Rider/Visual Studio:**

1. Right-click `AcademicAssessment.Web` project
2. Select "Debug"
3. Set breakpoints as needed

#### Debugging Blazor Apps

**VS Code:**

1. Ensure Aspire is running
2. Open browser dev tools (F12)
3. Use browser debugger for client-side debugging
4. Use VS Code breakpoints for server-side code (Blazor Server)

#### Viewing Logs

**Aspire Dashboard Method (Recommended):**

1. Open Aspire Dashboard (usually `http://localhost:15000`)
2. Click on service name (e.g., "webapi")
3. Navigate to "Logs" tab
4. Use filter to search logs
5. Adjust log level in dashboard settings

**Console Method:**

- Logs appear in the terminal where Aspire is running
- Color-coded by service
- Less searchable than dashboard

**Structured Logs (Advanced):**

```bash
# Query logs using Azure CLI locally (if App Insights configured)
az monitor app-insights query \
  --app <app-insights-name> \
  --analytics-query "traces | where message contains 'YourSearchTerm' | take 50"
```

---

## Database Operations

### View Database Data

**Using Docker:**

```bash
# Connect to PostgreSQL
docker exec -it $(docker ps --filter "name=postgres" --format "{{.ID}}") \
  psql -U edumind_user -d edumind_dev

# Inside psql:
\dt                          # List tables
\d assessments              # Describe assessments table
SELECT * FROM assessments;  # Query data
\q                          # Quit
```

**Using GUI (TablePlus/pgAdmin):**

- Host: `localhost`
- Port: `5432` (check Aspire Dashboard for actual port)
- Database: `edumind_dev`
- Username: `edumind_user`
- Password: (check Aspire Dashboard or appsettings.Development.json)

### Reset Database

**Complete reset (deletes all data):**

```bash
# Stop Aspire
# Ctrl+C in Aspire terminal

# Remove database
dotnet ef database drop \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web \
  --force

# Recreate and apply migrations
dotnet ef database update \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web

# Reseed data (optional)
docker exec -i $(docker ps --filter "name=postgres" --format "{{.ID}}") \
  psql -U edumind_user -d edumind_dev < scripts/seed-demo-data-final.sql
```

### Create New Migration

**When you've changed domain models:**

```bash
# Generate migration
dotnet ef migrations add AddStudentProgressTracking \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web \
  --output-dir Data/Migrations

# Review generated migration file in:
# src/AcademicAssessment.Infrastructure/Data/Migrations/

# Apply migration
dotnet ef database update \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web
```

---

## Evening Shutdown Routine (1 minute)

### Step 1: Stop Aspire

```bash
# In Aspire terminal:
# Press Ctrl+C

# Wait for graceful shutdown (usually 5-10 seconds)
```

### Step 2: Commit Your Work

```bash
# Stage changes
git add .

# Commit with descriptive message
git commit -m "feat: add student progress tracking endpoint"

# Push to feature branch
git push origin feature/student-progress-tracking
```

### Step 3: (Optional) Stop Docker

**If not using Docker for other projects:**

- Right-click Docker Desktop icon in system tray
- Select "Quit Docker Desktop"

---

## Tips and Tricks

### Speed Up Startup

**Use Stub LLM Provider:**

Edit `appsettings.Development.json`:

```json
{
  "LLM": {
    "Provider": "Stub"
  }
}
```

This skips Ollama initialization (saves ~30 seconds on startup).

### Multiple Terminal Windows

**Recommended setup:**

- **Terminal 1:** Aspire (keep running)
- **Terminal 2:** Git operations, tests, migrations
- **Terminal 3:** Docker/database commands

### Watch for File Changes

**Auto-rebuild on save:**

```bash
# Watch and rebuild on changes
dotnet watch --project src/AcademicAssessment.Web/AcademicAssessment.Web.csproj
```

### Quick Health Check Script

Create `scripts/health-check.sh`:

```bash
#!/bin/bash
echo "Checking services..."
echo -n "Web API: "
curl -s http://localhost:5103/health
echo ""
echo -n "Student App: "
curl -s -o /dev/null -w "%{http_code}" http://localhost:5049
echo ""
echo -n "Dashboard: "
curl -s -o /dev/null -w "%{http_code}" http://localhost:5050
echo ""
```

Make executable and run:

```bash
chmod +x scripts/health-check.sh
./scripts/health-check.sh
```

---

## Common Daily Issues

### Issue: Port Already in Use

**Quick fix:**

```bash
# Kill all dotnet processes
pkill -9 dotnet

# Or on Windows:
taskkill /IM dotnet.exe /F

# Restart Aspire
```

### Issue: Database Connection Timeout

**Quick fix:**

```bash
# Restart PostgreSQL container
docker restart $(docker ps --filter "name=postgres" --format "{{.ID}}")

# Wait 15 seconds, then check health
curl http://localhost:5103/health
```

### Issue: Stale NuGet Cache

**Quick fix:**

```bash
# Clear cache
dotnet nuget locals all --clear

# Restore
dotnet restore EduMind.AI.sln
```

### Issue: Hot Reload Not Working

**Quick fix:**

1. Stop Aspire (`Ctrl+C`)
2. Clean build:

   ```bash
   dotnet clean EduMind.AI.sln
   dotnet build EduMind.AI.sln --configuration Debug
   ```

3. Restart Aspire

---

## Performance Checklist

Run this checklist if development feels slow:

- [ ] Docker Desktop has enough resources (8GB+ RAM recommended)
- [ ] Using Debug configuration (not Release) for faster rebuilds
- [ ] LLM provider set to "Stub" in appsettings
- [ ] No unnecessary breakpoints slowing debugger
- [ ] Antivirus excluding repository folder
- [ ] SSD has enough free space (10GB+)

---

## Success Indicators

Your daily workflow is healthy if:

- ✅ Aspire starts in under 1 minute
- ✅ Health checks pass within 30 seconds of startup
- ✅ Hot reload works for most Blazor changes
- ✅ Unit tests run in under 10 seconds
- ✅ Database operations are responsive
- ✅ No frequent crashes or container restarts

---

**Status:** ✅ Daily Workflow Established  
**Next Playbook:** [03-first-time-azure-deployment.md](./03-first-time-azure-deployment.md)
