# Offline Sync Architecture

**Version:** 1.0  
**Date:** 2025-10-27  
**Status:** Draft

---

## Executive Summary

This document defines the architecture for offline assessment capabilities in the EduMind.AI mobile app. Students will be able to download assessments while online, complete them offline (e.g., during commute, in areas with poor connectivity), and automatically sync their responses when connectivity is restored.

**Key Requirements:**
- Download assessments for offline use
- Complete assessments without network connectivity
- Automatically sync when online
- Handle conflicts gracefully
- Minimize storage footprint
- Ensure data integrity

---

## 1. Architecture Overview

### 1.1 Operating Modes

```
┌─────────────────────────────────────────────────────┐
│                   Mobile App                         │
├─────────────────────────────────────────────────────┤
│                                                      │
│  ┌──────────────┐    ┌──────────────┐              │
│  │  Online Mode │    │ Offline Mode │              │
│  │  (Default)   │←→  │   (Cached)   │              │
│  └──────────────┘    └──────────────┘              │
│         ↓                    ↓                       │
│  ┌──────────────────────────────────┐               │
│  │     SQLite Local Database        │               │
│  │  • Cached Assessments            │               │
│  │  • Pending Responses             │               │
│  │  • Sync Queue                    │               │
│  └──────────────────────────────────┘               │
│         ↕                                            │
└─────────┼────────────────────────────────────────────┘
          ↓
    ┌─────────────┐
    │  REST API   │
    │  (Backend)  │
    └─────────────┘
```

### 1.2 Sync Flow

```
Download Phase (Online):
1. User selects "Download for Offline"
2. App fetches assessment from API
3. Store in SQLite with expiration
4. Mark as "downloaded"

Offline Phase:
1. User opens assessment (no network)
2. Load from SQLite cache
3. User answers questions
4. Save responses to SQLite
5. Add to sync queue
6. Mark as "pending_sync"

Sync Phase (Back Online):
1. Detect network connectivity
2. Process sync queue in order
3. Upload responses to API
4. Handle success/conflicts
5. Mark as "synced"
6. Optionally delete cache
```

---

## 2. SQLite Database Schema

### 2.1 Tables

```sql
-- Cached assessments (downloaded for offline use)
CREATE TABLE cached_assessments (
    -- Identity
    assessment_id TEXT PRIMARY KEY,
    assessment_version TEXT NOT NULL, -- For conflict detection
    
    -- Assessment data (JSON serialized)
    assessment_data TEXT NOT NULL, -- Full assessment JSON
    
    -- Metadata
    downloaded_at INTEGER NOT NULL, -- Unix timestamp (seconds)
    expires_at INTEGER NOT NULL, -- Unix timestamp (seconds)
    size_bytes INTEGER NOT NULL,
    
    -- Status
    is_completed BOOLEAN NOT NULL DEFAULT 0,
    last_accessed_at INTEGER,
    
    -- Indexes
    UNIQUE(assessment_id)
);

CREATE INDEX idx_cached_assessments_expires ON cached_assessments(expires_at);
CREATE INDEX idx_cached_assessments_accessed ON cached_assessments(last_accessed_at);

-- Pending responses (waiting to sync)
CREATE TABLE pending_responses (
    -- Identity
    id TEXT PRIMARY KEY, -- UUID generated client-side
    
    -- References
    student_id TEXT NOT NULL,
    assessment_id TEXT NOT NULL,
    question_id TEXT NOT NULL,
    
    -- Response data (JSON serialized)
    response_data TEXT NOT NULL, -- Answer, time spent, etc.
    
    -- Metadata
    answered_at INTEGER NOT NULL, -- Unix timestamp (milliseconds)
    synced BOOLEAN NOT NULL DEFAULT 0,
    sync_attempted_at INTEGER, -- Last sync attempt timestamp
    sync_attempts INTEGER NOT NULL DEFAULT 0,
    sync_error TEXT, -- Error message if sync failed
    
    -- Indexes
    UNIQUE(id)
);

CREATE INDEX idx_pending_responses_sync ON pending_responses(synced, sync_attempted_at);
CREATE INDEX idx_pending_responses_assessment ON pending_responses(assessment_id, synced);

-- Sync queue (ordered list of operations)
CREATE TABLE sync_queue (
    -- Identity
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    
    -- Operation
    operation_type TEXT NOT NULL, -- 'upload_response', 'submit_assessment'
    operation_data TEXT NOT NULL, -- JSON with operation details
    
    -- References
    pending_response_id TEXT, -- Link to pending_responses if applicable
    
    -- Status
    status TEXT NOT NULL DEFAULT 'pending', -- 'pending', 'in_progress', 'completed', 'failed'
    created_at INTEGER NOT NULL,
    processed_at INTEGER,
    error_message TEXT,
    retry_count INTEGER NOT NULL DEFAULT 0,
    
    -- Indexes
    UNIQUE(id)
);

CREATE INDEX idx_sync_queue_status ON sync_queue(status, created_at);

-- Cache metadata (app-level cache settings)
CREATE TABLE cache_metadata (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL,
    updated_at INTEGER NOT NULL
);
```

