# 09a. Core Assessment Features

**Document Version:** 1.0  
**Last Updated:** October 24, 2025  
**Status:** Active Development

---

## Table of Contents

1. [Overview](#overview)
2. [Question Management](#question-management)
3. [Assessment Engine](#assessment-engine)
4. [Scoring and Evaluation](#scoring-and-evaluation)
5. [Adaptive Testing](#adaptive-testing)
6. [Content Metadata](#content-metadata)
7. [Feature Status Summary](#feature-status-summary)

---

## Overview

This document catalogs the core assessment features of EduMind.AI, including question types, assessment creation, scoring mechanisms, and adaptive testing capabilities. These features form the foundation of the intelligent assessment platform.

### Related Documents

- [03-domain-model.md](03-domain-model.md) - Assessment entities and relationships
- [04-application-components.md](04-application-components.md) - Assessment engine architecture
- [06-external-integrations.md](06-external-integrations.md) - LLM integration for evaluation

---

## Question Management

### Question Types (9 Types)

**Status:** ✅ **All Implemented**

The system supports 9 distinct question types to accommodate diverse assessment needs across all subjects.

| Question Type | Status | Description | Implementation |
|--------------|--------|-------------|----------------|
| **MultipleChoice** | ✅ Complete | Single correct answer from options | `QuestionType.MultipleChoice` |
| **MultipleSelect** | ✅ Complete | Multiple correct answers | `QuestionType.MultipleSelect` |
| **TrueFalse** | ✅ Complete | Binary true/false questions | `QuestionType.TrueFalse` |
| **ShortAnswer** | ✅ Complete | Brief text response (1-2 sentences) | `QuestionType.ShortAnswer` |
| **FillInBlank** | ✅ Complete | Complete missing text | `QuestionType.FillInBlank` |
| **MathExpression** | ✅ Complete | Mathematical equations and formulas | `QuestionType.MathExpression` |
| **Essay** | ✅ Complete | Extended written response | `QuestionType.Essay` |
| **CodeSnippet** | ✅ Complete | Programming code responses | `QuestionType.CodeSnippet` |
| **Matching** | ✅ Complete | Pair items from two lists | `QuestionType.Matching` |

**Files:**

- `src/AcademicAssessment.Core/Entities/Question.cs`
- `src/AcademicAssessment.Core/Enums/QuestionType.cs`

**Sample Questions:** 171 questions across all types in database

### Question Content Features

#### ✅ Rich Content Support

**Status:** Fully Implemented

- **Markdown Rendering:** Questions support Markdown formatting via Markdig library
- **LaTeX/KaTeX Math:** Mathematical equations rendered with KaTeX (inline `$...$` and block `$$...$$`)
- **Code Highlighting:** Syntax highlighting via highlight.js for programming questions
- **Images:** Support for embedded images (planned)
- **Diagrams:** Support for diagrams and charts (planned)

**Files:**

- `src/AcademicAssessment.StudentApp/Components/QuestionRenderer.razor`
- `src/AcademicAssessment.StudentApp/wwwroot/js/assessment-ui.js`
- `src/AcademicAssessment.StudentApp/wwwroot/lib/katex/` (local assets)
- `src/AcademicAssessment.StudentApp/wwwroot/lib/highlight/` (local assets)

#### ✅ Question Metadata

**Status:** Fully Implemented

Each question includes comprehensive metadata for intelligent assessment:

```csharp
public record Question
{
    public Guid Id { get; init; }
    public string Text { get; init; }              // Question content
    public QuestionType Type { get; init; }        // Question type
    public Subject Subject { get; init; }          // Subject area
    public DifficultyLevel Difficulty { get; init; } // Difficulty (Easy/Medium/Hard/Expert)
    public string? Hint { get; init; }             // Optional hint
    public double IrtDifficulty { get; init; }     // IRT difficulty parameter (-3 to +3)
    public double IrtDiscrimination { get; init; } // IRT discrimination (0.5 to 2.5)
    public double IrtGuessing { get; init; }       // IRT guessing parameter (0 to 0.35)
    
    // Content Metadata (Phase 5)
    public string? BoardName { get; init; }        // Educational board (e.g., "CBSE", "IB")
    public string? ModuleName { get; init; }       // Module/unit (e.g., "Algebra", "Mechanics")
    public Dictionary<string, string>? Metadata { get; init; } // Flexible JSONB metadata
}
```

**Database:** `questions` table with 171 questions

### Question Bank Management

#### ✅ Subject-Specific Questions

**Status:** Fully Implemented

Questions organized by 5 core subjects:

| Subject | Question Count | Status |
|---------|---------------|--------|
| **Mathematics** | ~35 questions | ✅ Seeded |
| **Physics** | ~35 questions | ✅ Seeded |
| **Chemistry** | ~35 questions | ✅ Seeded |
| **Biology** | ~35 questions | ✅ Seeded |
| **English** | ~35 questions | ✅ Seeded |

**Files:**

- `scripts/seed-demo-data-final.sql` - Comprehensive seed data
- `scripts/add-questions-and-responses.sql` - Additional questions

#### 📋 Question Authoring UI

**Status:** Planned (Phase 4)

- Web-based question editor
- WYSIWYG Markdown editor
- Math equation builder
- Metadata tagging interface
- Bulk import from CSV/Excel
- Question preview

---

## Assessment Engine

### Assessment Creation

#### ✅ Assessment Entity

**Status:** Fully Implemented

```csharp
public record Assessment
{
    public Guid Id { get; init; }
    public string Title { get; init; }
    public string? Description { get; init; }
    public int DurationMinutes { get; init; }
    public int TotalQuestions { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public bool IsActive { get; init; }
    
    // Multi-tenant isolation
    public Guid? SchoolId { get; init; }
    public Guid? ClassId { get; init; }
}
```

**Database:** `assessments` table with 89 assessments

**Files:**

- `src/AcademicAssessment.Core/Entities/Assessment.cs`
- `src/AcademicAssessment.Infrastructure/Repositories/AssessmentRepository.cs`

#### ✅ Assessment-Question Relationship

**Status:** Fully Implemented

Many-to-many relationship between assessments and questions:

```sql
CREATE TABLE assessment_questions (
    assessment_id UUID NOT NULL REFERENCES assessments(id),
    question_id UUID NOT NULL REFERENCES questions(id),
    sequence_number INT NOT NULL,
    PRIMARY KEY (assessment_id, question_id)
);
```

**Features:**

- Custom question ordering via `sequence_number`
- Same question can appear in multiple assessments
- Flexible question pool management

### Assessment Delivery

#### ✅ Student Assessment Sessions

**Status:** Fully Implemented

Student assessment tracking with comprehensive state management:

```csharp
public record StudentAssessment
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public Guid AssessmentId { get; init; }
    public AssessmentStatus Status { get; init; }      // NotStarted/InProgress/Completed
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public int? Score { get; init; }
    public int? MaxScore { get; init; }
    public double? PercentageScore { get; init; }
    public TimeSpan? TimeTaken { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}
```

**Database:** `student_assessments` table with 295 sessions

**Features:**

- Session state persistence
- Progress tracking
- Time tracking
- Score calculation
- Metadata for flexible extensions

#### ✅ Response Tracking

**Status:** Fully Implemented

Individual question responses tracked with scoring:

```csharp
public record StudentResponse
{
    public Guid Id { get; init; }
    public Guid StudentAssessmentId { get; init; }
    public Guid QuestionId { get; init; }
    public string? ResponseText { get; init; }
    public List<Guid>? SelectedOptionIds { get; init; }
    public DateTime SubmittedAt { get; init; }
    public bool IsCorrect { get; init; }
    public int PointsEarned { get; init; }
    public int MaxPoints { get; init; }
    public string? Feedback { get; init; }
    public double? ConfidenceScore { get; init; }      // 0-1 confidence from LLM
}
```

**Database:** `student_responses` table with 1,179 responses

**Features:**

- Individual response storage
- Auto-save support (every 30 seconds)
- Partial credit support (`PointsEarned` vs `MaxPoints`)
- LLM confidence tracking

#### ✅ Timer and Auto-Save

**Status:** Fully Implemented

Real-time assessment session management:

**Features:**

- Countdown timer display (hours or minutes:seconds format)
- Session expiry detection with UI lockdown
- Auto-save with semaphore-based coordination (30-second interval)
- Debounced save operations to prevent conflicts
- Visual save status indicators ("Saving...", "Saved", "Save Failed")

**Files:**

- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentSession.razor.cs`
- Timer: `System.Threading.PeriodicTimer`
- Auto-save: `SemaphoreSlim` for thread-safe coordination

**Implementation Details:**

```csharp
// Auto-save coordination
private readonly SemaphoreSlim _saveSemaphore = new(1, 1);
private PeriodicTimer? _autoSaveTimer;
private PeriodicTimer? _countdownTimer;

// Auto-save every 30 seconds
_autoSaveTimer = new PeriodicTimer(TimeSpan.FromSeconds(30));
await foreach (var _ in _autoSaveTimer.Tick(CancellationToken.None))
{
    await SaveProgressAsync();
}
```

---

## Scoring and Evaluation

### Automatic Scoring

#### ✅ Exact Match Scoring

**Status:** Fully Implemented

Deterministic scoring for objective question types:

| Question Type | Scoring Method | Status |
|--------------|----------------|--------|
| MultipleChoice | Exact option match | ✅ Complete |
| MultipleSelect | All correct options selected, no incorrect | ✅ Complete |
| TrueFalse | Boolean match | ✅ Complete |
| FillInBlank | String match (case-insensitive) | ✅ Complete |
| MathExpression | Symbolic math comparison | ✅ Complete |

**Files:**

- `src/AcademicAssessment.Agents/Agents/*/EvaluateResponseAsync()` methods
- Fallback logic when LLM unavailable

#### ✅ LLM-Based Semantic Scoring

**Status:** Fully Implemented (OLLAMA)

Intelligent scoring for subjective responses using local LLM:

**Features:**

- Semantic similarity evaluation (not just exact match)
- Confidence scoring (0-1 scale)
- Partial credit support
- Handles spelling variations and paraphrasing
- Graceful fallback to exact match if LLM fails

**Example:**

```
Expected Answer: "4"
Student Response: "four"
LLM Score: 0.70 (70% confidence)
Result: Partial credit awarded
```

**Performance:**

- Response time: 20-25 seconds per evaluation (OLLAMA llama3.2:3b)
- Accuracy: 95%+ across all subjects (per historical docs)

**Files:**

- `src/AcademicAssessment.Infrastructure/ExternalServices/OllamaService.cs`
- `src/AcademicAssessment.Agents/Agents/*/EvaluateResponseAsync()`

#### ⚠️ Azure OpenAI Integration

**Status:** Planned (Configuration Ready)

- Faster response times (<5 seconds)
- Higher accuracy for complex essays
- Pay-per-use model (~$0.01/evaluation)
- Configuration structure implemented

**Files:**

- `src/AcademicAssessment.Web/Program.cs` (lines 437-438 with TODO comment)
- `appsettings.json` - Azure OpenAI section defined

### Score Aggregation

#### ✅ Assessment Scoring

**Status:** Fully Implemented

Comprehensive score calculation:

```csharp
// StudentAssessment scoring fields
public int? Score { get; init; }              // Total points earned
public int? MaxScore { get; init; }            // Total possible points
public double? PercentageScore { get; init; } // Percentage (0-100)
public TimeSpan? TimeTaken { get; init; }      // Duration
```

**Calculation:**

- Sum of `PointsEarned` across all responses = `Score`
- Sum of `MaxPoints` across all questions = `MaxScore`
- `PercentageScore` = (Score / MaxScore) * 100

**Files:**

- Assessment submission logic in `AssessmentController.cs`

#### ✅ Subject-Wise Performance

**Status:** Fully Implemented

Performance breakdown by subject:

```csharp
public record SubjectPerformanceDto
{
    public string Subject { get; init; }
    public int QuestionsAttempted { get; init; }
    public int CorrectAnswers { get; init; }
    public double AccuracyPercentage { get; init; }
    public string PerformanceLevel { get; init; } // Excellent/Good/Needs Improvement
}
```

**Features:**

- Per-subject accuracy tracking
- Performance level categorization
- Trend analysis over time

**Files:**

- `src/AcademicAssessment.Web/Controllers/StudentAnalyticsController.cs`
- 7 analytics endpoints implemented

---

## Adaptive Testing

### IRT (Item Response Theory) Parameters

#### ✅ IRT Model Implementation

**Status:** Fully Implemented

Three-parameter logistic (3PL) IRT model for adaptive testing:

**Parameters:**

1. **Difficulty (b)**: Range -3 to +3
   - -3: Very easy
   - 0: Average difficulty
   - +3: Very hard

2. **Discrimination (a)**: Range 0.5 to 2.5
   - Low (0.5-1.0): Poor differentiation
   - Medium (1.0-1.5): Good differentiation
   - High (1.5-2.5): Excellent differentiation

3. **Guessing (c)**: Range 0 to 0.35
   - 0: No guessing advantage (e.g., essay)
   - 0.25: 25% chance (4-option MCQ)
   - 0.33: 33% chance (3-option MCQ)

**Database:** All 171 questions have IRT parameters

**Files:**

- `src/AcademicAssessment.Core/Entities/Question.cs`
- IRT fields: `IrtDifficulty`, `IrtDiscrimination`, `IrtGuessing`

#### ⚠️ Adaptive Question Selection

**Status:** Partially Implemented

**Current Implementation:**

- Priority scoring based on 4 factors:
  - Never-assessed subjects (+100 points)
  - Recency (up to +40 points based on days since last assessment)
  - Declining performance (+30 points)
  - Low mastery (+28 points)
- IRT-based difficulty adjustment (match to student ability)

**Limitations:**

- Static assessment structure (questions pre-assigned)
- No dynamic question selection during assessment
- IRT parameters not used in real-time adaptation

**Planned:**

- Real-time question selection based on previous responses
- Dynamic difficulty adjustment during assessment
- Computerized Adaptive Testing (CAT) algorithms

**Files:**

- `src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs`
- `SelectNextSubjectForAssessment()` method (line 380+)

### Student Ability Estimation

#### 📋 Ability Tracking

**Status:** Planned

**Planned Features:**

- Theta (θ) estimation per subject (ability score -3 to +3)
- Bayesian updating after each response
- Ability trending over time
- Confidence intervals for estimates

**Database Schema:** Not yet implemented

**Related Work:**

- IRT parameters exist on questions
- Response history available for calculation
- Analytics infrastructure ready

#### 📋 Mastery Level Calculation

**Status:** Conceptual

**Planned Features:**

- Mastery thresholds per topic:
  - Novice: θ < -1.0
  - Intermediate: -1.0 ≤ θ < 0.5
  - Advanced: 0.5 ≤ θ < 1.5
  - Expert: θ ≥ 1.5
- Topic-level granularity (beyond subject level)
- Prerequisite mapping

---

## Content Metadata

### Educational Board Alignment

#### ✅ Board Metadata

**Status:** Database Schema Implemented, Data Population Pending

**Schema:**

```sql
ALTER TABLE questions ADD COLUMN board_name VARCHAR(100);
ALTER TABLE questions ADD COLUMN module_name VARCHAR(200);
CREATE INDEX idx_questions_board ON questions(board_name);
CREATE INDEX idx_questions_module ON questions(module_name);
```

**Supported Boards (Planned):**

- CBSE (Central Board of Secondary Education)
- ICSE (Indian Certificate of Secondary Education)
- IB (International Baccalaureate)
- Cambridge IGCSE
- State boards (customizable)

**Files:**

- Migration: `20251015212949_AddContentMetadataFields.cs`
- Documentation: `docs/architecture/CONTENT_METADATA_STRATEGY.md`

#### ✅ Flexible Metadata (JSONB)

**Status:** Fully Implemented

PostgreSQL JSONB column for extensible metadata:

**Schema:**

```sql
ALTER TABLE questions ADD COLUMN metadata JSONB;
ALTER TABLE courses ADD COLUMN metadata JSONB;
```

**Use Cases:**

- Custom tags (e.g., "problem-solving", "conceptual")
- Learning objectives (e.g., "LO-1.2.3")
- Standards alignment (e.g., "CCSS.MATH.CONTENT.8.F.A.1")
- Bloom's taxonomy levels
- Prerequisite topics

**Example:**

```json
{
  "bloom_level": "analyze",
  "tags": ["problem-solving", "real-world"],
  "standards": ["CCSS.MATH.CONTENT.8.F.A.1"],
  "prerequisites": ["linear-equations", "slope-concept"]
}
```

---

## Feature Status Summary

### Completed Features (✅)

**Assessment Core:**

- ✅ 9 question types implemented
- ✅ Rich content rendering (Markdown, KaTeX, highlight.js)
- ✅ Assessment creation and management
- ✅ Student assessment sessions with state tracking
- ✅ Response tracking with 1,179+ responses
- ✅ Timer and auto-save (30-second interval)
- ✅ Session expiry detection

**Scoring:**

- ✅ Exact match scoring for objective questions
- ✅ LLM-based semantic scoring (OLLAMA)
- ✅ Partial credit support
- ✅ Confidence scoring
- ✅ Assessment-level score aggregation
- ✅ Subject-wise performance tracking

**IRT and Adaptive Testing:**

- ✅ IRT parameters on all questions (difficulty, discrimination, guessing)
- ✅ Priority-based subject selection algorithm
- ✅ 4-factor scoring for next assessment

**Content Metadata:**

- ✅ Database schema for board/module metadata
- ✅ JSONB flexible metadata columns
- ✅ Indexes for efficient filtering

**Data:**

- ✅ 171 questions across 5 subjects
- ✅ 89 assessments
- ✅ 295 student assessment sessions
- ✅ 1,179 student responses

### In Progress (⚠️)

- ⚠️ Azure OpenAI integration (configuration ready, implementation pending)
- ⚠️ Real-time adaptive question selection
- ⚠️ Content metadata population (schema ready, data pending)

### Planned Features (📋)

**Content Management:**

- 📋 Question authoring UI (WYSIWYG editor)
- 📋 Bulk question import (CSV/Excel)
- 📋 Question preview and validation
- 📋 Image and diagram embedding

**Advanced Adaptive Testing:**

- 📋 Real-time CAT (Computerized Adaptive Testing)
- 📋 Student ability (theta) estimation
- 📋 Dynamic difficulty adjustment during assessment
- 📋 Mastery level calculation
- 📋 Topic-level granularity

**Enhanced Scoring:**

- 📋 Rubric-based scoring for essays
- 📋 Peer review integration
- 📋 Manual grading workflow for teachers
- 📋 Score normalization across cohorts

**Board Alignment:**

- 📋 Populate board metadata for all questions
- 📋 Board-specific question filtering
- 📋 Standards alignment mapping
- 📋 Learning objectives tracking

---

## Implementation Locations

### Key Files

**Core Entities:**

- `src/AcademicAssessment.Core/Entities/Question.cs`
- `src/AcademicAssessment.Core/Entities/Assessment.cs`
- `src/AcademicAssessment.Core/Entities/StudentAssessment.cs`
- `src/AcademicAssessment.Core/Entities/StudentResponse.cs`

**Enumerations:**

- `src/AcademicAssessment.Core/Enums/QuestionType.cs`
- `src/AcademicAssessment.Core/Enums/Subject.cs`
- `src/AcademicAssessment.Core/Enums/DifficultyLevel.cs`
- `src/AcademicAssessment.Core/Enums/AssessmentStatus.cs`

**Repositories:**

- `src/AcademicAssessment.Infrastructure/Repositories/QuestionRepository.cs`
- `src/AcademicAssessment.Infrastructure/Repositories/AssessmentRepository.cs`
- `src/AcademicAssessment.Infrastructure/Repositories/StudentAssessmentRepository.cs`
- `src/AcademicAssessment.Infrastructure/Repositories/StudentResponseRepository.cs`

**Assessment Delivery:**

- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentSession.razor`
- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentSession.razor.cs`
- `src/AcademicAssessment.StudentApp/Components/QuestionRenderer.razor`
- `src/AcademicAssessment.StudentApp/Components/Answers/*.razor`
- `src/AcademicAssessment.StudentApp/wwwroot/js/assessment-ui.js`

**Scoring:**

- `src/AcademicAssessment.Agents/Agents/MathematicsAssessmentAgent.cs`
- `src/AcademicAssessment.Agents/Agents/PhysicsAssessmentAgent.cs`
- `src/AcademicAssessment.Agents/Agents/ChemistryAssessmentAgent.cs`
- `src/AcademicAssessment.Agents/Agents/BiologyAssessmentAgent.cs`
- `src/AcademicAssessment.Agents/Agents/EnglishAssessmentAgent.cs`

**Adaptive Testing:**

- `src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs`
- `SelectNextSubjectForAssessment()` method
- IRT-based difficulty matching logic

**API Endpoints:**

- `src/AcademicAssessment.Web/Controllers/AssessmentController.cs`
- `src/AcademicAssessment.Web/Controllers/StudentAnalyticsController.cs`

**Database:**

- `src/AcademicAssessment.Infrastructure/Data/ApplicationDbContext.cs`
- Migrations: `src/AcademicAssessment.Infrastructure/Migrations/`
- Seed data: `scripts/seed-demo-data-final.sql`

---

## Related Documentation

- **[03-domain-model.md](03-domain-model.md)** - Assessment entity relationships
- **[04-application-components.md](04-application-components.md)** - Component architecture
- **[06-external-integrations.md](06-external-integrations.md)** - OLLAMA LLM integration
- **[09b-agent-orchestration-features.md](09b-agent-orchestration-features.md)** - Multi-agent coordination
- **[09c-user-interface-features.md](09c-user-interface-features.md)** - Student UI features
- **[09d-analytics-reporting-features.md](09d-analytics-reporting-features.md)** - Performance analytics

---

**Document Status:** Complete  
**Last Review:** October 24, 2025  
**Next Review:** After Week 3 UI completion
