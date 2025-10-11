# Student Privacy and Data Security Architecture

## Guiding Principles

### Core Privacy Principle
**"Student data is sacred and must be protected at all costs."**

EduMind.AI is built on the foundation that **every student's educational data is highly sensitive** and must be treated with the highest level of security and privacy. We implement a **defense-in-depth** strategy with multiple layers of protection to ensure that inadvertent data leaking is impossible.

### Legal and Ethical Framework

1. **FERPA Compliance** (Family Educational Rights and Privacy Act)
   - Student data is confidential educational records
   - Access strictly limited to legitimate educational interest
   - Parental consent required for data sharing
   - Right to inspect and correct records

2. **COPPA Compliance** (Children's Online Privacy Protection Act)
   - Enhanced protections for students under 13
   - Parental notification and consent
   - Limited data collection
   - No third-party advertising

3. **GDPR Principles** (for international deployments)
   - Right to be forgotten
   - Data portability
   - Privacy by design
   - Data minimization

4. **Ethical Data Stewardship**
   - Collect only what's necessary
   - Use only for educational purposes
   - Never sell or share student data
   - Transparent data practices

---

## Multi-Tenant Isolation Strategy

### Physical Data Partitioning (Recommended Approach)

To ensure **absolute data isolation** between schools and prevent any possibility of inadvertent data leaking, we implement **physical database partitioning** where each school has its own dedicated database instance.

#### Architecture: School-Level Database Isolation

```
┌─────────────────────────────────────────────────────────────────────┐
│                    SYSTEM (White-Box SaaS)                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │              PostgreSQL Cluster (Primary)                    │ │
│  │                                                              │ │
│  │  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐│ │
│  │  │   Database:    │  │   Database:    │  │   Database:    ││ │
│  │  │   school_001   │  │   school_002   │  │   school_003   ││ │
│  │  │                │  │                │  │                ││ │
│  │  │ • Students     │  │ • Students     │  │ • Students     ││ │
│  │  │ • Assessments  │  │ • Assessments  │  │ • Assessments  ││ │
│  │  │ • Responses    │  │ • Responses    │  │ • Responses    ││ │
│  │  │ • Progress     │  │ • Progress     │  │ • Progress     ││ │
│  │  │                │  │                │  │                ││ │
│  │  │ ISOLATED ✓     │  │ ISOLATED ✓     │  │ ISOLATED ✓     ││ │
│  │  └────────────────┘  └────────────────┘  └────────────────┘│ │
│  │                                                              │ │
│  │  Physical network-level isolation between school databases  │ │
│  │  No cross-database queries possible                         │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │              Shared System Database                          │ │
│  │                                                              │ │
│  │  • School metadata (name, contact, subscription)            │ │
│  │  • User authentication records (emails, roles)              │ │
│  │  • System configuration                                     │ │
│  │  • Audit logs (access tracking)                             │ │
│  │                                                              │ │
│  │  NO STUDENT DATA - Only organizational metadata             │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │              Shared Content Database                         │ │
│  │                                                              │ │
│  │  • Course curriculum (cross-school)                         │ │
│  │  • Question banks (no student responses)                    │ │
│  │  • Learning objectives                                      │ │
│  │  • Assessment templates                                     │ │
│  │                                                              │ │
│  │  NO STUDENT DATA - Only educational content                 │ │
│  └──────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

#### Benefits of Physical Partitioning

1. **Absolute Isolation**
   - Impossible to accidentally query wrong school's data
   - No shared tables between schools
   - Database-level security enforces boundaries
   - No risk of SQL injection across tenants

2. **Regulatory Compliance**
   - Each school's data is completely separate
   - Easy to provide data export for single school
   - Simple to implement "right to be forgotten"
   - Clear data ownership and custody

3. **Performance Isolation**
   - One school's heavy usage doesn't impact others
   - Independent scaling per school
   - School-specific backup and restore
   - Optimized indexes per school

4. **Security Audit Trail**
   - Per-school connection strings
   - Database-level access logs
   - Clear audit trail of who accessed which school
   - Easy to detect unauthorized access attempts

5. **Disaster Recovery**
   - School-specific backup schedules
   - Granular restore capabilities
   - Fail-over per school
   - No risk of cross-contamination during recovery

---

## Implementation: School Onboarding Process

### Phase 1: School Registration (Business Admin)

```csharp
/// <summary>
/// School onboarding is intentionally a manual, careful process
/// to ensure proper data isolation is established
/// </summary>
public record OnboardSchoolRequest
{
    public required string SchoolName { get; init; }
    public required string District { get; init; }
    public required string AdminEmail { get; init; }
    public required string AdminName { get; init; }
    public required SubscriptionTier Tier { get; init; }
    public int EstimatedStudentCount { get; init; }
    public int EstimatedTeacherCount { get; init; }
}

public interface ISchoolOnboardingService
{
    /// <summary>
    /// Step 1: Create school metadata and generate unique identifier
    /// </summary>
    Task<Result<SchoolId>> CreateSchoolMetadataAsync(
        OnboardSchoolRequest request,
        CancellationToken ct = default);
    
    /// <summary>
    /// Step 2: Provision dedicated database for the school
    /// This is a potentially long-running operation
    /// </summary>
    Task<Result<DatabaseConnectionString>> ProvisionSchoolDatabaseAsync(
        SchoolId schoolId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Step 3: Run database migrations on the new school database
    /// </summary>
    Task<Result<Unit>> InitializeSchoolSchemaAsync(
        SchoolId schoolId,
        DatabaseConnectionString connectionString,
        CancellationToken ct = default);
    
    /// <summary>
    /// Step 4: Create initial admin user account
    /// </summary>
    Task<Result<UserId>> CreateSchoolAdminUserAsync(
        SchoolId schoolId,
        string email,
        string name,
        CancellationToken ct = default);
    
    /// <summary>
    /// Step 5: Verify isolation and run security checks
    /// </summary>
    Task<Result<SecurityAuditReport>> VerifySchoolIsolationAsync(
        SchoolId schoolId,
        CancellationToken ct = default);
}
```

### Phase 2: Database Provisioning

```csharp
public class SchoolDatabaseProvisioner : ISchoolDatabaseProvisioner
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SchoolDatabaseProvisioner> _logger;
    
    public async Task<Result<DatabaseConnectionString>> ProvisionDatabaseAsync(
        SchoolId schoolId,
        CancellationToken ct)
    {
        try
        {
            // 1. Generate unique database name
            var dbName = GenerateDatabaseName(schoolId);
            
            // 2. Create database with isolation
            await CreateIsolatedDatabaseAsync(dbName, ct);
            
            // 3. Set up database-level security
            await ConfigureDatabaseSecurityAsync(dbName, schoolId, ct);
            
            // 4. Create dedicated connection string
            var connectionString = BuildSecureConnectionString(dbName, schoolId);
            
            // 5. Store connection string securely (Azure Key Vault)
            await StoreConnectionStringAsync(schoolId, connectionString, ct);
            
            // 6. Verify isolation
            await VerifyIsolationAsync(dbName, ct);
            
            _logger.LogInformation(
                "Successfully provisioned isolated database for school {SchoolId}: {DatabaseName}",
                schoolId, dbName);
            
            return connectionString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to provision database for school {SchoolId}", schoolId);
            return new Error("DB_PROVISION_FAILED", 
                "Failed to provision school database", ex);
        }
    }
    
    private static string GenerateDatabaseName(SchoolId schoolId) =>
        $"edumind_school_{schoolId.Value:N}";
    
    private async Task CreateIsolatedDatabaseAsync(string dbName, CancellationToken ct)
    {
        var masterConnectionString = _configuration["Database:MasterConnectionString"];
        
        await using var connection = new NpgsqlConnection(masterConnectionString);
        await connection.OpenAsync(ct);
        
        await using var command = connection.CreateCommand();
        command.CommandText = $@"
            CREATE DATABASE {dbName}
            WITH 
                OWNER = edumind_admin
                ENCODING = 'UTF8'
                LC_COLLATE = 'en_US.utf8'
                LC_CTYPE = 'en_US.utf8'
                TABLESPACE = pg_default
                CONNECTION LIMIT = 100;
            
            -- Revoke all public access
            REVOKE ALL ON DATABASE {dbName} FROM PUBLIC;
            
            -- Enable row-level security by default
            ALTER DATABASE {dbName} SET row_security = on;
        ";
        
        await command.ExecuteNonQueryAsync(ct);
    }
    
    private async Task ConfigureDatabaseSecurityAsync(
        string dbName, 
        SchoolId schoolId, 
        CancellationToken ct)
    {
        // Create dedicated database role for this school
        var roleName = $"school_{schoolId.Value:N}_role";
        
        await using var connection = new NpgsqlConnection(
            BuildConnectionString(dbName));
        await connection.OpenAsync(ct);
        
        await using var command = connection.CreateCommand();
        command.CommandText = $@"
            -- Create school-specific role
            CREATE ROLE {roleName} WITH
                LOGIN
                NOSUPERUSER
                NOCREATEDB
                NOCREATEROLE
                NOREPLICATION
                CONNECTION LIMIT 50;
            
            -- Grant minimal required permissions
            GRANT CONNECT ON DATABASE {dbName} TO {roleName};
            GRANT USAGE ON SCHEMA public TO {roleName};
            
            -- Enable audit logging for this database
            ALTER DATABASE {dbName} SET log_statement = 'all';
            ALTER DATABASE {dbName} SET log_connections = on;
            ALTER DATABASE {dbName} SET log_disconnections = on;
        ";
        
        await command.ExecuteNonQueryAsync(ct);
    }
}
```

### Phase 3: Connection String Resolution

```csharp
/// <summary>
/// Resolves the correct database connection string based on tenant context
/// </summary>
public interface ISchoolDatabaseResolver
{
    Task<Result<string>> GetConnectionStringAsync(
        SchoolId schoolId, 
        CancellationToken ct = default);
}

public class SchoolDatabaseResolver : ISchoolDatabaseResolver
{
    private readonly IDistributedCache _cache;
    private readonly IKeyVaultClient _keyVault;
    private readonly ILogger<SchoolDatabaseResolver> _logger;
    
    public async Task<Result<string>> GetConnectionStringAsync(
        SchoolId schoolId, 
        CancellationToken ct)
    {
        // Check cache first
        var cacheKey = $"school_connection_{schoolId.Value}";
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cached))
        {
            return DecryptConnectionString(cached);
        }
        
        // Retrieve from Key Vault
        var secretName = $"school-{schoolId.Value:N}-connection";
        var secret = await _keyVault.GetSecretAsync(secretName, ct);
        
        if (secret == null)
        {
            _logger.LogError(
                "Connection string not found for school {SchoolId}", schoolId);
            return new Error("CONNECTION_NOT_FOUND", 
                "School database connection not configured");
        }
        
        // Cache for 1 hour
        await _cache.SetStringAsync(
            cacheKey, 
            secret.Value, 
            new DistributedCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) 
            }, 
            ct);
        
        return DecryptConnectionString(secret.Value);
    }
}
```

### Phase 4: Dynamic DbContext per School

```csharp
/// <summary>
/// Factory that creates DbContext instances connected to the correct school database
/// </summary>
public class SchoolDbContextFactory : IDbContextFactory<AcademicContext>
{
    private readonly ISchoolDatabaseResolver _resolver;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<SchoolDbContextFactory> _logger;
    