### 2.2 Data Model Examples

**Cached Assessment:**
```json
{
  "assessment_id": "a1b2c3d4-...",
  "assessment_version": "2",
  "assessment_data": "{\"id\":\"a1b2c3d4-...\",\"title\":\"Algebra Quiz\",\"questions\":[...]}",
  "downloaded_at": 1698364800,
  "expires_at": 1698451200,
  "size_bytes": 45678,
  "is_completed": false,
  "last_accessed_at": 1698378400
}
```

**Pending Response:**
```json
{
  "id": "resp-12345",
  "student_id": "student-abc",
  "assessment_id": "a1b2c3d4-...",
  "question_id": "q-001",
  "response_data": "{\"answer\":\"42\",\"timeSpentMs\":45000}",
  "answered_at": 1698378400000,
  "synced": false,
  "sync_attempts": 0
}
```

---

## 3. Download Strategy

### 3.1 Assessment Download

**User Flow:**
1. Student browses available assessments
2. Taps "Download" icon (⬇️) next to assessment
3. App shows download progress
4. Assessment marked as "Available Offline" (✓)

**Implementation:**

```csharp
public class OfflineAssessmentService : IOfflineAssessmentService
{
    private readonly IAssessmentApiClient _apiClient;
    private readonly ISQLiteConnection _database;
    
    public async Task<Result> DownloadAssessmentAsync(
        Guid assessmentId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Fetch assessment from API
            var assessmentResult = await _apiClient.GetAssessmentAsync(assessmentId, cancellationToken);
            if (assessmentResult.IsFailure)
                return Result.Failure(assessmentResult.Error);
            
            var assessment = assessmentResult.Value;
            
            // 2. Serialize to JSON
            var assessmentJson = JsonSerializer.Serialize(assessment);
            var sizeBytes = Encoding.UTF8.GetByteCount(assessmentJson);
            
            // 3. Check storage space
            var storageResult = await CheckStorageSpaceAsync(sizeBytes);
            if (storageResult.IsFailure)
                return storageResult;
            
            // 4. Store in SQLite
            var cachedAssessment = new CachedAssessment
            {
                AssessmentId = assessment.Id.ToString(),
                AssessmentVersion = assessment.Version.ToString(),
                AssessmentData = assessmentJson,
                DownloadedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds(),
                SizeBytes = sizeBytes,
                IsCompleted = false
            };
            
            await _database.InsertOrReplaceAsync(cachedAssessment);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to download assessment: {ex.Message}");
        }
    }
    
    private async Task<Result> CheckStorageSpaceAsync(long requiredBytes)
    {
        // Check available storage
        var availableBytes = GetAvailableStorageBytes();
        if (availableBytes < requiredBytes * 2) // Require 2x space for safety
        {
            return Result.Failure("Insufficient storage space");
        }
        
        // Check cache size limit (e.g., 500 MB)
        var currentCacheSize = await GetCacheSizeBytesAsync();
        if (currentCacheSize + requiredBytes > 500_000_000)
        {
            // Evict oldest/least-used assessments
            await EvictOldAssessmentsAsync();
        }
        
        return Result.Success();
    }
}
```

