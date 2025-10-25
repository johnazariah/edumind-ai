# TODO-030: Implement Dead Letter Queue for Failed Tasks

**Priority:** P2 - Medium  
**Area:** Backend / Infrastructure  
**Estimated Effort:** Medium (4-6 hours)  
**Status:** Not Started

## Description

Implement a dead letter queue (DLQ) mechanism for tasks that fail repeatedly in the orchestrator, allowing manual intervention and preventing infinite retry loops.

## Context

The `StudentProgressOrchestrator` has a TODO comment (line 810) to move failed tasks to a dead letter queue after exhausting retry attempts. Currently, tasks that fail after maximum retries are logged but not persisted for later analysis or manual retry.

A DLQ provides:

- Persistent storage of failed tasks
- Manual inspection and debugging
- Manual retry capability
- Failure pattern analysis
- Prevention of infinite retry loops

## Technical Requirements

### Database Table

```sql
CREATE TABLE orchestration_dead_letter_queue (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    task_id UUID NOT NULL,
    task_type VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL,
    agent_id UUID,
    failure_reason TEXT NOT NULL,
    retry_count INT NOT NULL,
    first_failed_at TIMESTAMPTZ NOT NULL,
    last_failed_at TIMESTAMPTZ NOT NULL,
    error_details JSONB,
    status VARCHAR(50) NOT NULL DEFAULT 'pending', -- pending, retrying, resolved, abandoned
    manually_resolved BOOLEAN DEFAULT FALSE,
    resolved_at TIMESTAMPTZ,
    resolved_by VARCHAR(255),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_dlq_status ON orchestration_dead_letter_queue(status);
CREATE INDEX idx_dlq_task_type ON orchestration_dead_letter_queue(task_type);
CREATE INDEX idx_dlq_first_failed ON orchestration_dead_letter_queue(first_failed_at);
```

### Entity Model

```csharp
public class DeadLetterQueueEntity
{
    public required Guid Id { get; init; }
    public required Guid TaskId { get; init; }
    public required string TaskType { get; init; }
    public required string Payload { get; init; } // JSON
    public Guid? AgentId { get; init; }
    public required string FailureReason { get; init; }
    public required int RetryCount { get; init; }
    public required DateTimeOffset FirstFailedAt { get; init; }
    public required DateTimeOffset LastFailedAt { get; init; }
    public string? ErrorDetails { get; init; } // JSON
    public required DlqStatus Status { get; init; }
    public bool ManuallyResolved { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
    public string? ResolvedBy { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public enum DlqStatus
{
    Pending,
    Retrying,
    Resolved,
    Abandoned
}
```

### Repository Interface

```csharp
public interface IDeadLetterQueueRepository
{
    Task<Result<DeadLetterQueueEntity>> AddAsync(
        DeadLetterQueueEntity entity,
        CancellationToken cancellationToken = default);
        
    Task<Result<IReadOnlyList<DeadLetterQueueEntity>>> GetPendingAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);
        
    Task<Result<DeadLetterQueueEntity>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
        
    Task<Result<IReadOnlyList<DeadLetterQueueEntity>>> GetByTaskTypeAsync(
        string taskType,
        DlqStatus? status = null,
        CancellationToken cancellationToken = default);
        
    Task<Result> UpdateStatusAsync(
        Guid id,
        DlqStatus status,
        string? resolvedBy = null,
        CancellationToken cancellationToken = default);
        
    Task<Result> RetryAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
```

### Orchestrator Integration

Update `StudentProgressOrchestrator.RouteTaskWithFallback()`:

