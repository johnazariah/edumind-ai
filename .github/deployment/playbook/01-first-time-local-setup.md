# Playbook: First-Time Local Setup

**Scenario:** New developer setting up EduMind.AI for the first time on their local machine  
**Time Required:** 30-45 minutes  
**Difficulty:** Beginner

---

## Prerequisites

Before you begin, ensure you have:

- [ ] Git installed
- [ ] Admin/sudo access on your machine
- [ ] Stable internet connection
- [ ] At least 10GB free disk space

---

## Step 1: Install Required Software (15-20 minutes)

### 1.1 Install .NET 9.0 SDK

**Windows:**

1. Download from <https://dotnet.microsoft.com/download/dotnet/9.0>
2. Run the installer
3. Follow the installation wizard

**macOS:**

```bash
brew install dotnet@9
```

**Linux (Ubuntu/Debian):**

```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0
```

**Verify Installation:**

```bash
dotnet --version
# Expected output: 9.0.x
```

### 1.2 Install Docker Desktop

**All Platforms:**

1. Download from <https://www.docker.com/products/docker-desktop>
2. Install and start Docker Desktop
3. Accept the service agreement

**Verify Installation:**

```bash
docker --version
# Expected output: Docker version 24.x.x or later

docker ps
# Should show empty list or running containers (no error)
```

### 1.3 Install Git (if not already installed)

**Windows:**

- Download from <https://git-scm.com/download/win>

**macOS:**

```bash
brew install git
```

**Linux:**

```bash
sudo apt-get install git
```

**Verify:**

```bash
git --version
```

---

## Step 2: Clone the Repository (2 minutes)

```bash
# Navigate to your workspace directory
cd ~/workspace  # or C:\Users\<YourName>\workspace on Windows

# Clone the repository
git clone https://github.com/johnazariah/edumind-ai.git

# Navigate into the repository
cd edumind-ai

# Verify you're on the main branch
git branch
# Should show: * main
```

---

## Step 3: Restore Dependencies (5 minutes)

```bash
# Restore NuGet packages
dotnet restore EduMind.AI.sln

# This will download all required packages
# Expected output: Restore succeeded
```

**If you encounter errors:**

- Check internet connection
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Try restore again

---

## Step 4: Build the Solution (3 minutes)

```bash
# Build in Release configuration
dotnet build EduMind.AI.sln --configuration Release --no-restore

# Expected output: Build succeeded
```

**Verify:**

- Look for "Build succeeded" message
- Note: Warnings are okay, but no errors should appear

---

## Step 5: Set Up Local Database (5 minutes)

The database will be automatically started by Aspire, but you need to apply migrations.

### 5.1 Start Aspire (First Time)

```bash
# Run Aspire AppHost
dotnet run --project src/EduMind.AppHost/EduMind.AppHost.csproj
```

**What will happen:**

1. Aspire Dashboard opens in browser automatically
2. PostgreSQL container starts
3. Redis container starts
4. Web API, Dashboard, and Student App start
5. You'll see URLs in the console

**Keep this terminal running!**

### 5.2 Apply Database Migrations

**Open a NEW terminal** (keep Aspire running in the first one):

```bash
# Navigate to repository root
cd ~/workspace/edumind-ai  # Adjust path

# Apply migrations
dotnet ef database update \
  --project src/AcademicAssessment.Infrastructure \
  --startup-project src/AcademicAssessment.Web
```

**Expected output:**

```
Applying migration '20251015123456_InitialCreate'
Applying migration '20251016234567_AddAssessments'
...
Done.
```

### 5.3 Seed Demo Data (Optional)

```bash
# Connect to PostgreSQL and run seed script
docker exec -i $(docker ps --filter "name=postgres" --format "{{.ID}}") \
  psql -U edumind_user -d edumind_dev < scripts/seed-demo-data-final.sql
```