### 3.2 Cache Eviction Policy

**Eviction Rules:**
1. **Expired assessments** - Delete if `expires_at` < now
2. **Completed assessments** - Delete if synced and `is_completed` = true
3. **Least Recently Used (LRU)** - Delete oldest `last_accessed_at`
4. **Manual deletion** - User can delete from "Downloaded" list

**Eviction Priority:**
```
Priority 1: Expired and synced
Priority 2: Completed and synced, not accessed in 7 days
Priority 3: Downloaded but never accessed, older than 3 days
Priority 4: Least recently accessed
```

---

## 4. Offline Mode

### 4.1 Loading Cached Assessment

```csharp
public async Task<Result<Assessment>> LoadAssessmentAsync(
    Guid assessmentId,
    CancellationToken cancellationToken = default)
{
    // 1. Try to load from cache first
    var cached = await _database.Table<CachedAssessment>()
        .Where(ca => ca.AssessmentId == assessmentId.ToString())
        .FirstOrDefaultAsync();
    
    if (cached != null && !IsExpired(cached))
    {
        // Update last accessed
        cached.LastAccessedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _database.UpdateAsync(cached);
        
        // Deserialize from JSON
        var assessment = JsonSerializer.Deserialize<Assessment>(cached.AssessmentData);
        return Result.Success(assessment);
    }
    
    // 2. If not cached or expired, fetch from API
    if (!_connectivityService.IsConnected)
    {
        return Result.Failure("Assessment not available offline");
    }
    
    return await _apiClient.GetAssessmentAsync(assessmentId, cancellationToken);
}
```

### 4.2 Saving Responses Offline

```csharp
public async Task<Result> SaveResponseAsync(
    Guid assessmentId,
    Guid questionId,
    string answerData,
    TimeSpan timeSpent,
    CancellationToken cancellationToken = default)
{
    var responseId = Guid.NewGuid();
    var now = DateTimeOffset.UtcNow;
    
    // Create pending response
    var pendingResponse = new PendingResponse
    {
        Id = responseId.ToString(),
        StudentId = _authService.CurrentUserId.ToString(),
        AssessmentId = assessmentId.ToString(),
        QuestionId = questionId.ToString(),
        ResponseData = JsonSerializer.Serialize(new
        {
            Answer = answerData,
            TimeSpentMs = timeSpent.TotalMilliseconds
        }),
        AnsweredAt = now.ToUnixTimeMilliseconds(),
        Synced = false
    };
    
    // Save to SQLite
    await _database.InsertAsync(pendingResponse);
    
    // Add to sync queue
    await AddToSyncQueueAsync("upload_response", responseId);
    
    // Attempt immediate sync if online
    if (_connectivityService.IsConnected)
    {
        _ = Task.Run(() => ProcessSyncQueueAsync(cancellationToken));
    }
    
    return Result.Success();
}
```

---

## 5. Sync Strategy

### 5.1 Connectivity Detection

```csharp
public class ConnectivityService : IConnectivityService
{
    private readonly IConnectivity _connectivity;
    
    public bool IsConnected => _connectivity.NetworkAccess == NetworkAccess.Internet;
    
    public event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged;
    
    public ConnectivityService(IConnectivity connectivity)
    {
        _connectivity = connectivity;
        _connectivity.ConnectivityChanged += OnConnectivityChanged;
    }
    
    private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
    {
        ConnectivityChanged?.Invoke(this, e);
        
        // Trigger sync when back online
        if (e.NetworkAccess == NetworkAccess.Internet)
        {
            _ = Task.Run(() => TriggerSyncAsync());
        }
    }
}
```

### 5.2 Sync Queue Processing