    public async Task<AcademicContext> CreateDbContextAsync(
        CancellationToken ct = default)
    {
        if (_tenantContext.SchoolId == null)
        {
            throw new InvalidOperationException(
                "Cannot create school database context without SchoolId");
        }
        
        // Resolve connection string for this specific school
        var connectionStringResult = await _resolver.GetConnectionStringAsync(
            _tenantContext.SchoolId.Value, ct);
        
        if (connectionStringResult is Result<string>.Failure failure)
        {
            _logger.LogError(
                "Failed to resolve connection string for school {SchoolId}: {Error}",
                _tenantContext.SchoolId, failure.Error.Message);
            throw new InvalidOperationException(
                "Failed to resolve school database connection");
        }
        
        var connectionString = ((Result<string>.Success)connectionStringResult).Value;
        
        var optionsBuilder = new DbContextOptionsBuilder<AcademicContext>();
        optionsBuilder.UseNpgsql(connectionString);
        
        // Enable detailed logging in development
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            optionsBuilder.LogTo(
                message => _logger.LogDebug("EF Core: {Message}", message),
                LogLevel.Information);
        }
        
        return new AcademicContext(optionsBuilder.Options, _tenantContext);
    }
}

/// <summary>
/// Scoped service that provides the correct DbContext for the current tenant
/// </summary>
public class TenantDbContextProvider
{
    private readonly IDbContextFactory<AcademicContext> _factory;
    private AcademicContext? _context;
    
