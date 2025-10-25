# ADR-002: Blazor Server for Student App

**Status:** ✅ Accepted  
**Date:** October 2025  
**Context:** Week 1, Day 1 - UI Framework Selection

## Context

The student-facing application required a decision between:

- Blazor Server (maintains UI state on server, uses SignalR)
- Blazor WebAssembly (runs entirely in browser)
- Traditional MVC with JavaScript
- React/Angular SPA with separate backend

Key requirements:

- Real-time progress updates during assessments
- Secure assessment delivery (no client-side question tampering)
- Rich interactive UI (drag-and-drop, instant feedback)
- Server-side validation and scoring
- Low initial load time
- Support for 50-50,000 concurrent students

## Decision

Selected **Blazor Server** with SignalR for all student-facing interfaces (AcademicAssessment.StudentApp).

## Rationale

1. **Security**: All assessment logic stays on server - no client-side manipulation
2. **Real-time Built-in**: SignalR is native to Blazor Server for progress updates
3. **Server-side State**: Student session and assessment state maintained securely on server
4. **Faster Initial Load**: No large WASM download required
5. **Full .NET API Access**: Direct access to Entity Framework, repositories, and services
6. **Simpler Architecture**: Single technology stack (no REST API for every UI interaction)
7. **Cost-Effective**: Less bandwidth than WASM (only UI diffs transmitted)

## Consequences

### Positive

- Secure by design - assessment questions never exposed to client
- Real-time progress updates via SignalR with minimal code
- Consistent server-side validation
- Easy integration with assessment orchestrator
- Reduced attack surface (no client-side code to reverse-engineer)

### Negative

- Requires persistent SignalR connection (can't work offline)
- Server must maintain UI state for each connected student
- More server memory usage compared to WASM
- Network latency affects UI responsiveness
- Connection drops require reconnection logic

### Risks Mitigated

- Implemented Redis-backed SignalR backplane for scale-out
- Added circuit handler for connection recovery
- Configured connection timeouts (4 hours for long assessments)
- Fallback to HTML links for navigation if SignalR fails (ADR-063)

## Alternative Considered: Blazor WebAssembly

**Rejected because:**

- Assessment questions would be exposed in browser memory/DevTools
- Scoring logic could be tampered with
- Requires complex offline sync for network failures
- Larger initial download (multi-MB WASM runtime)
- Would require separate REST API for all operations

## Implementation Details

```csharp
// Student app uses interactive server components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SignalR configured with Azure SignalR Service for production
builder.Services.AddSignalR()
    .AddAzureSignalR(options => {
        options.ConnectionString = configuration["AzureSignalR:ConnectionString"];
    });
```

## Related Decisions

- ADR-006: Redis for Caching Layer (SignalR backplane)
- ADR-063: HTML Links vs Blazor Navigation (fallback strategy)
- ADR-020: Azure Container Apps for Hosting (scale-out SignalR)

## References

- `src/AcademicAssessment.StudentApp/AcademicAssessment.StudentApp.csproj`
- Commit: `ef219b5` - "feat: Week 1 - Complete Orchestrator Logic"
- docs/architecture/ARCHITECTURE_SUMMARY.md - Section on Student App
