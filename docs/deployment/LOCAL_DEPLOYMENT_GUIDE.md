# Local Deployment Testing Guide

**Date:** October 19, 2025  
**Purpose:** Test complete deployment of EduMind.AI locally using .NET Aspire

## 🎯 Deployment Readiness Assessment

### Current Test Coverage Status

| Component | Line Coverage | Branch Coverage | Status |
|-----------|---------------|-----------------|--------|
| **AssessmentController** | 100% | 85.71% | ✅ **Production Ready** |
| StudentAnalyticsController | 0% | 0% | ⚠️ Not Tested |
| OrchestrationController | 0% | 100%* | ⚠️ Investigate |

**Overall Integration Coverage:** 19.03% line, 6.80% branch

### Recommendation: **PROCEED WITH DEPLOYMENT TESTING** ✅

**Rationale:**

1. ✅ Core assessment functionality is well-tested (85.71% branch coverage)
2. ✅ All 57 integration tests passing
3. ✅ Zero build errors
4. ⚠️ Analytics controller can be tested post-deployment
5. 🎯 Best practice: Test deployment early and often

## 🚀 Local Deployment Options

### Option 1: .NET Aspire (Recommended)

**Pros:**

- ✅ Complete stack orchestration (Web API, Dashboard, Student App)
- ✅ Built-in service discovery
- ✅ Automatic dependency injection (PostgreSQL, Redis, OLLAMA)
- ✅ Dashboard for monitoring
- ✅ Development-optimized

**Cons:**

- ⚠️ Requires Docker running
- ⚠️ More resource-intensive

### Option 2: Individual Projects

**Pros:**

- ✅ Lightweight
- ✅ Easier debugging
- ✅ Faster startup

**Cons:**

- ❌ Manual dependency setup (DB, Redis)
- ❌ No service orchestration
- ❌ Not representative of production

### Option 3: Docker Compose (If available)

**Pros:**

- ✅ Production-like environment
- ✅ Complete isolation
- ✅ Easy cleanup

**Cons:**

- ❌ No Docker Compose file currently exists
- ⚠️ Would need to create

## 📋 Pre-Deployment Checklist

### Environment Setup

- [x] .NET 9.0 SDK installed
- [x] Docker available and running
- [ ] PostgreSQL connection string configured
- [ ] Redis connection configured (optional for local)
- [ ] Azure AD / Auth0 credentials configured
- [ ] OLLAMA for AI features (optional)

### Code Quality

- [x] All builds successful
- [x] Integration tests passing (57/57)
- [x] Core functionality tested (>80% coverage)
- [ ] Unit tests reviewed
- [ ] No critical warnings

### Configuration

- [ ] `appsettings.json` reviewed
- [ ] `appsettings.Development.json` configured
- [ ] Connection strings set
- [ ] Authentication providers configured
- [ ] CORS settings verified

## 🧪 Local Deployment Test Plan

### Step 1: Start Docker

```bash
# Verify Docker is running
docker ps

# If not running, start Docker daemon
sudo systemctl start docker  # Linux
# or start Docker Desktop on Windows/Mac
```

### Step 2: Run .NET Aspire AppHost

```bash
cd /workspaces/edumind-ai

# Option A: Using Visual Studio Code
# Open EduMind.AppHost/Program.cs and press F5

# Option B: Using CLI
dotnet run --project src/EduMind.AppHost/EduMind.AppHost.csproj
```

### Step 3: Verify Services

Once Aspire starts, it will:

1. Start PostgreSQL container
2. Start Redis container
3. Start OLLAMA container (optional)
4. Launch Web API (ports 5000/5001)
5. Launch Dashboard
6. Launch Student App
7. Open Aspire Dashboard in browser

**Expected URLs:**

- Aspire Dashboard: `http://localhost:15000` (or similar)
- Web API: `https://localhost:5001/swagger`
- Dashboard: Check Aspire dashboard for port
- Student App: Check Aspire dashboard for port

### Step 4: Health Checks

Verify each service:

```bash
# Check Web API health
curl http://localhost:5000/health

# Check Web API Swagger
curl http://localhost:5000/swagger/index.html

# Check authentication endpoint
curl http://localhost:5000/api/v1.0/health
```

### Step 5: Database Migration

```bash
# Apply migrations to create database schema
dotnet ef database update --project src/AcademicAssessment.Infrastructure

# Or use the task
# Run task: ef-database-update
```

### Step 6: Seed Demo Data (Optional)

```bash
# Run the demo data seeding script
./scripts/seed-demo-data.sh

# Or run SQL directly
psql -U postgres -d edumind -f scripts/seed-demo-data-v2.sql
```

### Step 7: Smoke Tests

#### Test 1: Get Assessments (Unauthenticated - should fail)

```bash
curl -X GET http://localhost:5000/api/v1.0/Assessment \
  -H "Content-Type: application/json"
# Expected: 401 Unauthorized
```

#### Test 2: Get Assessments (Authenticated)

```bash
# First, get a token from your auth provider
# Then use it:
curl -X GET http://localhost:5000/api/v1.0/Assessment \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json"
# Expected: 200 OK with assessment list
```

#### Test 3: Create Assessment Session

```bash
curl -X GET "http://localhost:5000/api/v1.0/Assessment/{assessment-id}/session" \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json"
# Expected: 200 OK with session data
```

#### Test 4: Analytics Endpoint

```bash
curl -X GET http://localhost:5000/api/v1.0/StudentAnalytics/recent-sessions \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
# Expected: 200 OK with session list
```

### Step 8: UI Testing

1. **Student App:**
   - Navigate to Student App URL from Aspire Dashboard
   - Login with test credentials
   - Browse available assessments
   - Start an assessment
   - Answer questions
   - Submit assessment
   - View results