    public TenantDbContextProvider(IDbContextFactory<AcademicContext> factory)
    {
        _factory = factory;
    }
    
    public async Task<AcademicContext> GetContextAsync(CancellationToken ct = default)
    {
        if (_context == null)
        {
            _context = await _factory.CreateDbContextAsync(ct);
        }
        return _context;
    }
}
```

---

## Aggregate Reporting Without Privacy Leaks

### Anonymization Strategy for Reports

When generating aggregate reports (class-level or school-level), we must ensure individual students cannot be identified.

#### Minimum Aggregation Threshold

```csharp
/// <summary>
/// Configuration for privacy-preserving aggregation
/// </summary>
public static class PrivacyConfig
{
    /// <summary>
    /// Minimum number of students required for aggregate reporting
    /// Below this threshold, data is suppressed to prevent identification
    /// </summary>
    public const int MinimumAggregationThreshold = 5;
    
    /// <summary>
    /// For very small classes, we need even higher threshold
    /// to prevent deductive disclosure
    /// </summary>
    public const int SmallClassThreshold = 10;
    
    /// <summary>
    /// Complementary suppression: if 9 out of 10 students are shown,
    /// the 10th can be deduced
    /// </summary>
    public const double MaximumGroupProportionThreshold = 0.9;
}
```

#### Privacy-Preserving Aggregate Functions

```csharp
/// <summary>
/// Extension methods for privacy-preserving aggregation
/// </summary>
public static class PrivacyPreservingAggregation
{
    /// <summary>
    /// Calculate average with privacy protection
    /// Returns null if sample size too small
    /// </summary>
    public static double? PrivacyPreservingAverage(
        this IEnumerable<double> values,
        int minimumThreshold = PrivacyConfig.MinimumAggregationThreshold)
    {
        var valuesList = values.ToList();
        
        return valuesList.Count >= minimumThreshold
            ? valuesList.Average()
            : null; // Suppress if below threshold
    }
    
