# Integration Testing Guide

**Purpose:** Test API endpoints, database operations, and component interactions.

**Audience:** Developers testing controllers, services, and data access.

---

## 🎯 What are Integration Tests?

Integration tests verify **multiple components working together**, including:

✅ API Controllers + Services + Repositories + Database  
✅ Authentication and authorization flows  
✅ Database queries and transactions  
✅ External service integrations  
✅ Cache operations (Redis)  
✅ Multi-layer workflows

**Key Difference from Unit Tests:**

| Unit Tests | Integration Tests |
|------------|-------------------|
| Single class in isolation | Multiple components together |
| Mock all dependencies | Use real dependencies (DB, Redis) |
| Milliseconds | Seconds |
| ~500 tests | ~300 tests |

---

## 🏗️ Integration Test Architecture

### WebApplicationFactory Pattern

We use **Microsoft.AspNetCore.Mvc.Testing** to spin up the entire API in-memory:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;

public class AssessmentControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;
    
    public AssessmentControllerTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetAssessments_ReturnsOkWithList()
    {
        // Act
        var response = await client.GetAsync("/api/v1/assessment");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var assessments = await response.Content
            .ReadFromJsonAsync<List<AssessmentSummary>>();
        assessments.Should().NotBeNull();
    }
}
```

### Custom Factory for Configuration

Override factory to customize services:

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real database
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AcademicDbContext>));
            if (descriptor != null) services.Remove(descriptor);
            
            // Add test database
            services.AddDbContext<AcademicDbContext>(options =>
            {
                options.UseNpgsql(GetTestConnectionString());
            });
            
            // Override other services as needed
            services.AddScoped<IOllamaService, StubOllamaService>();
        });
        
        builder.UseEnvironment("Testing");
    }
    
    private string GetTestConnectionString() =>
        "Host=localhost;Database=edumind_test;Username=edumind_test_user;Password=edumind_test_pass";
}
```

---

## 🗄️ Database Setup

### Test Database Requirements

Integration tests require PostgreSQL running:

```bash
# Start PostgreSQL via Docker Compose
docker-compose up -d postgresql

# Verify connection
psql -h localhost -U edumind_test_user -d edumind_test -c "SELECT 1;"
```

### Database Per Test (Isolation)

**Option 1: Transactions (Fast)**

```csharp
public class RepositoryTests : IDisposable
{
    private readonly AcademicDbContext context;
    private readonly IDbContextTransaction transaction;
    
    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AcademicDbContext>()
            .UseNpgsql(TestConnectionString)
            .Options;
        
        context = new AcademicDbContext(options);
        transaction = context.Database.BeginTransaction();
    }
    
    [Fact]
    public async Task SaveAssessment_Success()
    {
        // Test uses transaction, rolled back after
        var repository = new AssessmentRepository(context);
        var assessment = CreateTestAssessment();
        
        await repository.SaveAsync(assessment);
        
        var retrieved = await repository.GetByIdAsync(assessment.Id);
        retrieved.Should().BeOfType<Result<Assessment>.Success>();
    }
    
    public void Dispose()
    {
        transaction.Rollback();  // Rollback all changes
        transaction.Dispose();
        context.Dispose();
    }
}
```

**Option 2: Database Per Test Class**

```csharp
public class AssessmentApiTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly string testDbName = $"test_db_{Guid.NewGuid()}";
    
    public async Task InitializeAsync()
    {
        // Create test database
        await CreateTestDatabaseAsync(testDbName);
        await RunMigrationsAsync(testDbName);
        await SeedTestDataAsync(testDbName);
    }
    
    public async Task DisposeAsync()
    {
        // Drop test database
        await DropTestDatabaseAsync(testDbName);
    }
}
```

### Seed Data for Tests

```csharp
public static class TestDataSeeder
{
    public static async Task SeedAsync(AcademicDbContext context)
    {
        // Clear existing data
        context.Assessments.RemoveRange(context.Assessments);
        context.Students.RemoveRange(context.Students);
        await context.SaveChangesAsync();
        
        // Add test data
        var course = new Course
        {
            Id = TestIds.CourseId,
            Name = "Test Course",
            Subject = Subject.Mathematics,
            GradeLevel = GradeLevel.HighSchool
        };
        
        var assessment = new Assessment
        {
            Id = TestIds.AssessmentId,
            CourseId = course.Id,
            Title = "Integration Test Assessment",
            // ... other properties
        };
        
        context.Courses.Add(course);
        context.Assessments.Add(assessment);
        await context.SaveChangesAsync();
    }
}

// Usage in tests
public async Task InitializeAsync()
{
    await TestDataSeeder.SeedAsync(context);
}
```