```csharp
public class SyncService : ISyncService
{
    public async Task<Result> ProcessSyncQueueAsync(CancellationToken cancellationToken = default)
    {
        if (!_connectivityService.IsConnected)
            return Result.Failure("No network connectivity");
        
        // Lock to prevent concurrent sync operations
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            // Get pending sync operations
            var pendingOps = await _database.Table<SyncQueueItem>()
                .Where(sq => sq.Status == "pending")
                .OrderBy(sq => sq.CreatedAt)
                .ToListAsync();
            
            foreach (var op in pendingOps)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                // Mark as in progress
                op.Status = "in_progress";
                await _database.UpdateAsync(op);
                
                // Process operation
                var result = await ProcessOperationAsync(op, cancellationToken);
                
                if (result.IsSuccess)
                {
                    // Mark as completed
                    op.Status = "completed";
                    op.ProcessedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    await _database.UpdateAsync(op);
                    
                    // Update pending response
                    if (!string.IsNullOrEmpty(op.PendingResponseId))
                    {
                        var response = await _database.Table<PendingResponse>()
                            .Where(pr => pr.Id == op.PendingResponseId)
                            .FirstOrDefaultAsync();
                        
                        if (response != null)
                        {
                            response.Synced = true;
                            await _database.UpdateAsync(response);
                        }
                    }
                }
                else
                {
                    // Mark as failed
                    op.Status = "failed";
                    op.ErrorMessage = result.Error;
                    op.RetryCount++;
                    await _database.UpdateAsync(op);
                    
                    // Retry logic
                    if (op.RetryCount < 3)
                    {
                        // Reset to pending for retry
                        op.Status = "pending";
                        await _database.UpdateAsync(op);
                    }
                }
            }
            
            return Result.Success();
        }
        finally
        {
            _syncLock.Release();
        }
    }
    
    private async Task<Result> ProcessOperationAsync(
        SyncQueueItem operation,
        CancellationToken cancellationToken)
    {
        return operation.OperationType switch
        {
            "upload_response" => await UploadResponseAsync(operation, cancellationToken),
            "submit_assessment" => await SubmitAssessmentAsync(operation, cancellationToken),
            _ => Result.Failure($"Unknown operation type: {operation.OperationType}")
        };
    }
}
```

### 5.3 Retry Strategy

**Exponential Backoff:**
```
Attempt 1: Immediate
Attempt 2: Wait 5 seconds
Attempt 3: Wait 30 seconds
Attempt 4: Wait 5 minutes
Attempt 5+: Wait 30 minutes
```

**Retry Conditions:**
- Network errors (timeout, connection refused)
- Server errors (500, 502, 503, 504)
- Rate limiting (429)

**No Retry:**
- Client errors (400, 401, 403, 404)
- Conflict errors (409) - Requires manual resolution

---

## 6. Conflict Resolution

### 6.1 Conflict Scenarios

**Scenario 1: Assessment Modified Server-Side**
- Student downloads assessment version 1
- Teacher updates assessment to version 2
- Student completes offline
- Student syncs

**Resolution:** 
- Detect version mismatch
- Show warning: "This assessment has been updated. Your responses may not be valid."
- Options:
  1. Submit anyway (if questions unchanged)
  2. Discard and re-download updated version
  3. Contact teacher

**Implementation:**
```csharp
private async Task<Result> ValidateAssessmentVersionAsync(
    string assessmentId,
    string cachedVersion,
    CancellationToken cancellationToken)
{
    var currentAssessment = await _apiClient.GetAssessmentAsync(
        Guid.Parse(assessmentId), 
        cancellationToken);
    
    if (currentAssessment.IsSuccess && 
        currentAssessment.Value.Version.ToString() != cachedVersion)
    {
        return Result.Failure($"Assessment version mismatch. Cached: {cachedVersion}, Current: {currentAssessment.Value.Version}");
    }
    
    return Result.Success();
}
```

**Scenario 2: Duplicate Response**
- Student answers question online
- Network drops before sync completes
- Student answers same question again offline
- Both responses queued for sync

**Resolution:**
- Server detects duplicate (same student + assessment + question)
- Use last-write-wins (most recent `answered_at`)
- Return 409 Conflict with message
- App marks duplicate as synced (skip)

**Scenario 3: Assessment Deleted**
- Student downloads assessment
- Teacher deletes assessment from system
- Student completes offline
- Student syncs