    /// <summary>
    /// Calculate count with privacy protection
    /// Adds small random noise to prevent exact counting
    /// </summary>
    public static int? PrivacyPreservingCount(
        this IEnumerable<object> items,
        int minimumThreshold = PrivacyConfig.MinimumAggregationThreshold)
    {
        var count = items.Count();
        
        if (count < minimumThreshold)
            return null; // Suppress
        
        // Add differential privacy noise for large groups
        if (count >= 20)
        {
            var noise = Random.Shared.Next(-1, 2); // -1, 0, or 1
            return count + noise;
        }
        
        return count;
    }
    
    /// <summary>
    /// Calculate distribution with privacy protection
    /// Suppresses small buckets that could identify individuals
    /// </summary>
    public static Dictionary<TKey, int> PrivacyPreservingDistribution<T, TKey>(
        this IEnumerable<T> items,
        Func<T, TKey> keySelector,
        int minimumBucketSize = PrivacyConfig.MinimumAggregationThreshold)
        where TKey : notnull
    {
        var distribution = items
            .GroupBy(keySelector)
            .ToDictionary(g => g.Key, g => g.Count());
        
        // Remove small buckets
        var safeBuckets = distribution
            .Where(kvp => kvp.Value >= minimumBucketSize)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        
        // If removing small buckets leaves only 1 bucket, suppress all
        return safeBuckets.Count >= 2 
            ? safeBuckets 
            : new Dictionary<TKey, int>();
    }
}
```

#### Safe Aggregate Report Models

```csharp
/// <summary>
/// Class performance report that protects student privacy
/// </summary>
public record ClassPerformanceReport
{
    public required Guid ClassId { get; init; }
    public required string ClassName { get; init; }
    public required Subject Subject { get; init; }
    public required DateTimeOffset ReportDate { get; init; }
    