**Alternative (if above doesn't work):**

```bash
# Find PostgreSQL container
docker ps | grep postgres

# Copy the container ID, then:
docker exec -it <container-id> psql -U edumind_user -d edumind_dev

# Inside psql:
\i /path/to/scripts/seed-demo-data-final.sql
```

---

## Step 6: Verify Everything Works (5 minutes)

### 6.1 Check Aspire Dashboard

Open the Aspire Dashboard URL shown in your first terminal (usually `http://localhost:15000` or similar).

**You should see:**

- ✅ PostgreSQL: Running
- ✅ Redis: Running
- ✅ webapi: Running
- ✅ studentapp: Running
- ✅ dashboard: Running

### 6.2 Test Web API Health

```bash
# Test health endpoint
curl http://localhost:5103/health

# Expected response: "Healthy"
```

If you get "Unhealthy":

- Check Aspire Dashboard for error logs
- Verify PostgreSQL and Redis are running
- Check Web API logs in Aspire Dashboard

### 6.3 Test Student App

Open browser to: `http://localhost:5049`

**You should see:**

- Student App home page loads
- No error messages
- Assessment dashboard visible

### 6.4 Test Dashboard

Open browser to: `http://localhost:5050`

**You should see:**

- Admin/Teacher dashboard loads
- No error messages

---

## Step 7: Run Tests (5 minutes)

```bash
# Run unit tests
dotnet test tests/AcademicAssessment.Tests.Unit \
  --configuration Release \
  --verbosity normal

# Expected output: All tests passed
```

---

## Success Criteria

You've successfully set up the local environment if:

- [x] All software installed (dotnet, docker, git)
- [x] Repository cloned
- [x] Solution builds without errors
- [x] Database migrations applied
- [x] Aspire starts all services
- [x] Health check returns "Healthy"
- [x] Web UI loads without errors
- [x] Unit tests pass

---

## Common Issues and Solutions

### Issue: "Docker is not available"

**Symptom:** Aspire fails to start with Docker error

**Solution:**

1. Ensure Docker Desktop is running (check system tray icon)
2. Open Docker Desktop GUI
3. Wait for it to fully start
4. Try running Aspire again

### Issue: "Port already in use"

**Symptom:** Error message about port 5103, 5049, or 5050

**Solution:**

```bash
# Find process using the port (example for 5103)
# Windows:
netstat -ano | findstr :5103

# macOS/Linux:
lsof -i :5103

# Kill the process
# Windows:
taskkill /PID <process-id> /F

# macOS/Linux:
kill -9 <process-id>
```

### Issue: Database migration fails

**Symptom:** "Unable to connect to database" error

**Solution:**

1. Verify PostgreSQL container is running: `docker ps | grep postgres`
2. Check PostgreSQL logs in Aspire Dashboard
3. Verify connection string in `appsettings.Development.json`
4. Try restarting Aspire

### Issue: "Unhealthy" health check

**Symptom:** `/health` endpoint returns "Unhealthy"

**Solution:**

1. Check Aspire Dashboard for service status
2. Review logs for webapi in Aspire Dashboard
3. Look for connection errors to PostgreSQL or Redis
4. Verify both containers are running
5. Check firewall isn't blocking connections

---

## Next Steps

Now that your local environment is set up:

1. **Read the Code:** Start with `src/EduMind.AppHost/Program.cs`
2. **Explore the API:** Open `https://localhost:5103/swagger`
3. **Review Architecture:** See `.github/adr/` for architectural decisions
4. **Try the Student App:** Navigate through an assessment
5. **Run Integration Tests:** See `tests/AcademicAssessment.Tests.Integration`

---

## Getting Help

If you encounter issues not covered here:

1. Check existing GitHub Issues: <https://github.com/johnazariah/edumind-ai/issues>
2. Review troubleshooting guide: `.github/deployment/reference.md#troubleshooting`
3. Check deployment docs: `docs/deployment/`
4. Ask in team chat/discussion

---

## Configuration Tips

### Speed Up Startup

Add to `appsettings.Development.json`:

```json
{
  "LLM": {
    "Provider": "Stub"  // Faster startup, no Ollama required
  }
}
```

### Disable Specific Services

In `src/EduMind.AppHost/Program.cs`, comment out services you don't need:

```csharp
// var dashboard = builder.AddProject<Projects.AcademicAssessment_Dashboard>("dashboard")
//     .WithExternalHttpEndpoints()
//     ...
```

---

**Status:** ✅ Setup Complete  
**Next Playbook:** [02-daily-development-workflow.md](./02-daily-development-workflow.md)
