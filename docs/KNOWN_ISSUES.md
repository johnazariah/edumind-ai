# Known Issues

## .NET 9.0 WebApplicationFactory Serialization Bug

**Status:** BLOCKED - Awaiting Framework Fix  
**Severity:** HIGH - Blocks all integration tests  
**Affected Version:** .NET 9.0.10  
**GitHub Issue:** <https://github.com/dotnet/aspnetcore/issues/52187>

### Summary

Integration tests using `WebApplicationFactory<Program>` fail with serialization errors when controllers return responses. The error occurs in the test host's `PipeWriter` implementation.

### Error Message

```text
System.InvalidOperationException: The PipeWriter 'ResponseBodyPipeWriter' does not implement PipeWriter.UnflushedBytes
   at System.Text.Json.ThrowHelper.ThrowInvalidOperationException_PipeWriterDoesNotImplementUnflushedBytes
   at System.Text.Json.Serialization.Metadata.JsonTypeInfo`1.SerializeAsync(PipeWriter pipeWriter, ...)
   at Microsoft.AspNetCore.Mvc.Formatters.SystemTextJsonOutputFormatter.WriteResponseBodyAsync(...)
```

### Impact

- ✅ **Authentication:** Working correctly - tests authenticate and reach controllers
- ✅ **Authorization:** All policies configured and working  
- ✅ **Business Logic:** Controller actions execute successfully
- ❌ **Response Serialization:** Fails when writing JSON response back to test client

### Attempted Workarounds (All Failed)

1. **DefaultBufferSize = 4096** in `Program.cs` AddControllers().AddJsonOptions()
   - Result: Same error, no improvement

2. **DefaultBufferSize = 1** in test factory Configure JsonOptions
   - Result: Same error, no improvement

3. **PostConfigure JwtBearerOptions** instead of Configure
   - Result: Fixed auth but not serialization

### Test Evidence

Test logs show successful authentication but serialization failure:

```text
[INF] User 00000000-0000-0000-0000-000000000001 requesting performance summary
[INF] Getting performance summary for student 00000000-0000-0000-0000-000000000001
[ERR] HTTP GET /api/v1/students/.../performance-summary responded 500
```

The controller receives the request, processes it, but fails when returning the response.

### Possible Solutions

#### Option 1: Use Newtonsoft.Json for Tests (RECOMMENDED)

Add to `Program.cs`:

```csharp
builder.Services.AddControllers()
    .AddNewtonsoftJson(); // NuGet: Microsoft.AspNetCore.Mvc.NewtonsoftJson
```

This bypasses System.Text.Json and the PipeWriter issue.

#### Option 2: Test Against Real Aspire Instance

Instead of using `WebApplicationFactory`, run tests against the actual Aspire-hosted API:

- Start Aspire: `dotnet run --project src/EduMind.AppHost`
- Update test configuration to use live endpoint
- Trade-off: Slower tests, requires running services

#### Option 3: Wait for .NET Framework Update

Monitor the GitHub issue and update to .NET 9.0.11+ when available.

#### Option 4: Downgrade to .NET 8.0

Revert to .NET 8.0 LTS where this issue does not exist.

### Current Status

- Integration test configuration is **complete and correct**
- Authentication and authorization are **fully working**
- Blocked by framework-level bug in .NET 9 test host
- Awaiting framework fix or implementing Newtonsoft.Json workaround

### Related Files

- `tests/AcademicAssessment.Tests.Integration/Helpers/AuthenticatedWebApplicationFactory.cs`
- `src/AcademicAssessment.Web/Program.cs` (lines 167-173)
- All test classes in `AcademicAssessment.Tests.Integration`

### Last Updated

January 2025 - .NET 9.0.10 with Aspire 9.5.1
