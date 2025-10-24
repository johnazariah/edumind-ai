# 09c. User Interface Features

**Document Version:** 1.0  
**Last Updated:** October 24, 2025  
**Status:** Active Development

---

## Table of Contents

1. [Overview](#overview)
2. [Student App](#student-app)
3. [Dashboard App](#dashboard-app)
4. [Accessibility Features](#accessibility-features)
5. [Mobile Responsiveness](#mobile-responsiveness)
6. [Feature Status Summary](#feature-status-summary)

---

## Overview

This document catalogs the user interface features across all three applications (Student App, Dashboard, and Teacher/Admin interfaces). EduMind.AI uses Blazor Server for interactive, real-time user experiences.

### Related Documents

- [02-system-architecture.md](02-system-architecture.md) - Application architecture
- [04-application-components.md](04-application-components.md) - UI components
- [09a-core-assessment-features.md](09a-core-assessment-features.md) - Assessment features
- [11-user-workflows.md](11-user-workflows.md) - User workflows and journeys

---

## Student App

### Assessment Discovery

#### ✅ Assessment Dashboard

**Status:** Fully Implemented (Week 2, Days 8-9)

Landing page showing available and in-progress assessments:

**Features:**

- Assessment cards with metadata (title, duration, question count, subjects)
- Progress indicators for in-progress assessments
- Filtering by subject (All, Math, Physics, Chemistry, Biology, English)
- Filtering by difficulty (All, Easy, Medium, Hard, Expert)
- Search functionality by title
- Color-coded subject badges
- Responsive grid layout

**Route:** `/assessments`

**Files:**

- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentDashboard.razor`
- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentDashboard.razor.cs`

**Sample UI Elements:**

```html
<div class="assessment-card">
  <h3>Algebra Fundamentals</h3>
  <div class="badges">
    <span class="badge badge-math">Mathematics</span>
    <span class="badge badge-medium">Medium</span>
  </div>
  <div class="meta">
    <span>⏱️ 45 minutes</span>
    <span>❓ 20 questions</span>
  </div>
  <button class="btn-primary">Start Assessment</button>
</div>
```

#### ✅ Assessment Detail Page

**Status:** Fully Implemented (Week 2, Days 8-9)

Detailed view before starting assessment:

**Features:**

- Assessment title and description
- Metadata display (duration, question count, subjects covered)
- Learning objectives list
- Difficulty level indicator
- Instructions and rules
- Historical attempts with scores (if any)
- "Start Assessment" button with confirmation
- Breadcrumb navigation

**Route:** `/assessment/{id}`

**Files:**

- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentDetail.razor`
- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentDetail.razor.cs`

#### ✅ Assessment Navigation Component

**Status:** Fully Implemented (Week 2, Days 8-9)

Reusable breadcrumb navigation:

**Features:**

- Breadcrumb trail (Home → Assessments → [Assessment Name])
- Active page highlighting
- Clickable links for navigation
- Responsive collapse on mobile

**Files:**

- `src/AcademicAssessment.StudentApp/Components/AssessmentNavigation.razor`

### Assessment Session

#### ✅ Question Rendering

**Status:** Fully Implemented (Week 2, Days 10-11)

Rich content rendering for all question types:

**Features:**

- **Markdown Support:** Rendered via Markdig library
- **LaTeX/KaTeX Math:** Inline (`$...$`) and block (`$$...$$`) equations
- **Code Syntax Highlighting:** Via highlight.js (supports 50+ languages)
- **Question Text:** Formatted HTML rendering
- **Optional Hints:** Toggle-able hint display with "Show Hint" button
- **Question Numbering:** Current question number and total
- **Subject Badge:** Color-coded subject indicator

**Supported Content Types:**

- Plain text
- Markdown formatting (bold, italic, lists, tables)
- Mathematical equations (rendered beautifully with KaTeX)
- Code snippets (syntax-highlighted)
- Images (planned)
- Diagrams (planned)

**Files:**

- `src/AcademicAssessment.StudentApp/Components/QuestionRenderer.razor`
- `src/AcademicAssessment.StudentApp/wwwroot/js/assessment-ui.js`
- Local assets: `wwwroot/lib/katex/`, `wwwroot/lib/highlight/`

**Example Rendering:**

```markdown
## Question 1

Calculate the **derivative** of $f(x) = x^2 + 3x + 2$

$$
\frac{d}{dx}f(x) = ?
$$
```

Renders with proper heading, bold text, inline math, and centered block equation.

#### ✅ Answer Input Components

**Status:** Fully Implemented (Week 2, Days 10-11)

Nine specialized input components for all question types:

**1. Multiple Choice (Radio)**

- Single selection from options
- Radio buttons with labels
- Disabled state support
- Clear visual selection

**2. Multiple Select (Checkbox)**

- Multiple correct answers
- Checkboxes with labels
- Disabled state support
- Select/deselect toggling

**3. True/False**

- Binary choice (specialized multiple choice)
- Large, clear buttons
- Disabled state support

**4. Short Answer**

- Single-line text input
- Placeholder text
- Character limit indicator (planned)
- Disabled state support

**5. Fill in the Blank**

- Similar to short answer
- Context-aware placeholder
- Multiple blanks support (planned)

**6. Math Expression**

- Specialized text input
- Math keyboard overlay (planned)
- LaTeX input support (planned)
- Preview rendering (planned)

**7. Essay**

- Multi-line textarea (8 rows)
- Rich text editor (planned)
- Word count indicator (planned)
- Auto-expanding height (planned)
- Disabled state support

**8. Code Snippet**

- Code editor with syntax highlighting (planned)
- Language selection dropdown (planned)
- Line numbers (planned)
- Auto-indentation (planned)

**9. Matching**

- Drag-and-drop interface (planned)
- Click-to-match fallback (current)
- Visual pair indicators

**Files:**

- `src/AcademicAssessment.StudentApp/Components/Answers/MultipleChoiceAnswer.razor`
- `src/AcademicAssessment.StudentApp/Components/Answers/ShortAnswerInput.razor`
- `src/AcademicAssessment.StudentApp/Components/Answers/EssayAnswerInput.razor`
- Namespace: `AcademicAssessment.StudentApp.Components.Answers`

**Common Features:**

- Disabled state when session expired
- Validation indicators
- Clear visual feedback
- Responsive layout

#### ✅ Assessment Session Page

**Status:** Fully Implemented (Week 2, Days 11-12)

Complete assessment-taking experience with state management:

**Core Features:**

1. **Timer Display**
   - Countdown timer (hours or minutes:seconds)
   - Visual warnings at 5 minutes remaining (orange)
   - Critical warning at 1 minute (red)
   - Session expiry detection
   - Auto-lockdown on expiry

2. **Auto-Save**
   - Automatic save every 30 seconds
   - Semaphore-based coordination (thread-safe)
   - Debounced save operations
   - Visual save status ("Saving...", "Saved", "Save Failed")
   - Error handling with retry

3. **Question Navigation**
   - Previous/Next buttons
   - Question palette (grid of all questions)
   - Jump to specific question
   - Linear progression enforcement (optional)

4. **Answer State Management**
   - Dictionary-based answer tracking
   - Selected options for MCQ/multiple select
   - Free-text responses for short answer/essay
   - Persistent across navigation
   - Clear response functionality

5. **Session Management**
   - Load session on page load
   - Track answered vs unanswered questions
   - Calculate completion percentage
   - Mark questions for review
   - Submit assessment with confirmation

6. **Progress Tracking**
   - Questions answered counter
   - Percentage completion
   - Subject-wise breakdown (via ProgressVisualization)
   - Time spent tracking

**Route:** `/assessment/{AssessmentId:guid}/session`

**Files:**

- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentSession.razor`
- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentSession.razor.cs` (600+ lines)

**State Management:**

```csharp
// Answer tracking
private Dictionary<Guid, AnswerState> _answers = new();

// Thread-safe auto-save
private readonly SemaphoreSlim _saveSemaphore = new(1, 1);

// Timers
private PeriodicTimer? _autoSaveTimer;
private PeriodicTimer? _countdownTimer;
```

#### ✅ Question Palette

**Status:** Fully Implemented (Week 2, Days 10-12)

Visual navigation grid showing all questions:

**Features:**

- Grid layout (5 columns on desktop, responsive on mobile)
- Question numbers as buttons
- Status indicators:
  - **Green:** Answered
  - **Blue:** Current question
  - **Yellow:** Marked for review
  - **Gray:** Unanswered
- Click to jump to specific question
- Disabled state when session expired
- Review flag icons

**Files:**

- `src/AcademicAssessment.StudentApp/Components/QuestionPalette.razor`

**Visual Example:**

```
[1] [2] [3] [4] [5]
[6] [7] [8] [9] [10]
[11][12][13][14][15]

Legend:
🟢 Answered  🔵 Current  🟡 Review  ⚪ Unanswered
```

#### ✅ Progress Visualization

**Status:** Fully Implemented (Week 2, Day 14)

Enhanced progress tracking sidebar:

**Features:**

1. **Overall Progress**
   - Large progress bar (0-100%)
   - Percentage text display
   - Color-coded (red → yellow → green)

2. **Status Legend**
   - Answered count and percentage
   - Flagged for review count
   - Unanswered count
   - Current question indicator

3. **Subject-Wise Breakdown**
   - Progress bar per subject
   - Color-coded by subject (Math=blue, Physics=purple, etc.)
   - Question count per subject
   - Completion percentage per subject

4. **Time Statistics**
   - Average time per question
   - Estimated finish time
   - Time efficiency indicator

**Files:**

- `src/AcademicAssessment.StudentApp/Components/ProgressVisualization.razor`

**Integration:** Displayed in sidebar of AssessmentSession page

#### ✅ Save/Submit Integration

**Status:** Fully Implemented (Week 2, Day 13)

API integration for saving and submitting assessments:

**Save Endpoint:**

- **Route:** `POST /api/v1.0/Assessment/{id}/session/save`
- **DTOs:** `SaveAssessmentSessionRequest`, `SaveAssessmentSessionResponse`
- **Features:**
  - Save current answers
  - Update session state
  - Return confirmation
  - Error handling with detailed messages

**Submit Endpoint:**

- **Route:** `POST /api/v1.0/Assessment/{id}/session/submit`
- **DTOs:** `SubmitAssessmentSessionRequest`, `SubmitAssessmentSessionResponse`
- **Features:**
  - Final submission (no further edits)
  - Trigger orchestrator for evaluation
  - Calculate preliminary scores
  - Return session ID for results

**Toast Notifications:**

- Success messages (green)
- Error messages (red)
- Info messages (blue)
- Auto-dismiss after 3 seconds
- Dismissible manually

**Files:**

- `src/AcademicAssessment.Web/Controllers/AssessmentController.cs` (lines 340-420)
- `src/AcademicAssessment.StudentApp/Components/ToastNotification.razor`
- DTOs in `src/AcademicAssessment.Web/Models/`

### Assessment Results

#### ✅ Results Page

**Status:** Fully Implemented (Week 2, Days 13-14)

Comprehensive results display after submission:

**Features:**

1. **Overall Score Card**
   - Large score display (e.g., "85/100")
   - Percentage with letter grade
   - Performance level (Excellent/Good/Needs Improvement)
   - Gradient background based on score
   - Celebration animation (planned)

2. **Subject-Wise Performance**
   - Breakdown by all subjects attempted
   - Questions attempted per subject
   - Correct answers per subject
   - Accuracy percentage per subject
   - Performance level per subject
   - Color-coded progress bars

3. **Time Efficiency**
   - Total time taken
   - Average time per question
   - Comparison to class average (planned)
   - Efficiency rating

4. **Strengths and Weaknesses**
   - Top performing subjects (strengths)
   - Subjects needing improvement (weaknesses)
   - Specific topic recommendations
   - Visual icons for quick scanning

5. **Recommended Next Steps**
   - Actionable recommendations
   - Review specific topics
   - Practice exercises
   - Additional resources (planned)
   - Next assessment suggestions

6. **Action Buttons**
   - Review Answers (navigate to review page)
   - Back to Dashboard
   - Retake Assessment (if allowed)
   - Download Report (PDF, planned)

**Route:** `/assessment/results/{sessionId}`

**Files:**

- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentResults.razor`
- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentResults.razor.cs`
- API: `GET /api/v1.0/Assessment/results/{sessionId}`
- DTOs: `AssessmentResultsDto`, `SubjectPerformanceDto`

**API Endpoint:**

Currently returns mock data for testing. Full implementation pending database integration.

#### 📋 Answer Review Page

**Status:** Planned (Week 3)

Detailed review of all questions and answers:

**Planned Features:**

- Show all questions with student's answers
- Indicate correct/incorrect with visual markers
- Display correct answers for comparison
- Show LLM feedback and confidence scores
- Explain why answers were marked incorrect
- Filter by: All, Correct, Incorrect, Review-flagged
- Navigate between questions
- Print-friendly version

**Files:** Not yet implemented

### Authentication

#### ✅ Azure AD B2C Integration

**Status:** Fully Implemented

**Features:**

- Microsoft Identity integration
- JWT bearer token authentication
- Automatic token refresh
- Redirect to login page if unauthenticated
- Secure cookie storage
- HTTPS enforcement

**Files:**

- `src/AcademicAssessment.StudentApp/Program.cs` (authentication setup)
- Integration with Azure AD B2C tenant

**See Also:** [07-security-privacy.md](07-security-privacy.md) for complete authentication details

---

## Dashboard App

### Overview

**Status:** Partially Implemented

The Dashboard app serves teachers, school admins, and system admins with monitoring and management capabilities.

#### ⚠️ Real-Time Monitoring Dashboard

**Status:** Implemented (Orchestration Monitoring Only)

**Current Implementation:**

- HTML/CSS/JavaScript dashboard
- SignalR real-time updates (5-second intervals)
- Orchestration metrics:
  - Success rate
  - Total routings
  - Queue depth
  - Agent utilization
  - Circuit breaker states
  - Alert feed

**Access:** `/monitoring-dashboard.html` (currently static HTML, not Blazor)

**Files:**

- `src/AcademicAssessment.Web/wwwroot/monitoring-dashboard.html`

**Planned Migration to Blazor:**

- Convert to Blazor Server component
- Add authentication and role-based access
- Integrate with Dashboard app navigation
- Add more granular metrics

#### 📋 Teacher Dashboard

**Status:** Planned (Week 4-5)

**Planned Features:**

1. **Class Overview**
   - Student roster
   - Overall class performance
   - Recent assessment activity
   - Alerts for struggling students

2. **Assessment Management**
   - Create new assessments
   - Edit existing assessments
   - Assign to classes or individual students
   - Set deadlines and time limits
   - View submission status

3. **Student Progress Tracking**
   - Individual student reports
   - Class-wide analytics
   - Subject-wise performance
   - Progress trends over time
   - Mastery tracking

4. **Grading Interface**
   - Review essay and code responses
   - Provide feedback
   - Adjust scores (with audit trail)
   - Bulk grading tools

5. **Reporting**
   - Generate PDF reports
   - Export data to Excel/CSV
   - Custom report builder
   - Share reports with parents/admins

**Files:** Not yet implemented

#### 📋 Admin Dashboard

**Status:** Planned (Week 5-6)

**Planned Features:**

1. **School Management** (School Admin)
   - Manage teachers and students
   - Class/section management
   - School-wide settings
   - Content library management

2. **System Monitoring** (System Admin)
   - Service health dashboard
   - Performance metrics
   - Error logs
   - Agent status
   - Database statistics

3. **User Management**
   - Create/edit user accounts
   - Assign roles
   - Manage permissions
   - View audit logs

4. **Content Management**
   - Question bank management
   - Curriculum mapping
   - Standards alignment
   - Bulk import/export

5. **Analytics and Insights**
   - System-wide trends
   - Usage statistics
   - ROI reporting
   - Predictive analytics

**Files:** Not yet implemented

---

## Accessibility Features

### WCAG 2.1 AA Compliance

#### 📋 Accessibility Standards

**Status:** Planned (Week 3, Days 15-16)

**Target Compliance:** WCAG 2.1 Level AA

**Planned Features:**

1. **Keyboard Navigation**
   - All interactive elements keyboard-accessible
   - Tab order follows logical flow
   - Skip navigation links
   - Keyboard shortcuts for common actions
   - Visible focus indicators

2. **Screen Reader Support**
   - ARIA labels on all interactive elements
   - Semantic HTML structure
   - Alt text for all images
   - Descriptive link text
   - Live region announcements for dynamic content

3. **Visual Accessibility**
   - High contrast mode
   - Minimum contrast ratios (4.5:1 for text)
   - Resizable text (up to 200%)
   - No color-only indicators
   - Clear visual focus states

4. **Cognitive Accessibility**
   - Clear, consistent navigation
   - Simple language
   - Ample time limits (adjustable)
   - Progress indicators
   - Error prevention and recovery

5. **Assistive Technology**
   - Screen reader testing (NVDA, JAWS, VoiceOver)
   - Voice control compatibility
   - Switch control support
   - Magnification support

**Files:** Accessibility audit and implementation pending

#### 📋 Accessibility Testing

**Planned Tools:**

- axe DevTools (automated testing)
- WAVE (web accessibility evaluation)
- Lighthouse accessibility audits
- Manual screen reader testing
- Keyboard-only navigation testing

---

## Mobile Responsiveness

### Mobile-First Design

#### 📋 Responsive Layouts

**Status:** Partially Implemented (Bootstrap 5 responsive utilities)

**Current Implementation:**

- Bootstrap 5 grid system
- Responsive breakpoints (sm, md, lg, xl)
- Mobile-friendly navigation
- Touch-friendly button sizes (minimum 44x44px)

**Pending Optimizations:**

1. **Mobile Assessment Experience**
   - Vertical layout for questions
   - Swipe gestures for navigation
   - Bottom navigation bar
   - Optimized timer display
   - Collapsible question palette

2. **Mobile Dashboard**
   - Card-based layouts
   - Swipe actions for quick operations
   - Bottom sheet menus
   - Optimized data tables

3. **Touch Interactions**
   - Large tap targets (minimum 44x44px)
   - Swipe to navigate
   - Pull to refresh
   - Pinch to zoom (for math/diagrams)
   - Long-press menus

**Files:** Responsive CSS in component files

#### 📋 Progressive Web App (PWA)

**Status:** Planned (Week 3, Days 15-16)

**Planned Features:**

1. **Installability**
   - Web app manifest
   - Install prompts
   - App icons (all sizes)
   - Splash screens

2. **Offline Capability**
   - Service worker caching
   - Offline assessment taking
   - Sync when reconnected
   - Offline indicator

3. **Native-Like Experience**
   - Full-screen mode
   - Push notifications (assessment reminders)
   - Background sync
   - Home screen shortcuts

**Files:** Not yet implemented

### Tablet Optimization

#### 📋 Tablet-Specific Features

**Status:** Planned

**Features:**

- Split-view support (iPad)
- Landscape mode optimization
- Stylus/pencil input for math/diagrams
- Multi-window support
- Drag-and-drop between apps

---

## Feature Status Summary

### Completed Features (✅)

**Student App - Assessment Discovery:**

- ✅ Assessment dashboard with filtering and search
- ✅ Assessment detail page with metadata and learning objectives
- ✅ Assessment navigation breadcrumbs

**Student App - Assessment Session:**

- ✅ Question rendering (Markdown, KaTeX, highlight.js)
- ✅ All 9 answer input components
- ✅ Assessment session page with full state management
- ✅ Timer with countdown and expiry detection
- ✅ Auto-save (30-second interval with semaphore coordination)
- ✅ Question palette with status indicators
- ✅ Progress visualization with subject breakdown
- ✅ Save/submit API integration
- ✅ Toast notifications

**Student App - Results:**

- ✅ Assessment results page with comprehensive data
- ✅ Subject-wise performance breakdown
- ✅ Strengths and weaknesses identification
- ✅ Recommended next steps
- ✅ Action buttons (review, retake, back)

**Authentication:**

- ✅ Azure AD B2C integration
- ✅ JWT bearer token authentication
- ✅ Secure cookie storage

**Dashboard App:**

- ✅ Orchestration monitoring dashboard (HTML/JS, real-time SignalR)

### In Progress (⚠️)

- ⚠️ Answer review page (design complete, implementation pending)
- ⚠️ Blazor migration of monitoring dashboard

### Planned Features (📋)

**Student App:**

- 📋 Image and diagram rendering in questions
- 📋 Rich text editor for essay answers
- 📋 Math keyboard for math expression input
- 📋 Code editor for code snippet answers
- 📋 Drag-and-drop for matching questions
- 📋 Answer review page with detailed feedback
- 📋 PDF report generation

**Dashboard App:**

- 📋 Teacher dashboard (complete implementation)
- 📋 Admin dashboards (school and system admin)
- 📋 Assessment creation UI
- 📋 Grading interface for manual review
- 📋 Reporting and analytics dashboards
- 📋 User management UI
- 📋 Content management UI

**Accessibility:**

- 📋 WCAG 2.1 AA compliance audit
- 📋 Keyboard navigation enhancements
- 📋 Screen reader optimization
- 📋 High contrast mode
- 📋 Adjustable font sizes
- 📋 Cognitive accessibility improvements

**Mobile/Tablet:**

- 📋 Mobile-optimized layouts (all pages)
- 📋 PWA implementation (offline support, installability)
- 📋 Touch gestures (swipe, pinch, long-press)
- 📋 Tablet split-view support
- 📋 Stylus/pencil input

**General Enhancements:**

- 📋 Dark mode theme
- 📋 Localization/i18n (multiple languages)
- 📋 RTL support (right-to-left languages)
- 📋 Customizable themes per school
- 📋 Notification center (in-app notifications)

---

## Implementation Locations

### Key Files

**Student App - Pages:**

- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentDashboard.razor`
- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentDetail.razor`
- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentSession.razor`
- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentSession.razor.cs`
- `src/AcademicAssessment.StudentApp/Components/Pages/AssessmentResults.razor`

**Student App - Components:**

- `src/AcademicAssessment.StudentApp/Components/AssessmentNavigation.razor`
- `src/AcademicAssessment.StudentApp/Components/QuestionRenderer.razor`
- `src/AcademicAssessment.StudentApp/Components/QuestionPalette.razor`
- `src/AcademicAssessment.StudentApp/Components/ProgressVisualization.razor`
- `src/AcademicAssessment.StudentApp/Components/ToastNotification.razor`
- `src/AcademicAssessment.StudentApp/Components/Answers/*.razor` (9 components)

**Student App - Assets:**

- `src/AcademicAssessment.StudentApp/wwwroot/js/assessment-ui.js`
- `src/AcademicAssessment.StudentApp/wwwroot/lib/katex/` (KaTeX assets)
- `src/AcademicAssessment.StudentApp/wwwroot/lib/highlight/` (highlight.js assets)
- `src/AcademicAssessment.StudentApp/wwwroot/css/` (custom styles)

**API Controllers:**

- `src/AcademicAssessment.Web/Controllers/AssessmentController.cs`
- `src/AcademicAssessment.Web/Controllers/StudentAnalyticsController.cs`

**Dashboard App:**

- `src/AcademicAssessment.Dashboard/` (minimal implementation, planned expansion)
- `src/AcademicAssessment.Web/wwwroot/monitoring-dashboard.html` (current monitoring)

**Configuration:**

- `src/AcademicAssessment.StudentApp/Program.cs` (Blazor setup, authentication)
- `src/AcademicAssessment.StudentApp/App.razor` (routing, auth states)
- `src/AcademicAssessment.StudentApp/_Imports.razor` (global using statements)

---

## Related Documentation

- **[02-system-architecture.md](02-system-architecture.md)** - Application architecture
- **[04-application-components.md](04-application-components.md)** - UI components
- **[07-security-privacy.md](07-security-privacy.md)** - Authentication and authorization
- **[08-observability.md](08-observability.md)** - Monitoring and logging
- **[09a-core-assessment-features.md](09a-core-assessment-features.md)** - Assessment engine
- **[09b-agent-orchestration-features.md](09b-agent-orchestration-features.md)** - Multi-agent coordination
- **[11-user-workflows.md](11-user-workflows.md)** - User journeys and workflows

---

**Document Status:** Complete  
**Last Review:** October 24, 2025  
**Next Review:** After Week 3 UI enhancements
