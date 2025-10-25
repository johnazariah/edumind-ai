# TODO-050: Replace HTML Navigation Workaround with Proper Solution

**Priority:** P3 - Low  
**Area:** Frontend / Technical Debt  
**Estimated Effort:** Medium (4-6 hours)  
**Status:** Not Started (Deferred)

## Description

Replace the HTML anchor link workaround currently used for navigation with a proper solution that addresses the underlying Blazor Server SignalR connectivity issues.

## Context

**Current Situation:**  
Several critical navigation points in the Student App use HTML anchor links (`<a href="">`) instead of Blazor's `NavigationManager` or `@onclick` events. This workaround was implemented because SignalR connections were unreliable during long-running LLM calls, causing interactive components to fail.

**Files Affected:**

- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentDetail.razor`
- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentDashboard.razor`
- Other pages with navigation buttons

**Documented In:**

- ADR-063: HTML Links vs Blazor Navigation (Workaround)
- Commit c45c0ec: WIP: Fix Student App HTTP client and navigation issues

**Trade-offs:**

- ✅ Reliable navigation (works even when SignalR fails)
- ✅ No dependency on JavaScript interop
- ❌ Full page reload (slower, loses component state)
- ❌ Less elegant than Blazor's built-in navigation
- ❌ Inconsistent with Blazor best practices

## Root Cause Analysis

The underlying issue appears to be:

1. **Long-running operations:** LLM calls (20-25 seconds with Ollama) keep connections busy
2. **SignalR timeout:** Default SignalR timeout (30 seconds) can be exceeded
3. **Connection recovery:** SignalR doesn't gracefully recover during operation
4. **Blazor Server limitation:** Interactive components require active SignalR connection

## Possible Solutions

### Option 1: Configure SignalR Timeouts

Increase SignalR timeouts to accommodate long-running operations:

```csharp
// In Program.cs
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        options.DisconnectedCircuitMaxRetained = 100;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5);
        options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(2);
    })
    .AddHubOptions(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
        options.HandshakeTimeout = TimeSpan.FromMinutes(1);
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.MaximumReceiveMessageSize = 1024 * 1024; // 1 MB
    });
```

**Pros:**

- Addresses root cause
- No code changes to components
- Preserves Blazor's reactive model

**Cons:**

- Keeps connections open longer (resource usage)
- Doesn't solve problem if operation exceeds even extended timeout
- May mask other connectivity issues

### Option 2: Move LLM Calls to Background Jobs

Execute long-running LLM operations asynchronously:

```csharp
// Start assessment session
public async Task<IActionResult> StartSession(Guid assessmentId)
{
    var sessionId = Guid.NewGuid();
    
    // Queue background job for question generation
    await _backgroundJobClient.EnqueueAsync(() => 
        GenerateQuestionsAsync(sessionId, assessmentId));
    
    return Ok(new { sessionId, status = "Generating questions..." });
}

// Poll for status
public async Task<IActionResult> GetSessionStatus(Guid sessionId)
{
    var session = await _sessionRepository.GetByIdAsync(sessionId);
    return Ok(new { status = session.Status, progress = session.Progress });
}
```

**Pros:**

- Keeps SignalR connections responsive
- Better UX with progress updates
- Scalable for production load

**Cons:**

- Requires background job infrastructure (Hangfire, Azure Functions)
- More complex implementation
- Delayed gratification for users

### Option 3: Migrate to Blazor WebAssembly

Replace Blazor Server with Blazor WebAssembly:

**Pros:**

- No SignalR dependency
- Client-side execution
- Better scalability

**Cons:**

- Major architectural change
- Exposes API keys (need API gateway)
- Larger initial download
- Requires significant refactoring

### Option 4: Hybrid Approach (Recommended)

Combine multiple strategies:

1. Increase SignalR timeouts moderately (Option 1)
2. Move question generation to background jobs (Option 2)
3. Keep HTML navigation as fallback for critical paths
4. Gradually replace workarounds as jobs are implemented

## Acceptance Criteria

- [ ] Root cause documented and understood
- [ ] Solution approach selected and approved
- [ ] SignalR configuration optimized (if Option 1)
- [ ] Background job system implemented (if Option 2)
- [ ] LLM calls moved to background processing
- [ ] Progress polling implemented
- [ ] HTML navigation replaced with Blazor `NavigationManager`
- [ ] `@onclick` events restored for navigation buttons
- [ ] Component state preserved during navigation
- [ ] Testing confirms reliable operation during LLM calls
- [ ] Performance testing under load
- [ ] No regressions in existing functionality
- [ ] ADR-063 updated to reflect resolution
- [ ] Code comments removed indicating workaround

## Dependencies

**Option 1 (Config):**

- No dependencies

**Option 2 (Background Jobs):**

- Hangfire or Azure Functions
- Message queue (Azure Service Bus, RabbitMQ)
- Database for job tracking

**Option 3 (WASM):**

- Major refactoring effort
- API gateway or authentication changes

## References

- **ADR:**
  - `.github/adr/063-html-links-vs-blazor-navigation.md` (documents workaround)
  
- **Files:**
  - `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentDetail.razor`
  - `src/AcademicAssessment.StudentApp/Program.cs` (SignalR configuration)
  - `src/AcademicAssessment.Web/Controllers/AssessmentController.cs`
  
- **Commits:**
  - c45c0ec: WIP: Fix Student App HTTP client and navigation issues
  
- **Documentation:**
  - `docs/deployment/LOCAL_DEPLOYMENT_GUIDE.md` (documents the issue)

- **Related TODOs:**
  - TODO-060: Implement Background Job System (prerequisite for Option 2)
  - TODO-061: Add Progress Polling UI

## Implementation Notes

1. **Phased Approach:** Don't replace all at once
2. **Testing:** Test each change thoroughly before moving to next
3. **Monitoring:** Add logging to track SignalR disconnections
4. **Fallback:** Keep HTML navigation as emergency fallback
5. **Performance:** Monitor resource usage with extended timeouts
6. **Documentation:** Update ADR when solution is permanent

## Testing Strategy

**Unit Tests:**

- Test navigation manager calls
- Mock SignalR connections

**Integration Tests:**

- Start long-running operation and test navigation
- Simulate SignalR disconnection
- Verify reconnection logic

**Load Tests:**

- 100+ concurrent users with LLM calls
- Monitor SignalR connection pool
- Measure timeout frequency

**Manual Tests:**

- Start assessment (20-25 second LLM call)
- Attempt navigation during generation
- Verify UI remains responsive
- Test on slow network connections
- Test with network interruptions

## Recommendation

**Defer to P3 (Low Priority)** because:

1. **Current workaround is functional** - Users can complete workflows
2. **No data loss** - Full page reload is safe
3. **Higher priorities exist** - Complete core features first
4. **Proper solution is complex** - Requires background job infrastructure
5. **Technical debt is documented** - ADR-063 explains the situation

**Revisit when:**

- Background job system is implemented (TODO-060)
- User feedback indicates page reloads are problematic
- Production load reveals scaling issues
- Time permits for technical debt paydown

## Future Consideration

Long-term, consider migrating to a modern frontend framework:

- React + TypeScript (better ecosystem, more mature)
- Vue.js (simpler learning curve)
- Svelte (best performance)

This would eliminate SignalR entirely and provide better developer experience.
