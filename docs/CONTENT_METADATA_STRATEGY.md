# Content Metadata Strategy

**Date**: October 15, 2025  
**Status**: ✅ Implemented  
**Phase**: Post-Phase 5 Enhancement

---

## Overview

EduMind.AI uses a **flexible metadata approach** instead of a rigid Board→Grade→Subject→Chapter→Module→Topic hierarchy. This design choice aligns with our strategic focus as an **intelligent assessment platform** rather than a full-featured LMS.

### Strategic Rationale

Based on our [Gap Analysis](./GAP_ANALYSIS.md):

- **Content Management Parity**: 20% (not core strength)
- **Assessment & Analytics Parity**: 85% (core differentiator)
- **Market Position**: Integration platform, not LMS replacement

**Decision**: Add flexible metadata without complex hierarchy management.

---

## Implementation

### ✅ Added to Models

#### 1. Course Model (`Course.cs`)

```csharp
public record Course
{
    // Existing fields...
    public required Subject Subject { get; init; }
    public required GradeLevel GradeLevel { get; init; }
    public IReadOnlyList<string> LearningObjectives { get; init; } = [];
    public IReadOnlyList<string> Topics { get; init; } = [];
    
    // NEW: Optional metadata fields
    
    /// <summary>
    /// Optional: Board/Curriculum name (e.g., "CBSE", "ICSE", "IB", "State Board")
    /// </summary>
    public string? BoardName { get; init; }
    
    /// <summary>
    /// Optional: Module name (e.g., "Algebra", "Thermodynamics", "Cell Biology")
    /// </summary>
    public string? ModuleName { get; init; }
    
    /// <summary>
    /// Optional: Flexible metadata dictionary
    /// Example: { "externalId": "google-classroom-123", "boardCode": "CBSE-2024" }
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = 
        new Dictionary<string, string>();
}
```

#### 2. Question Model (`Question.cs`)

```csharp
public record Question
{
    // Existing fields...
    public required Subject Subject { get; init; }
    public required GradeLevel GradeLevel { get; init; }
    public IReadOnlyList<string> LearningObjectives { get; init; } = [];
    public IReadOnlyList<string> Topics { get; init; } = [];
    
    // NEW: Optional metadata fields (same as Course)
    
    public string? BoardName { get; init; }
    public string? ModuleName { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = 
        new Dictionary<string, string>();
}
```

---

## Usage Examples

### Example 1: CBSE Board Content

```csharp
var course = new Course
{
    Id = Guid.NewGuid(),
    Name = "8th Grade Mathematics",
    Code = "MATH-8",
    Subject = Subject.Mathematics,
    GradeLevel = GradeLevel.Grade8,
    Description = "Comprehensive mathematics course for 8th grade",
    
    // Board/Module metadata
    BoardName = "CBSE",
    ModuleName = "Algebra",
    
    Metadata = new Dictionary<string, string>
    {
        { "boardCode", "CBSE-2024" },
        { "term", "Semester 1" },
        { "unit", "Unit 3" }
    }.AsReadOnly(),
    
    LearningObjectives = new[]
    {
        "Solve linear equations",
        "Factor quadratic expressions"
    },
    
    IsActive = true,
    CreatedAt = DateTimeOffset.UtcNow,
    UpdatedAt = DateTimeOffset.UtcNow
};
```

### Example 2: International Baccalaureate (IB)

```csharp
var question = new Question
{
    Id = Guid.NewGuid(),
    CourseId = courseId,
    QuestionText = "Solve for x: 2x + 5 = 15",
    QuestionType = QuestionType.ShortAnswer,
    Subject = Subject.Mathematics,
    GradeLevel = GradeLevel.Grade10,
    DifficultyLevel = DifficultyLevel.Medium,
    CorrectAnswer = "5",
    Points = 2,
    
    // Board/Module metadata
    BoardName = "IB",
    ModuleName = "Linear Algebra",
    
    Metadata = new Dictionary<string, string>
    {
        { "ibCode", "MYP-5-Math" },
        { "bloomsLevel", "3" },
        { "estimatedTimeSeconds", "120" }
    }.AsReadOnly(),
    
    LearningObjectives = new[] { "Solve linear equations" },
    Topics = new[] { "Algebra", "Equations" },
    
    IsActive = true,
    CreatedAt = DateTimeOffset.UtcNow,
    UpdatedAt = DateTimeOffset.UtcNow
};
```

### Example 3: External LMS Integration

```csharp
var course = new Course
{
    // ... standard fields ...
    
    BoardName = "State Board",
    ModuleName = "Mechanics",
    
    Metadata = new Dictionary<string, string>
    {
        { "googleClassroomId", "course_12345" },
        { "canvasId", "canvas_67890" },
        { "syncSource", "google-classroom" },
        { "lastSyncedAt", "2025-10-15T10:00:00Z" }
    }.AsReadOnly()
};
```

---

## API Filtering