    // Aggregated metrics (no individual student data)
    public int? TotalStudents { get; init; }  // May be null if < threshold
    public double? AverageScore { get; init; } // Null if < threshold
    public double? MedianScore { get; init; }  // Null if < threshold
    public double? StandardDeviation { get; init; }
    
    // Distribution (small buckets suppressed)
    public Dictionary<DifficultyLevel, int>? DifficultyDistribution { get; init; }
    public Dictionary<MasteryLevel, int>? MasteryDistribution { get; init; }
    
    // Areas of improvement (aggregated, no names)
    public List<LearningObjectivePerformance> AreasNeedingImprovement { get; init; } = [];
    public List<LearningObjectivePerformance> StrengthAreas { get; init; } = [];
    
    // Privacy metadata
    public bool IsDataSuppressed { get; init; }
    public string? SuppressionReason { get; init; }
}

/// <summary>
/// Aggregated learning objective performance (no student names)
/// </summary>
public record LearningObjectivePerformance
{
    public required string ObjectiveId { get; init; }
    public required string ObjectiveDescription { get; init; }
    public double? AverageMastery { get; init; }  // 0.0 to 1.0
    public int? StudentCount { get; init; }       // Null if < threshold
    public string? RecommendedIntervention { get; init; }
}

/// <summary>
/// School-wide report with additional privacy protections
/// </summary>
public record SchoolPerformanceReport
{
    public required Guid SchoolId { get; init; }
    public required string SchoolName { get; init; }
    public required DateTimeOffset ReportDate { get; init; }
    
    // School-level aggregates
    public int? TotalStudents { get; init; }
    public int? TotalClasses { get; init; }
    public Dictionary<GradeLevel, double?>? AverageScoresByGrade { get; init; }
    public Dictionary<Subject, double?>? AverageScoresBySubject { get; init; }
    
    // Trends (no individual identification possible)
    public List<TrendDataPoint> PerformanceTrend { get; init; } = [];
    
    // Class comparison (anonymized if needed)
    public List<AnonymizedClassPerformance> ClassComparison { get; init; } = [];
    
    public bool IsDataSuppressed { get; init; }
    public string? SuppressionReason { get; init; }
}

/// <summary>
/// Anonymized class performance for school-level comparison
/// </summary>
public record AnonymizedClassPerformance
{
    // No class name, only anonymous identifier
    public required string AnonymousClassId { get; init; }  // e.g., "Class A", "Class B"
    public required GradeLevel GradeLevel { get; init; }
    public required Subject Subject { get; init; }
    public double? AverageScore { get; init; }
    public int? StudentCount { get; init; }  // Rounded or suppressed
}
```

#### Report Generation Service

```csharp
public interface IPrivacyPreservingReportService
{
    Task<Result<ClassPerformanceReport>> GenerateClassReportAsync(
        Guid classId,
        Subject subject,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        CancellationToken ct = default);
    