---

## 🔌 API Integration Tests

### GET Endpoint Tests

```csharp
[Fact]
public async Task GetAssessments_NoFilters_ReturnsAllAssessments()
{
    // Arrange
    await SeedTestAssessmentsAsync(count: 5);
    
    // Act
    var response = await client.GetAsync("/api/v1/assessment");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var assessments = await response.Content
        .ReadFromJsonAsync<List<AssessmentSummary>>();
    
    assessments.Should().NotBeNull();
    assessments.Should().HaveCount(5);
}

[Fact]
public async Task GetAssessment_ExistingId_ReturnsAssessment()
{
    // Arrange
    var expectedId = await SeedTestAssessmentAsync();
    
    // Act
    var response = await client.GetAsync($"/api/v1/assessment/{expectedId}");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var assessment = await response.Content
        .ReadFromJsonAsync<Assessment>();
    
    assessment.Should().NotBeNull();
    assessment!.Id.Should().Be(expectedId);
}

[Fact]
public async Task GetAssessment_NonExistentId_ReturnsNotFound()
{
    // Arrange
    var nonExistentId = Guid.NewGuid();
    
    // Act
    var response = await client.GetAsync($"/api/v1/assessment/{nonExistentId}");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

### POST Endpoint Tests

```csharp
[Fact]
public async Task CreateAssessment_ValidData_ReturnsCreated()
{
    // Arrange
    var newAssessment = new CreateAssessmentRequest
    {
        CourseId = TestIds.CourseId,
        Title = "New Assessment",
        Subject = "Mathematics",
        GradeLevel = "High School",
        Difficulty = "Intermediate",
        EstimatedDurationMinutes = 45
    };
    
    // Act
    var response = await client.PostAsJsonAsync("/api/v1/assessment", newAssessment);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    response.Headers.Location.Should().NotBeNull();
    
    var created = await response.Content.ReadFromJsonAsync<Assessment>();
    created.Should().NotBeNull();
    created!.Title.Should().Be(newAssessment.Title);
    created.CourseId.Should().Be(newAssessment.CourseId);
}

