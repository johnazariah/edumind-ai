# Story 009: Build Question Authoring UI for Teachers

**Priority:** P1 - Production Quality  
**Status:** Ready for Implementation  
**Effort:** Large (2-3 weeks)  
**Dependencies:** None

---

## Problem Statement

Teachers currently have **no user interface** to create or manage assessment questions. The only way to add questions is by:

1. Manually editing PostgreSQL database
2. Running SQL insert scripts
3. Requesting developer assistance

**Current Pain Points:**

- Teachers cannot create custom questions for their classes
- No question preview before saving
- No metadata tagging (difficulty, subject, bloom's taxonomy)
- Cannot bulk import questions from existing content
- No question bank management or organization

**Business Impact:**

- Poor teacher experience limits platform adoption
- Dependency on developers for content creation
- Cannot scale content library efficiently
- Teachers cannot customize assessments for their students
- Competitive disadvantage vs platforms with authoring tools (Kahoot, Quizlet, Khan Academy)

---

## Goals & Success Criteria

### Functional Goals

1. **WYSIWYG Question Editor**
   - Rich text editor for question text (Markdown + KaTeX)
   - Support all 9 question types (multiple choice, true/false, short answer, essay, matching, fill-in-blank, ordering, multiple select, code)
   - Live preview of how question appears to students
   - Image/diagram upload support

2. **Metadata Tagging**
   - Subject (Math, Science, English, History, etc.)
   - Topic (Algebra, Geometry, Grammar, etc.)
   - Difficulty (Easy, Medium, Hard)
   - Bloom's Taxonomy level (Remember, Understand, Apply, Analyze, Evaluate, Create)
   - IRT parameters (difficulty, discrimination, guessing)
   - Standards alignment (Common Core, Next Gen Science, etc.)

3. **Question Bank Management**
   - Browse/search existing questions
   - Filter by subject, difficulty, tags
   - Organize questions into folders
   - Share questions with other teachers (same school/district)
   - Archive/delete questions

4. **Bulk Import**
   - Upload CSV/Excel with multiple questions
   - Template download for each question type
   - Validation and error reporting

### Non-Functional Goals

- **User-friendly:** Non-technical teachers can create questions without training
- **Fast:** Preview updates in <200ms
- **Mobile-responsive:** Works on tablets (80% of teachers use iPads)

### Success Criteria

- [ ] Teachers can create all 9 question types via UI
- [ ] Math equations render correctly (KaTeX)
- [ ] Live preview matches student view exactly
- [ ] Bulk import supports 100+ questions in one upload
- [ ] Question search returns results in <500ms
- [ ] 90% of beta teachers rate UI as "easy to use" (≥4/5)

---

## Technical Approach

### UI Framework

**Blazor Dashboard** (existing infrastructure)

### Component Structure

```
Pages/
└── Questions/
    ├── QuestionBankPage.razor          # Browse/search questions
    ├── CreateQuestionPage.razor        # Create new question
    ├── EditQuestionPage.razor          # Edit existing question
    ├── BulkImportPage.razor            # Upload CSV/Excel
    └── QuestionPreviewModal.razor      # Live preview modal

Components/
└── QuestionAuthoring/
    ├── QuestionTypeSelector.razor      # Select question type
    ├── MarkdownEditor.razor            # Rich text editor
    ├── MathEquationPicker.razor        # KaTeX equation builder
    ├── ImageUploader.razor             # Upload images
    ├── MetadataEditor.razor            # Tags, difficulty, etc.
    ├── AnswerKeyEditor.razor           # Question-type specific answer editor
    └── QuestionPreview.razor           # Read-only preview component
```

### Question Types to Support

| Type | UI Requirements |
|------|-----------------|
| **Multiple Choice** | Question text + 2-6 options (one correct) |
| **True/False** | Question text + correct answer (T/F) |
| **Short Answer** | Question text + acceptable answer(s) with fuzzy matching |
| **Essay** | Question text + rubric + max length |
| **Matching** | 2 lists (left/right) + correct pairs |
| **Fill-in-Blank** | Sentence with `[blanks]` + answer key |
| **Ordering** | List of items + correct order |
| **Multiple Select** | Question text + 2-6 options (1+ correct) |
| **Code** | Question text + language + starter code + test cases |

---

## Task Decomposition

### Task 1: Create Question Bank Page (Browse/Search)

- **Description:** Dashboard page to browse and search existing questions
- **Files to Create:**
  - `src/AcademicAssessment.Dashboard/Pages/Questions/QuestionBankPage.razor`
  - `src/AcademicAssessment.Dashboard/Pages/Questions/QuestionBankPage.razor.cs`
- **Features:**
  - Data grid with questions (ID, text preview, type, difficulty, subject)
  - Search by text (question or answer)
  - Filter by subject, difficulty, question type
  - Sort by created date, difficulty, usage count
  - Pagination (50 questions per page)
  - Actions: View, Edit, Delete, Duplicate
- **Acceptance:** Teachers can browse and search questions
- **Dependencies:** None

### Task 2: Create Markdown Rich Text Editor Component

- **Description:** Reusable Markdown editor with live preview
- **Files to Create:**
  - `src/AcademicAssessment.Dashboard/Components/QuestionAuthoring/MarkdownEditor.razor`
- **Library:** Use **Markdig** (already in project) + custom toolbar
- **Features:**
  - Toolbar: Bold, Italic, Heading, List, Link, Image, Code Block
  - Split view: Editor | Preview
  - Live preview (updates on typing with 200ms debounce)
  - Markdown syntax help link
- **Acceptance:** Markdown renders correctly in preview
- **Dependencies:** None

### Task 3: Create Math Equation Picker Component

- **Description:** UI to insert KaTeX equations into questions
- **Files to Create:**
  - `src/AcademicAssessment.Dashboard/Components/QuestionAuthoring/MathEquationPicker.razor`
- **Features:**
  - Common equation templates (fraction, exponent, square root, integral, sum)
  - LaTeX input field for custom equations
  - Live KaTeX preview
  - "Insert" button adds equation to markdown editor
- **Acceptance:** Equations render correctly with KaTeX
- **Dependencies:** Task 2

### Task 4: Create Image Uploader Component

- **Description:** Upload images for questions (diagrams, charts, etc.)
- **Files to Create:**
  - `src/AcademicAssessment.Dashboard/Components/QuestionAuthoring/ImageUploader.razor`
  - `src/AcademicAssessment.Infrastructure/Services/ImageStorageService.cs`
- **Features:**
  - Drag-and-drop or file picker
  - Image preview before upload
  - Resize large images (max 1920px width)
  - Upload to Azure Blob Storage
  - Return markdown image syntax: `![alt text](url)`
- **Storage:** Azure Blob Storage container: `question-images`
- **Acceptance:** Images uploaded and displayed in questions
- **Dependencies:** Task 2

### Task 5: Create Metadata Editor Component

- **Description:** Form to tag questions with metadata
- **Files to Create:**
  - `src/AcademicAssessment.Dashboard/Components/QuestionAuthoring/MetadataEditor.razor`
- **Fields:**

  ```
  - Subject: Dropdown (Math, Science, English, History, Other)
  - Topic: Text input (e.g., "Algebra", "Geometry")
  - Difficulty: Radio buttons (Easy, Medium, Hard)
  - Bloom's Level: Dropdown (Remember, Understand, Apply, Analyze, Evaluate, Create)
  - Standards: Multi-select (Common Core, NGSS, etc.)
  - Tags: Chip input (free-form tags)
  - IRT Parameters (optional):
    - Difficulty: Slider (-3 to +3)
    - Discrimination: Slider (0 to 3)
    - Guessing: Slider (0 to 0.5)
  ```

- **Acceptance:** All metadata fields editable
- **Dependencies:** None

### Task 6: Create Answer Key Editor for Multiple Choice

- **Description:** UI to create multiple choice questions
- **Files to Create:**
  - `src/AcademicAssessment.Dashboard/Components/QuestionAuthoring/MultipleChoiceEditor.razor`
- **Features:**
  - Add/remove options (2-6 options)
  - Mark correct answer (radio button)
  - Reorder options (drag-and-drop)
  - Option text supports Markdown + equations
- **Acceptance:** Multiple choice questions created correctly
- **Dependencies:** Tasks 2-3

### Task 7: Create Answer Key Editors for Other Question Types

- **Description:** Create answer editors for remaining 8 question types
- **Files to Create:**
  - `TrueFalseEditor.razor` - Radio buttons (True/False)
  - `ShortAnswerEditor.razor` - Text input + acceptable answers list
  - `EssayEditor.razor` - Rubric editor + max word count
  - `MatchingEditor.razor` - Two lists + drag-and-drop pairing
  - `FillInBlankEditor.razor` - Text with [blank] markers + answer key
  - `OrderingEditor.razor` - List of items + drag to reorder
  - `MultipleSelectEditor.razor` - Checkboxes for options
  - `CodeEditor.razor` - Language selector + Monaco editor + test cases
- **Acceptance:** All question types have functional editors
- **Dependencies:** Tasks 2-3

### Task 8: Create Question Preview Component

- **Description:** Read-only preview of how question appears to students
- **Files to Create:**
  - `src/AcademicAssessment.Dashboard/Components/QuestionAuthoring/QuestionPreview.razor`
- **Features:**
  - Render question exactly as students see it
  - Use same components as StudentApp (for consistency)
  - Show correct answer highlighted (for teachers only)
  - Responsive preview (desktop, tablet, mobile)
- **Acceptance:** Preview matches student view exactly
- **Dependencies:** Tasks 6-7

### Task 9: Create Question Type Selector

- **Description:** First step in question creation - choose question type
- **Files to Create:**
  - `src/AcademicAssessment.Dashboard/Components/QuestionAuthoring/QuestionTypeSelector.razor`
- **UI:** Grid of cards with icons

  ```
  [Multiple Choice] [True/False]    [Short Answer]
  [Essay]          [Matching]       [Fill-in-Blank]
  [Ordering]       [Multiple Select] [Code]
  ```

- **Acceptance:** Clicking card loads appropriate editor
- **Dependencies:** None

### Task 10: Create Question Creation Page

- **Description:** Main page to create new question
- **Files to Create:**
  - `src/AcademicAssessment.Dashboard/Pages/Questions/CreateQuestionPage.razor`
- **Flow:**
  1. Select question type (Task 9)
  2. Enter question text (Task 2)
  3. Add equations/images (Tasks 3-4)
  4. Configure answer key (Tasks 6-7)
  5. Add metadata (Task 5)
  6. Preview question (Task 8)
  7. Save to database
- **Validation:**
  - Question text required
  - At least one correct answer
  - All required metadata filled
- **Acceptance:** New questions saved to database
- **Dependencies:** Tasks 2-9

### Task 11: Create Question Edit Page

- **Description:** Edit existing question
- **Files to Create:**
  - `src/AcademicAssessment.Dashboard/Pages/Questions/EditQuestionPage.razor`
- **Features:**
  - Load existing question from ID
  - Pre-populate all fields
  - Show usage count (how many assessments use this question)
  - Warn if editing question used in active assessments
  - Save changes (creates new version, keeps old for historical data)
- **Acceptance:** Questions edited successfully
- **Dependencies:** Task 10

### Task 12: Create Bulk Import Page

- **Description:** Upload CSV/Excel with multiple questions
- **Files to Create:**
  - `src/AcademicAssessment.Dashboard/Pages/Questions/BulkImportPage.razor`
  - `src/AcademicAssessment.Infrastructure/Services/QuestionImportService.cs`
- **CSV Format (Multiple Choice Example):**

  ```csv
  Type,Question,Option1,Option2,Option3,Option4,CorrectAnswer,Difficulty,Subject,Topic
  MultipleChoice,"What is 2+2?","3","4","5","6","4","Easy","Math","Arithmetic"
  ```

- **Features:**
  - Download CSV template for each question type
  - Upload CSV/Excel file
  - Validate all rows
  - Show preview of imported questions
  - Display errors (row number + error message)
  - Bulk save (all-or-nothing transaction)
- **Acceptance:** 100+ questions imported successfully
- **Dependencies:** Task 10

### Task 13: Create Question Service

- **Description:** Application service for question CRUD operations
- **Files to Create:**
  - `src/AcademicAssessment.Core/Services/IQuestionService.cs`
  - `src/AcademicAssessment.Infrastructure/Services/QuestionService.cs`
- **Methods:**

  ```csharp
  Task<Result<Guid>> CreateQuestionAsync(CreateQuestionDto dto, CancellationToken ct);
  Task<Result<Question>> GetQuestionByIdAsync(Guid id, CancellationToken ct);
  Task<Result<PagedResult<Question>>> SearchQuestionsAsync(QuestionSearchDto search, CancellationToken ct);
  Task<Result> UpdateQuestionAsync(Guid id, UpdateQuestionDto dto, CancellationToken ct);
  Task<Result> DeleteQuestionAsync(Guid id, CancellationToken ct);
  Task<Result<int>> BulkImportQuestionsAsync(List<CreateQuestionDto> questions, CancellationToken ct);
  ```

- **Acceptance:** Service methods work with repository
- **Dependencies:** None (can run parallel with UI tasks)

### Task 14: Create Question Management API Endpoints

- **Description:** REST API for question CRUD
- **Files to Create:**
  - `src/AcademicAssessment.Web/Controllers/QuestionsController.cs`
- **Endpoints:**

  ```csharp
  POST   /api/questions              # Create question
  GET    /api/questions/{id}         # Get by ID
  GET    /api/questions              # Search/filter
  PUT    /api/questions/{id}         # Update question
  DELETE /api/questions/{id}         # Delete question
  POST   /api/questions/bulk-import  # Bulk import CSV
  GET    /api/questions/{id}/usage   # How many assessments use this?
  ```

- **Authorization:** `[Authorize(Roles = "Teacher,TenantAdmin")]`
- **Acceptance:** All endpoints functional
- **Dependencies:** Task 13

### Task 15: Write Integration Tests

- **Description:** Test question authoring workflows
- **Files to Create:**
  - `tests/AcademicAssessment.Tests.Integration/Questions/QuestionAuthoringTests.cs`
- **Test Cases:**

  ```csharp
  [Fact]
  public async Task CreateMultipleChoiceQuestion_Success()
  
  [Fact]
  public async Task BulkImport_100Questions_Success()
  
  [Fact]
  public async Task SearchQuestions_BySubject_ReturnsFiltered()
  
  [Fact]
  public async Task EditQuestion_InActiveAssessment_ShowsWarning()
  
  [Fact]
  public async Task DeleteQuestion_NotInUse_Success()
  ```

- **Acceptance:** All tests pass
- **Dependencies:** Task 14

### Task 16: Update Documentation

- **Description:** User guide for teachers
- **Files to Create:**
  - `docs/user-guides/TEACHER_QUESTION_AUTHORING.md`
- **Content:**
  - How to create each question type (with screenshots)
  - How to add equations and images
  - How to use bulk import
  - Best practices for metadata tagging
  - Troubleshooting common errors
- **Acceptance:** Documentation complete with screenshots
- **Dependencies:** Task 15

---

## Acceptance Criteria

### Functional Requirements

- [ ] Teachers can create all 9 question types via UI
- [ ] Markdown editor with live preview
- [ ] Math equations (KaTeX) supported
- [ ] Image upload to Azure Blob Storage
- [ ] Metadata tagging (subject, difficulty, Bloom's, IRT)
- [ ] Question preview matches student view
- [ ] Bulk import CSV/Excel (100+ questions)
- [ ] Question search/filter by metadata
- [ ] Edit existing questions (with versioning)

### Usability Requirements

- [ ] 90% of beta teachers rate UI ≥4/5 "easy to use"
- [ ] Question creation takes <5 minutes for experienced teachers
- [ ] Preview updates in <200ms

### Technical Requirements

- [ ] Mobile-responsive (works on iPad)
- [ ] Search returns results in <500ms
- [ ] Image upload <2MB per image
- [ ] All API endpoints have proper authorization

---

## Context & References

### Documentation

- [Domain Model - Question Types](.github/specification/03-domain-model.md#question-types)

### External References

- [Markdig (Markdown)](https://github.com/xoofx/markdig)
- [KaTeX (Math Rendering)](https://katex.org/)
- [Monaco Editor (Code Editor)](https://microsoft.github.io/monaco-editor/)

### Competitor Analysis

- [Kahoot Question Creator](https://kahoot.com/)
- [Quizlet Create Flashcards](https://quizlet.com/create)
- [Khan Academy Exercise Creator](https://www.khanacademy.org/)

---

## Notes

- **Image Storage:** Budget $10/month Azure Blob Storage for question images
- **Beta Testing:** Recruit 5-10 teachers for usability testing before GA release
- **Accessibility:** Ensure WCAG 2.1 AA compliance (screen readers, keyboard navigation)

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot  
**Last Updated:** 2025-10-25