    Task<Result<SchoolPerformanceReport>> GenerateSchoolReportAsync(
        Guid schoolId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        CancellationToken ct = default);
}

public class PrivacyPreservingReportService : IPrivacyPreservingReportService
{
    private readonly IStudentAssessmentRepository _assessmentRepo;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PrivacyPreservingReportService> _logger;
    
    public async Task<Result<ClassPerformanceReport>> GenerateClassReportAsync(
        Guid classId,
        Subject subject,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        CancellationToken ct)
    {
        // Verify authorization (teacher must be assigned to this class)
        if (!await VerifyClassAccessAsync(classId, ct))
        {
            return new Error("UNAUTHORIZED", "Not authorized to view this class");
        }
        
        // Get all assessments for the class
        var assessmentsResult = await _assessmentRepo.GetByClassAndSubjectAsync(
            classId, subject, startDate, endDate, ct);
        
        if (assessmentsResult is Result<IReadOnlyList<StudentAssessment>>.Failure failure)
        {
            return failure.Error;
        }
        
        var assessments = ((Result<IReadOnlyList<StudentAssessment>>.Success)assessmentsResult).Value;
        
        // Check minimum threshold
        var studentCount = assessments.Select(a => a.StudentId).Distinct().Count();
        
        if (studentCount < PrivacyConfig.MinimumAggregationThreshold)
        {
            _logger.LogInformation(
                "Suppressing class report for class {ClassId} - only {Count} students (minimum {Threshold})",
                classId, studentCount, PrivacyConfig.MinimumAggregationThreshold);
            
            return new ClassPerformanceReport
            {
                ClassId = classId,
                ClassName = await GetClassNameAsync(classId, ct),
                Subject = subject,
                ReportDate = DateTimeOffset.UtcNow,
                IsDataSuppressed = true,
                SuppressionReason = $"Insufficient students ({studentCount}) for privacy-preserving aggregation. Minimum required: {PrivacyConfig.MinimumAggregationThreshold}"
            };
        }
        
        // Calculate privacy-preserving aggregates
        var scores = assessments
            .Where(a => a.Score.HasValue)
            .Select(a => a.Score!.Value)
            .ToList();
        
        var averageScore = scores.PrivacyPreservingAverage();
        var medianScore = CalculateMedian(scores);
        var stdDev = CalculateStandardDeviation(scores);
        
        // Analyze learning objectives
        var objectivePerformance = await AnalyzeLearningObjectivesAsync(
            assessments, ct);
        
        // Identify areas needing improvement
        var areasNeedingImprovement = objectivePerformance
            .Where(op => op.AverageMastery < 0.6) // Below 60% mastery
            .OrderBy(op => op.AverageMastery)
            .ToList();
        
        var strengthAreas = objectivePerformance
            .Where(op => op.AverageMastery >= 0.8) // 80%+ mastery
            .OrderByDescending(op => op.AverageMastery)
            .ToList();
        
        return new ClassPerformanceReport
        {
            ClassId = classId,
            ClassName = await GetClassNameAsync(classId, ct),
            Subject = subject,
            ReportDate = DateTimeOffset.UtcNow,
            TotalStudents = studentCount,
            AverageScore = averageScore,
            MedianScore = medianScore,
            StandardDeviation = stdDev,
            AreasNeedingImprovement = areasNeedingImprovement,
            StrengthAreas = strengthAreas,
            IsDataSuppressed = false
        };
    }
    