**Resolution:**
- Server returns 404 Not Found
- App shows error: "This assessment is no longer available."
- Option to delete local copy
- Responses discarded (cannot submit)

### 6.2 Conflict Resolution UI

```
┌────────────────────────────────────┐
│ ⚠️  Sync Conflict                  │
├────────────────────────────────────┤
│ The assessment "Algebra Quiz" has  │
│ been updated since you downloaded  │
│ it. Your responses may not be      │
│ compatible with the new version.   │
│                                    │
│ What would you like to do?         │
│                                    │
│ [Submit Anyway]                    │
│ [Discard & Re-download]            │
│ [Contact Teacher]                  │
└────────────────────────────────────┘
```

---

## 7. Performance Optimization

### 7.1 Chunked Sync

For large assessments with many questions, sync in batches:

```csharp
public async Task<Result> SyncResponsesInBatchesAsync(
    List<PendingResponse> responses,
    int batchSize = 10,
    CancellationToken cancellationToken = default)
{
    var batches = responses.Chunk(batchSize);
    
    foreach (var batch in batches)
    {
        var batchResult = await _apiClient.SubmitResponseBatchAsync(
            batch.Select(r => new ResponseDto
            {
                Id = Guid.Parse(r.Id),
                QuestionId = Guid.Parse(r.QuestionId),
                ResponseData = r.ResponseData,
                AnsweredAt = DateTimeOffset.FromUnixTimeMilliseconds(r.AnsweredAt)
            }).ToList(),
            cancellationToken);
        
        if (batchResult.IsFailure)
            return batchResult;
        
        // Mark batch as synced
        foreach (var response in batch)
        {
            response.Synced = true;
            await _database.UpdateAsync(response);
        }
    }
    
    return Result.Success();
}
```

### 7.2 Compression

Compress large JSON payloads before storage and transmission:

```csharp
private string CompressJson(string json)
{
    var bytes = Encoding.UTF8.GetBytes(json);
    using var memoryStream = new MemoryStream();
    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
    {
        gzipStream.Write(bytes, 0, bytes.Length);
    }
    return Convert.ToBase64String(memoryStream.ToArray());
}

private string DecompressJson(string compressedBase64)
{
    var compressedBytes = Convert.FromBase64String(compressedBase64);
    using var inputStream = new MemoryStream(compressedBytes);
    using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
    using var outputStream = new MemoryStream();
    gzipStream.CopyTo(outputStream);
    return Encoding.UTF8.GetString(outputStream.ToArray());
}
```

### 7.3 Delta Sync

Only sync changed data:

```csharp
// Track changes
CREATE TABLE response_changes (
    response_id TEXT PRIMARY KEY,
    changed_at INTEGER NOT NULL,
    change_type TEXT NOT NULL -- 'created', 'updated', 'deleted'
);

// Sync only changes since last sync
var lastSyncTime = await GetLastSyncTimeAsync();
var changes = await _database.Table<ResponseChange>()
    .Where(rc => rc.ChangedAt > lastSyncTime)
    .ToListAsync();
```

---

## 8. Storage Management

### 8.1 Storage Limits

| Item | Limit | Reason |
|------|-------|--------|
| Total cache size | 500 MB | Balance usability vs storage |
| Single assessment | 10 MB | Prevent abuse |
| Max cached assessments | 50 | Reasonable for typical use |
| Pending responses | Unlimited | Critical data |
| Cache expiration | 7 days | Prevent stale data |

### 8.2 User-Visible Storage Info

```
┌────────────────────────────────────┐
│ Offline Storage                    │
├────────────────────────────────────┤
│ Used: 127 MB / 500 MB              │
│ [▓▓▓▓░░░░░░] 25%                   │
│                                    │
│ Downloaded Assessments: 12         │
│ Pending Sync: 3 responses          │
│                                    │
│ [Clear Cache]  [Settings]          │
└────────────────────────────────────┘
```

---

## 9. Testing Strategy

### 9.1 Unit Tests