```csharp
// Replace TODO comment at line 810
catch (Exception ex)
{
    _logger.LogError(ex, "Task {TaskId} failed after {Retries} retries", 
        task.Id, maxRetries);
        
    // Move to dead letter queue for manual intervention
    var dlqEntry = new DeadLetterQueueEntity
    {
        Id = Guid.NewGuid(),
        TaskId = task.Id,
        TaskType = task.TaskType,
        Payload = JsonSerializer.Serialize(task),
        AgentId = selectedAgent?.AgentId,
        FailureReason = ex.Message,
        RetryCount = maxRetries,
        FirstFailedAt = task.CreatedAt,
        LastFailedAt = DateTimeOffset.UtcNow,
        ErrorDetails = JsonSerializer.Serialize(new
        {
            ExceptionType = ex.GetType().FullName,
            StackTrace = ex.StackTrace,
            InnerException = ex.InnerException?.Message
        }),
        Status = DlqStatus.Pending,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };
    
    await _dlqRepository.AddAsync(dlqEntry, cancellationToken);
    
    return new Error("TaskFailed", $"Task moved to dead letter queue: {dlqEntry.Id}");
}
```

### API Endpoints

```csharp
[ApiController]
[Route("api/v1/orchestration/dlq")]
public class DeadLetterQueueController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPendingTasks(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        // Return list of pending DLQ entries
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTaskById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Return single DLQ entry
    }
    
    [HttpPost("{id}/retry")]
    public async Task<IActionResult> RetryTask(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Attempt to retry the failed task
    }
    
    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> ResolveTask(
        Guid id,
        [FromBody] ResolveRequest request,
        CancellationToken cancellationToken = default)
    {
        // Mark task as manually resolved
    }
    
    [HttpPost("{id}/abandon")]
    public async Task<IActionResult> AbandonTask(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Mark task as abandoned (won't retry)
    }
}
```

## Acceptance Criteria

- [ ] Database table for DLQ created
- [ ] Entity model and repository interface defined
- [ ] Repository implementation with all CRUD operations
- [ ] Orchestrator integrates DLQ on task failure
- [ ] Failed tasks persist to DLQ after max retries
- [ ] API endpoints for DLQ management created
- [ ] UI page to view DLQ entries (Dashboard app)
- [ ] Manual retry functionality implemented
- [ ] Manual resolve functionality implemented
- [ ] Abandon task functionality implemented
- [ ] Filtering by task type and status
- [ ] Error details captured (exception type, stack trace)
- [ ] Automatic cleanup of old resolved/abandoned entries (>30 days)
- [ ] Metrics: DLQ size, retry success rate
- [ ] Alerts when DLQ size exceeds threshold (e.g., >100 items)
- [ ] Unit tests for repository (>80% coverage)
- [ ] Integration tests for full DLQ flow
- [ ] API documentation (Swagger)

## Dependencies

- Database migration tool (EF Core migrations)
- JSON serialization (System.Text.Json)

## References

- **Files:**
  - `src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs` (line 810)
  - `src/AcademicAssessment.Infrastructure/Entities/DeadLetterQueueEntity.cs` (new)
  - `src/AcademicAssessment.Infrastructure/Repositories/DeadLetterQueueRepository.cs` (new)
  - `src/AcademicAssessment.Web/Controllers/DeadLetterQueueController.cs` (new)
  
- **Documentation:**
  - `.github/adr/010-multi-agent-architecture.md`
  
- **Related TODOs:**
  - TODO-004: Real-time Monitoring Dashboard (can display DLQ metrics)
  - TODO-031: Implement Agent Health Checking (uses similar failure tracking)

## Implementation Notes

1. **Retry Strategy:** Exponential backoff with jitter for manual retries
2. **Payload Size:** Consider compression for large task payloads
3. **Cleanup Job:** Background job to purge old entries (>30 days)
4. **Metrics:** Track DLQ size, retry success rate, resolution time
5. **Alerts:** Notify admins when DLQ size exceeds threshold
6. **Security:** Restrict DLQ management to admin role
7. **Idempotency:** Ensure retry is idempotent (check if task already succeeded)

## Testing Strategy

**Unit Tests:**

- Repository CRUD operations
- Entity validation
- Status transitions

**Integration Tests:**

- Create orchestrator task that always fails
- Verify task moves to DLQ after max retries
- Retry task from DLQ and verify success
- Resolve task manually
- Abandon task

**Manual Tests:**

- View DLQ entries in Dashboard
- Retry failed task from UI
- Resolve task with notes
- Verify cleanup job runs