    private async Task<List<LearningObjectivePerformance>> AnalyzeLearningObjectivesAsync(
        IReadOnlyList<StudentAssessment> assessments,
        CancellationToken ct)
    {
        // Group by learning objective
        var objectiveScores = assessments
            .SelectMany(a => a.Responses)
            .SelectMany(r => r.LearningObjectiveIds.Select(lo => (ObjectiveId: lo, r.IsCorrect)))
            .GroupBy(x => x.ObjectiveId)
            .Select(g => new
            {
                ObjectiveId = g.Key,
                StudentCount = g.Count(),
                Mastery = g.Average(x => x.IsCorrect ? 1.0 : 0.0)
            })
            .Where(x => x.StudentCount >= PrivacyConfig.MinimumAggregationThreshold)
            .ToList();
        
        var result = new List<LearningObjectivePerformance>();
        
        foreach (var obj in objectiveScores)
        {
            var description = await GetObjectiveDescriptionAsync(obj.ObjectiveId, ct);
            var intervention = DetermineIntervention(obj.Mastery);
            
            result.Add(new LearningObjectivePerformance
            {
                ObjectiveId = obj.ObjectiveId,
                ObjectiveDescription = description,
                AverageMastery = obj.Mastery,
                StudentCount = obj.StudentCount,
                RecommendedIntervention = intervention
            });
        }
        
        return result;
    }
}
```

---

## Audit Logging for Privacy Compliance

### Comprehensive Audit Trail

Every access to student data must be logged for compliance and security auditing.

```csharp
/// <summary>
/// Audit log entry for data access tracking
/// </summary>
public record DataAccessAuditLog
{
    public required Guid Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required Guid UserId { get; init; }
    public required string UserEmail { get; init; }
    public required UserRole UserRole { get; init; }
    public required Guid? SchoolId { get; init; }
    public required string Action { get; init; }  // "VIEW", "EXPORT", "MODIFY", "DELETE"
    public required string Resource { get; init; } // "StudentAssessment", "StudentProfile", etc.
    public required Guid? ResourceId { get; init; }
    public required string IpAddress { get; init; }
    public required string UserAgent { get; init; }
    public bool WasAuthorized { get; init; }
    public string? DenialReason { get; init; }
    public Dictionary<string, string> AdditionalMetadata { get; init; } = new();
}

/// <summary>
/// Middleware that logs all data access
/// </summary>
public class DataAccessAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DataAccessAuditMiddleware> _logger;
    
    public async Task InvokeAsync(
        HttpContext context,
        IAuditLogRepository auditRepo,
        ITenantContext tenantContext)
    {
        var startTime = DateTimeOffset.UtcNow;
        var path = context.Request.Path.Value;
        
        // Only audit data access endpoints
        if (path?.Contains("/api/student/") == true ||
            path?.Contains("/api/teacher/") == true ||
            path?.Contains("/api/school-admin/") == true)
        {
            await _next(context);
            
            // Log after request completes
            var auditLog = new DataAccessAuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = startTime,
                UserId = tenantContext.UserId,
                UserEmail = context.User.FindFirst(ClaimTypes.Email)?.Value ?? "unknown",
                UserRole = tenantContext.Role,
                SchoolId = tenantContext.SchoolId,
                Action = context.Request.Method,
                Resource = ExtractResourceType(path),
                ResourceId = ExtractResourceId(path),
                IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                WasAuthorized = context.Response.StatusCode < 400
            };
            
            await auditRepo.AddAsync(auditLog);
            
            _logger.LogInformation(
                "Data access: {User} ({Role}) accessed {Resource} {ResourceId} from {SchoolId} - {StatusCode}",
                auditLog.UserEmail,
                auditLog.UserRole,
                auditLog.Resource,
                auditLog.ResourceId,
                auditLog.SchoolId,
                context.Response.StatusCode);
        }
        else
        {
            await _next(context);
        }
    }
}
```

---

## Data Retention and Right to Be Forgotten

### GDPR/FERPA Compliance

```csharp
/// <summary>
/// Service for handling student data deletion requests
/// </summary>
public interface IStudentDataDeletionService
{
    /// <summary>
    /// Permanently delete all data for a student
    /// This is irreversible and used for "right to be forgotten"
    /// </summary>
    Task<Result<Unit>> DeleteStudentDataAsync(
        Guid studentId,
        string requestReason,
        CancellationToken ct = default);
    