- Cache storage/retrieval
- Sync queue processing
- Conflict detection
- Expiration logic
- Compression/decompression

### 9.2 Integration Tests

- End-to-end offline workflow
- Sync with real API
- Connectivity change handling
- Multiple concurrent syncs

### 9.3 Manual Testing Scenarios

1. **Airplane Mode Test:**
   - Download assessment
   - Enable airplane mode
   - Complete assessment
   - Disable airplane mode
   - Verify sync

2. **Intermittent Connectivity:**
   - Start assessment online
   - Disconnect randomly during assessment
   - Reconnect randomly
   - Verify all responses synced

3. **Conflict Test:**
   - Download assessment
   - Modify assessment on server
   - Complete offline
   - Sync and handle conflict

4. **Storage Full:**
   - Fill cache to limit
   - Attempt to download
   - Verify eviction works

---

## 10. Monitoring &amp; Analytics

### 10.1 Metrics to Track

- **Usage:**
  - Assessments downloaded per user
  - Offline completion rate
  - Average offline session duration
  
- **Performance:**
  - Sync queue processing time
  - Sync success rate
  - Conflict rate
  
- **Storage:**
  - Average cache size per user
  - Cache hit rate
  - Eviction frequency

### 10.2 Logging

```csharp
_logger.LogInformation(
    "Assessment downloaded for offline use. " +
    "AssessmentId: {AssessmentId}, Size: {SizeKB} KB, ExpiresAt: {ExpiresAt}",
    assessmentId,
    sizeBytes / 1024,
    expiresAt);

_logger.LogWarning(
    "Sync conflict detected. AssessmentId: {AssessmentId}, " +
    "CachedVersion: {CachedVersion}, CurrentVersion: {CurrentVersion}",
    assessmentId,
    cachedVersion,
    currentVersion);

_logger.LogError(
    ex,
    "Failed to sync response. ResponseId: {ResponseId}, Attempt: {Attempt}",
    responseId,
    retryCount);
```

---

## 11. Implementation Phases

### Phase 1: Basic Offline (2 weeks)
- SQLite database setup
- Download and cache assessments
- Load cached assessments
- Basic eviction (expired only)

### Phase 2: Response Storage (1 week)
- Save responses offline
- Sync queue implementation
- Basic sync (no conflicts)

### Phase 3: Robust Sync (2 weeks)
- Connectivity detection
- Automatic sync on reconnect
- Retry logic with exponential backoff
- Batch sync

### Phase 4: Conflict Resolution (1 week)
- Version checking
- Conflict detection
- User-facing conflict resolution UI

### Phase 5: Optimization (1 week)
- Compression
- Delta sync
- Performance tuning
- Cache management UI

**Total Estimated Effort:** 7 weeks

---

## 12. Risks &amp; Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Data loss if device lost/reset | High | Low | Regular sync, encourage online use |
| Storage exhaustion | Medium | Medium | Eviction policy, user warnings |
| Sync conflicts | Medium | Medium | Clear conflict resolution UI |
| Stale cached data | Low | High | Expiration, version checking |
| Security (data on device) | High | Low | Encrypt SQLite database |

---

## 13. Security Considerations

### 13.1 Encryption

Encrypt SQLite database using SQLCipher:

```csharp
var connectionString = new SQLiteConnectionString(
    databasePath,
    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex,
    storeDateTimeAsTicks: true,
    key: GetEncryptionKey());
```

### 13.2 Key Management

- Use platform-specific secure storage (Keychain on iOS, KeyStore on Android)
- Rotate keys periodically
- Derive key from user credentials (requires login)

---

## Conclusion

This offline sync architecture enables students to use EduMind.AI in low-connectivity environments while ensuring data integrity and a smooth user experience. The phased implementation allows for iterative development and testing.

**Recommendation:** Implement in phases, prioritizing basic offline capability first. Gather user feedback on sync behavior and conflict resolution UX.

**Next Steps:**
1. Review and approve this architecture
2. Set up SQLite database with schema
3. Implement download and cache logic
4. Implement sync queue processing
5. Test with real-world connectivity scenarios