### Query by Board

```csharp
// Repository method
public async Task<Result<IReadOnlyList<Course>>> GetByBoardAsync(
    string boardName,
    CancellationToken cancellationToken = default)
{
    var courses = await _context.Courses
        .Where(c => c.BoardName == boardName && c.IsActive)
        .ToListAsync(cancellationToken);
        
    return Result.Success<IReadOnlyList<Course>>(courses.AsReadOnly());
}
```

### API Endpoint Example

```csharp
// GET /api/v1/courses?board=CBSE&module=Algebra&grade=8
[HttpGet]
public async Task<IActionResult> GetCourses(
    [FromQuery] string? board = null,
    [FromQuery] string? module = null,
    [FromQuery] GradeLevel? grade = null)
{
    var query = _context.Courses.Where(c => c.IsActive);
    
    if (!string.IsNullOrEmpty(board))
        query = query.Where(c => c.BoardName == board);
        
    if (!string.IsNullOrEmpty(module))
        query = query.Where(c => c.ModuleName == module);
        
    if (grade.HasValue)
        query = query.Where(c => c.GradeLevel == grade.Value);
        
    var courses = await query.ToListAsync();
    return Ok(courses);
}
```

---

## Benefits

### ✅ **Flexibility**

- No rigid hierarchy to maintain
- Schools can use their own organizational structure
- Metadata dictionary allows custom attributes

### ✅ **Simplicity**

- Optional fields - no breaking changes
- Works with existing Subject/Grade hierarchy
- Easy to query and filter

### ✅ **Integration-Friendly**

- Map external LMS content via metadata
- Store external IDs without schema changes
- Support multiple boards simultaneously

### ✅ **Future-Proof**

- Add new metadata keys without migrations
- Schools define their own taxonomy
- Can add full hierarchy later if needed

---

## When to Use Each Field

| Field | Use Case | Example Values |
|-------|----------|----------------|
| **BoardName** | Educational board/curriculum system | "CBSE", "ICSE", "IB", "State Board", "IGCSE" |
| **ModuleName** | Grouping topics within subject | "Algebra", "Thermodynamics", "Cell Biology", "Grammar" |
| **Metadata["key"]** | Custom attributes, external IDs | `{"externalId": "123", "term": "Q1"}` |

---

## Migration Strategy

### Database Migration (Future)

When we add database support for metadata:

```csharp
public partial class AddContentMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "board_name",
            table: "courses",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
            
        migrationBuilder.AddColumn<string>(
            name: "module_name",
            table: "courses",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);
            
        migrationBuilder.AddColumn<string>(
            name: "metadata",
            table: "courses",
            type: "jsonb",
            nullable: false,
            defaultValue: "{}");
            
        // Create indexes for common queries
        migrationBuilder.CreateIndex(
            name: "ix_courses_board_name",
            table: "courses",
            column: "board_name");
            
        migrationBuilder.CreateIndex(
            name: "ix_courses_module_name",
            table: "courses",
            column: "module_name");
            
        // Same for questions table
        // ...
    }
}
```

---

## Alternative: Full Hierarchy (Not Recommended Now)

If customer demand grows for full content management, we could implement:

```csharp
// Future: Only if needed
public record Board
{
    public Guid Id { get; init; }
    public string Name { get; init; } // "CBSE", "IB"
    public string Code { get; init; }
}

public record Module
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public string Name { get; init; }
    public int OrderIndex { get; init; }
}

// Then: Course → Module → Topic → LearningObjective
```

**Estimated Effort**: 2-3 weeks  
**Priority**: LOW (wait for customer demand)

---

## Recommendations

### ✅ **Do Now**

1. Use optional metadata fields for board/module tagging
2. Store external LMS IDs in metadata dictionary
3. Add API filters for board/module queries
4. Document board naming conventions

### ⏳ **Do Later** (if demand exists)

1. Build API for external content mapping
2. Add curriculum management dashboard
3. Implement full hierarchy (separate service)

### ❌ **Don't Do**

1. Build video hosting platform
2. Replicate full LMS features
3. Complex content approval workflows

---

## Success Metrics

**Short-term** (1-3 months):

- ✅ Schools can tag content by board
- ✅ External LMS content can be mapped
- ✅ Filtering by board/module works in API

**Long-term** (6-12 months):

- Integration with 3+ major LMS platforms
- 80%+ of schools use board tagging
- No requests for full content hierarchy

---

## Related Documents

- [Gap Analysis](./GAP_ANALYSIS.md) - Competitive positioning
- [A2A Agent Integration Plan](./A2A_AGENT_INTEGRATION_PLAN.md) - Assessment focus
- [OLLAMA Integration Complete](./OLLAMA_INTEGRATION_COMPLETE.md) - AI capabilities

---

**Status**: ✅ **IMPLEMENTED**  
**Next Steps**: Monitor customer usage, add API endpoints if needed  
**Review Date**: December 15, 2025