    /// <summary>
    /// Export all data for a student (GDPR data portability)
    /// </summary>
    Task<Result<StudentDataExport>> ExportStudentDataAsync(
        Guid studentId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Anonymize student data (keep for research/analytics)
    /// </summary>
    Task<Result<Unit>> AnonymizeStudentDataAsync(
        Guid studentId,
        CancellationToken ct = default);
}

public class StudentDataDeletionService : IStudentDataDeletionService
{
    public async Task<Result<Unit>> DeleteStudentDataAsync(
        Guid studentId,
        string requestReason,
        CancellationToken ct)
    {
        // IMPORTANT: This affects only the student's school database
        // Physical partitioning ensures we don't accidentally delete
        // data from other schools
        
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        
        try
        {
            // 1. Delete assessment responses
            await _context.StudentResponses
                .Where(sr => sr.StudentId == studentId)
                .ExecuteDeleteAsync(ct);
            
            // 2. Delete assessments
            await _context.StudentAssessments
                .Where(sa => sa.StudentId == studentId)
                .ExecuteDeleteAsync(ct);
            
            // 3. Delete progress tracking
            await _context.StudentProgress
                .Where(sp => sp.StudentId == studentId)
                .ExecuteDeleteAsync(ct);
            
            // 4. Delete student profile
            await _context.Students
                .Where(s => s.Id == studentId)
                .ExecuteDeleteAsync(ct);
            
            // 5. Log the deletion
            await _auditRepo.AddAsync(new DataAccessAuditLog
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                UserId = _tenantContext.UserId,
                Action = "DELETE_STUDENT_DATA",
                Resource = "Student",
                ResourceId = studentId,
                SchoolId = _tenantContext.SchoolId,
                AdditionalMetadata = new()
                {
                    ["Reason"] = requestReason,
                    ["DeletedBy"] = _tenantContext.UserId.ToString()
                }
            }, ct);
            
            await transaction.CommitAsync(ct);
            
            _logger.LogWarning(
                "Permanently deleted all data for student {StudentId} from school {SchoolId}. Reason: {Reason}",
                studentId, _tenantContext.SchoolId, requestReason);
            
            return Unit.Value;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, 
                "Failed to delete student data for {StudentId}", studentId);
            return new Error("DELETE_FAILED", "Failed to delete student data", ex);
        }
    }
}
```

---

## Summary: Privacy-First Architecture

### Key Privacy Protections

1. ✅ **Physical Database Partitioning**
   - Each school has dedicated database instance
   - Impossible to accidentally query wrong school
   - Network-level isolation

2. ✅ **Minimum Aggregation Thresholds**
   - Reports suppressed if < 5 students
   - Small buckets removed from distributions
   - Differential privacy noise for large groups

3. ✅ **Comprehensive Audit Logging**
   - Every data access logged
   - Immutable audit trail
   - FERPA compliance

4. ✅ **Right to Be Forgotten**
   - Complete data deletion capability
   - Data export for portability
   - Anonymization option

5. ✅ **Role-Based Access Control**
   - Least privilege principle
   - Teachers see only their classes
   - School admins see only their school
   - Course admins see only anonymized data

### Privacy Checklist for Every Feature

Before implementing any feature that touches student data:

- [ ] Does it access student PII?
- [ ] Is the access logged in audit trail?
- [ ] Is proper authorization enforced?
- [ ] If generating reports, are aggregation thresholds met?
- [ ] Can students be identified from the output?
- [ ] Is data from correct school database only?
- [ ] Is there a legitimate educational interest?
- [ ] Have we minimized data collection?

---

*Last Updated: October 11, 2025*