2. **Dashboard:**
   - Navigate to Dashboard URL
   - Login with admin credentials
   - View student analytics
   - Review assessment results
   - Check orchestration metrics

## 🔧 Troubleshooting

### Issue: Docker containers won't start

**Solution:**

```bash
# Check Docker daemon
docker ps

# Restart Docker
sudo systemctl restart docker

# Check available resources
docker system df
```

### Issue: Database connection fails

**Solution:**

```bash
# Check PostgreSQL container
docker ps | grep postgres

# Check connection string in appsettings.Development.json
# Default: Server=localhost;Database=edumind;User Id=postgres;Password=postgres;
```

### Issue: Aspire dashboard shows errors

**Solution:**

1. Check Aspire Dashboard logs
2. Look for red/failed services
3. Click on service to see detailed logs
4. Check port conflicts: `netstat -tulpn | grep LISTEN`

### Issue: Authentication fails

**Solution:**

1. Check `appsettings.Development.json` auth configuration
2. Verify Azure AD / Auth0 credentials
3. Check CORS settings
4. Review JWT token configuration

### Issue: OLLAMA not available

**Solution:**

```bash
# OLLAMA is optional for basic functionality
# If needed, pull a model:
docker exec -it ollama ollama pull llama2

# Or disable AI features temporarily
# Comment out OLLAMA references in Program.cs
```

## 📊 What to Test

### Critical User Journeys

#### Journey 1: Student Assessment Flow

1. Login as student
2. Browse assessments
3. Select assessment (Algebra, Chemistry, etc.)
4. Start assessment session
5. Answer multiple choice questions
6. Answer free-response questions
7. Use review flags
8. Save progress
9. Submit assessment
10. View results
11. Review recommendations

#### Journey 2: Admin Dashboard Flow

1. Login as admin/teacher
2. View student list
3. Review assessment results
4. Analyze performance metrics
5. View subject breakdowns
6. Check recent sessions
7. Review analytics

#### Journey 3: API Integration Flow

1. Get authentication token
2. List available assessments
3. Create assessment session
4. Save answers
5. Submit session
6. Retrieve results
7. Get analytics

### Performance Testing

```bash
# Simple load test with curl
for i in {1..10}; do
  curl -X GET http://localhost:5000/api/v1.0/Assessment \
    -H "Authorization: Bearer $TOKEN" &
done
wait

# Or use Apache Bench
ab -n 100 -c 10 -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/api/v1.0/Assessment
```

## 🎯 Success Criteria

Deployment is successful if:

- ✅ All Aspire services start without errors
- ✅ PostgreSQL database is created and migrated
- ✅ Web API responds to health checks
- ✅ Swagger UI is accessible
- ✅ Authentication works (can get tokens)
- ✅ Student can complete full assessment journey
- ✅ Dashboard shows analytics
- ✅ No critical errors in logs
- ✅ Response times < 500ms for API calls
- ✅ UI loads without console errors

## 📝 Deployment Test Results Template

```markdown
# Local Deployment Test - [Date]

## Environment
- OS: [Linux/Windows/Mac]
- .NET Version: 9.0.306
- Docker Version: 28.4.0

## Services Started
- [ ] PostgreSQL
- [ ] Redis
- [ ] OLLAMA
- [ ] Web API
- [ ] Dashboard
- [ ] Student App

## Smoke Tests
- [ ] Health endpoint responds
- [ ] Swagger accessible
- [ ] Authentication works
- [ ] Get assessments returns data
- [ ] Create session works
- [ ] Submit assessment works
- [ ] Get results works
- [ ] Analytics endpoint works

## User Journeys
- [ ] Student can login
- [ ] Student can browse assessments
- [ ] Student can complete assessment
- [ ] Student can view results
- [ ] Admin can view dashboard
- [ ] Admin can see analytics

## Performance
- API response time: [X]ms
- UI load time: [X]s
- Database queries: [fast/slow]

## Issues Found
1. [Issue description]
2. [Issue description]

## Conclusion
- [ ] Ready for Azure deployment
- [ ] Needs fixes before deployment
```

## 🚦 Decision Matrix

### Deploy Now If

- ✅ Core functionality works (AssessmentController)
- ✅ Critical user journey completes
- ✅ No blocking bugs in local deployment
- ✅ Authentication configured

### Add More Tests First If

- ❌ Critical bugs found in local deployment
- ❌ Authentication not working
- ❌ Database migrations failing
- ❌ Analytics features are critical for MVP

## 📌 Recommended Next Steps

### Immediate (Today)

1. ✅ Run local Aspire deployment
2. ✅ Complete smoke tests
3. ✅ Test critical user journey (student assessment)
4. ✅ Document any issues found

### Short Term (This Week)

1. Fix any issues from local deployment
2. Add StudentAnalyticsController tests (if analytics is critical)
3. Create Azure deployment scripts
4. Set up CI/CD pipeline testing

### Medium Term (Next Sprint)

1. Deploy to Azure Dev environment
2. Run integration tests against Azure
3. Performance testing
4. Security audit
5. User acceptance testing

## 🎓 Conclusion

**Recommendation: Proceed with local deployment testing NOW.**

You have solid test coverage on the core functionality (AssessmentController @ 85.71% branch coverage). The best way to find integration issues is to run the full stack locally.

After successful local deployment:

1. Document any issues
2. Fix critical bugs
3. Add tests for found issues
4. Proceed to Azure deployment

This iterative approach (test → deploy → fix → test) is more effective than trying to achieve 100% test coverage before ever deploying.

**Next Command to Run:**

```bash
dotnet run --project src/EduMind.AppHost/EduMind.AppHost.csproj
```

Then watch the magic happen! 🚀