[Fact]
public async Task CreateAssessment_InvalidData_ReturnsBadRequest()
{
    // Arrange
    var invalidRequest = new CreateAssessmentRequest
    {
        // Missing required fields
        Title = ""
    };
    
    // Act
    var response = await client.PostAsJsonAsync("/api/v1/assessment", invalidRequest);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    
    var problemDetails = await response.Content
        .ReadFromJsonAsync<ValidationProblemDetails>();
    problemDetails.Should().NotBeNull();
    problemDetails!.Errors.Should().ContainKey("Title");
}
```

### PUT/PATCH Endpoint Tests

```csharp
[Fact]
public async Task UpdateAssessment_ValidData_ReturnsOk()
{
    // Arrange
    var existingId = await SeedTestAssessmentAsync();
    var updateRequest = new UpdateAssessmentRequest
    {
        Title = "Updated Title",
        Difficulty = "Advanced"
    };
    
    // Act
    var response = await client.PutAsJsonAsync(
        $"/api/v1/assessment/{existingId}", 
        updateRequest);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    
    var updated = await response.Content.ReadFromJsonAsync<Assessment>();
    updated!.Title.Should().Be(updateRequest.Title);
    updated.Difficulty.Should().Be(updateRequest.Difficulty);
}
```

### DELETE Endpoint Tests

```csharp
[Fact]
public async Task DeleteAssessment_ExistingId_ReturnsNoContent()
{
    // Arrange
    var existingId = await SeedTestAssessmentAsync();
    
    // Act
    var response = await client.DeleteAsync($"/api/v1/assessment/{existingId}");
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    
    // Verify deletion
    var getResponse = await client.GetAsync($"/api/v1/assessment/{existingId}");
    getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

---

## 🔐 Authentication Tests

### Testing Protected Endpoints

```csharp
public class AuthenticatedApiTests
{
    private readonly HttpClient client;
    
    public AuthenticatedApiTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }
    
    private void SetAuthToken(string token)
    {
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
    }
    
    [Fact]
    public async Task ProtectedEndpoint_NoAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await client.GetAsync("/api/v1/assessment/my-assessments");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsOk()
    {
        // Arrange
        var token = await GetTestTokenAsync("student@test.com");
        SetAuthToken(token);
        
        // Act
        var response = await client.GetAsync("/api/v1/assessment/my-assessments");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    
    [Fact]
    public async Task TeacherEndpoint_StudentToken_ReturnsForbidden()
    {
        // Arrange
        var token = await GetTestTokenAsync("student@test.com", role: "Student");
        SetAuthToken(token);
        
        // Act
        var response = await client.PostAsJsonAsync("/api/v1/assessment", new { });
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    
    private async Task<string> GetTestTokenAsync(string email, string role = "Student")
    {
        // Generate JWT for testing
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(TestConfiguration.JwtSecret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
```

---

## 🗃️ Repository Integration Tests

Test repository methods with real PostgreSQL:

```csharp
public class AssessmentRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly AcademicDbContext context;
    private readonly AssessmentRepository repository;
    
    public AssessmentRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AcademicDbContext>()
            .UseNpgsql(TestConnectionString)
            .Options;
        
        context = new AcademicDbContext(options);
        repository = new AssessmentRepository(context);
    }
    
    public async Task InitializeAsync()
    {
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }
    
    public async Task DisposeAsync()
    {
        await context.DisposeAsync();
    }
    
    [Fact]
    public async Task SaveAsync_NewAssessment_SavesSuccessfully()
    {
        // Arrange
        var assessment = CreateTestAssessment();
        
        // Act
        var result = await repository.SaveAsync(assessment);
        
        // Assert
        result.Should().BeOfType<Result<Assessment>.Success>();
        
        // Verify persistence
        var retrieved = await repository.GetByIdAsync(assessment.Id);
        retrieved.Should().BeOfType<Result<Assessment>.Success>();
        var success = (Result<Assessment>.Success)retrieved;
        success.Value.Title.Should().Be(assessment.Title);
    }
    
    [Fact]
    public async Task GetByCourseIdAsync_MultipleAssessments_ReturnsAll()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        await SeedAssessmentsForCourseAsync(courseId, count: 3);
        
        // Act
        var result = await repository.GetByCourseIdAsync(courseId);
        
        // Assert
        result.Should().BeOfType<Result<IReadOnlyList<Assessment>>.Success>();
        var success = (Result<IReadOnlyList<Assessment>>.Success)result;
        success.Value.Should().HaveCount(3);
        success.Value.Should().OnlyContain(a => a.CourseId == courseId);
    }
    
    [Fact]
    public async Task DeleteAsync_ExistingAssessment_RemovesFromDatabase()
    {
        // Arrange
        var assessment = await SeedTestAssessmentAsync();
        
        // Act
        var result = await repository.DeleteAsync(assessment.Id);
        
        // Assert
        result.Should().BeOfType<Result<bool>.Success>();
        
        // Verify deletion
        var retrieved = await repository.GetByIdAsync(assessment.Id);
        retrieved.Should().BeOfType<Result<Assessment>.Failure>();
    }
}
```

---

## 💾 Redis Integration Tests

Test caching operations:

```csharp
public class CacheIntegrationTests
{
    private readonly IConnectionMultiplexer redis;
    private readonly IDatabase cache;
    
    public CacheIntegrationTests()
    {
        redis = ConnectionMultiplexer.Connect("localhost:6379");
        cache = redis.GetDatabase();
    }
    
    [Fact]
    public async Task CacheAssessment_ThenRetrieve_Success()
    {
        // Arrange
        var assessment = CreateTestAssessment();
        var key = $"assessment:{assessment.Id}";
        var json = JsonSerializer.Serialize(assessment);
        
        // Act - Cache
        await cache.StringSetAsync(key, json, TimeSpan.FromMinutes(10));
        
        // Act - Retrieve
        var cached = await cache.StringGetAsync(key);
        
        // Assert
        cached.HasValue.Should().BeTrue();
        var deserialized = JsonSerializer.Deserialize<Assessment>(cached!);
        deserialized.Should().BeEquivalentTo(assessment);
    }
    
    [Fact]
    public async Task CacheExpiration_AfterTTL_KeyNotFound()
    {
        // Arrange
        var key = "expiring-key";
        await cache.StringSetAsync(key, "value", TimeSpan.FromSeconds(1));
        
        // Act
        await Task.Delay(1100);  // Wait for expiration
        var result = await cache.StringGetAsync(key);
        
        // Assert
        result.HasValue.Should().BeFalse();
    }
}
```

---

## 🧪 Testing Best Practices

### 1. Test Data Isolation

Each test should be independent:

```csharp
[Fact]
public async Task Test1_CreatesData()
{
    // Create test-specific data
    var assessment = await SeedTestAssessmentAsync();
    // Test uses this data
    // Data cleaned up after test
}

[Fact]
public async Task Test2_AlsoCreatesData()
{
    // Creates its own data, independent of Test1
    var assessment = await SeedTestAssessmentAsync();
}
```

### 2. Use Test Constants

```csharp
public static class TestIds
{
    public static readonly Guid CourseId = new("10000000-0000-0000-0000-000000000001");
    public static readonly Guid AssessmentId = new("20000000-0000-0000-0000-000000000002");
    public static readonly Guid StudentId = new("30000000-0000-0000-0000-000000000003");
}

public static class TestUsers
{
    public const string StudentEmail = "student@test.com";
    public const string TeacherEmail = "teacher@test.com";
}
```

### 3. Helper Methods

```csharp
public class ApiTestBase
{
    protected async Task<Guid> SeedTestAssessmentAsync(
        string title = "Test Assessment",
        string difficulty = "Intermediate")
    {
        var assessment = new Assessment
        {
            Id = Guid.NewGuid(),
            Title = title,
            Difficulty = difficulty,
            // ... other required properties
        };
        
        await context.Assessments.AddAsync(assessment);
        await context.SaveChangesAsync();
        
        return assessment.Id;
    }
    
    protected async Task<List<Guid>> SeedTestAssessmentsAsync(int count)
    {
        var ids = new List<Guid>();
        for (int i = 0; i < count; i++)
        {
            ids.Add(await SeedTestAssessmentAsync($"Assessment {i + 1}"));
        }
        return ids;
    }
}
```

---

## 🏃 Running Integration Tests

### Prerequisites

```bash
# 1. Start services
docker-compose up -d

# 2. Wait for PostgreSQL to be ready
docker-compose logs postgresql | grep "ready to accept connections"

# 3. Run integration tests
dotnet test tests/AcademicAssessment.Tests.Integration/
```

### Specific Tests

```bash
# Single test class
dotnet test --filter "FullyQualifiedName~AssessmentControllerTests"

# Authenticated tests only
dotnet test --filter "FullyQualifiedName~Authenticated"

# Repository tests
dotnet test --filter "FullyQualifiedName~RepositoryTests"
```

### CI/CD Execution

Integration tests run in GitHub Actions with services:

```yaml
jobs:
  integration-tests:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:17
        env:
          POSTGRES_DB: edumind_test
          POSTGRES_USER: edumind_test_user
          POSTGRES_PASSWORD: edumind_test_pass
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
      redis:
        image: redis:7
        options: >-
          --health-cmd "redis-cli ping"
    
    steps:
      - name: Run integration tests
        run: dotnet test tests/AcademicAssessment.Tests.Integration/
```

---

## 🐛 Troubleshooting

### Connection Failures

**Problem:** `Npgsql.NpgsqlException: Connection refused`

```bash
# Check PostgreSQL is running
docker-compose ps

# Check logs
docker-compose logs postgresql

# Restart if needed
docker-compose restart postgresql
```

### Test Data Conflicts

**Problem:** Tests fail due to existing data

```bash
# Reset test database
docker-compose down -v
docker-compose up -d

# Or programmatically in test setup:
await context.Database.EnsureDeletedAsync();
await context.Database.EnsureCreatedAsync();
```

### Slow Integration Tests

**Problem:** Tests take too long (>60s)

**Solutions:**

1. Use transactions for rollback (faster than recreating DB)
2. Seed only necessary data
3. Run tests in parallel (xUnit does this by default)
4. Use connection pooling

---

**Last Updated:** 2025-10-25  
**Related:** [Unit Testing](./03-unit-testing.md) | [E2E Testing](./05-e2e-testing.md) | [Troubleshooting](./12-troubleshooting.md)
