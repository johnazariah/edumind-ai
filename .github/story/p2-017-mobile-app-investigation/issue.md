# Story 017: Mobile App Investigation (MAUI + Duolingo-Style Gamification)

**Priority:** P2 - Enhancement (Post-Production)  
**Status:** Ready for Investigation  
**Effort:** Small (1-2 weeks)  
**Dependencies:** None (can start after Story 006)

---

## Problem Statement

EduMind.AI currently lacks a native mobile application, limiting our ability to compete with platforms like **Duolingo, Khan Academy, and Quizlet** that provide seamless mobile learning experiences. Students increasingly prefer mobile-first experiences with:

- Quick, bite-sized assessments on-the-go
- Gamification (XP, streaks, badges, leaderboards)
- Push notifications for daily reminders
- Offline capability for assessments
- Native app store presence

**Current State:**

- Web app only (Blazor Server, responsive but not optimized for mobile)
- No native mobile features (offline, push notifications, gestures)
- No gamification elements
- No app store presence

**Market Context:**

- Duolingo: 500M+ downloads, 42M daily active users
- Khan Academy: 100M+ downloads
- Quizlet: 60M+ students

**Strategic Impact:**
Without a native mobile app, we cannot effectively compete in the B2C market or provide the modern learning experience students expect.

---

## Goals & Success Criteria

### Investigation Goals

This is an **investigation story** (not full implementation). The goal is to:

1. **Prove technical feasibility** of .NET MAUI Blazor Hybrid approach
2. **Measure code reuse** between web and mobile
3. **Benchmark performance** on real Android devices
4. **Design gamification system** (XP, streaks, leaderboards)
5. **Evaluate offline sync strategy** (SQLite + background sync)
6. **Compare alternatives** (MAUI vs React Native vs PWA)
7. **Estimate effort** for full implementation
8. **Make go/no-go recommendation**

### Success Criteria

- [ ] Working proof-of-concept: One screen (Assessment Taking) converted to MAUI
- [ ] Measured code reuse percentage (target: >75%)
- [ ] Performance benchmarks on 3 Android devices
- [ ] Gamification system design document
- [ ] Offline sync architecture diagram
- [ ] Effort estimate for full implementation (±20% accuracy)
- [ ] Technical feasibility report with recommendation
- [ ] Executive summary for go/no-go decision

---

## Technical Approach

### Investigation Scope

**Week 1: Proof-of-Concept**

1. Create new .NET MAUI project
2. Convert 1 complex screen (Assessment Taking) to MAUI Blazor Hybrid
3. Test on Android emulator and 2 real devices
4. Measure what percentage of code can be reused
5. Identify MAUI-specific adaptations needed

**Week 2: Architecture & Design**
6. Design gamification system (database schema, services, UI)
7. Design offline sync strategy (SQLite, conflict resolution)
8. Design push notification architecture
9. Compare MAUI vs alternatives (React Native, PWA)
10. Create effort estimate for full implementation

### Technology Stack Evaluation

**Option 1: .NET MAUI Blazor Hybrid (Recommended)**

- Same Blazor components from web app
- C# throughout (web, mobile, API)
- 80-90% code reuse expected

**Option 2: React Native**

- Zero code reuse from Blazor web
- Requires JavaScript/TypeScript expertise
- Larger ecosystem, more community resources

**Option 3: Progressive Web App (PWA)**

- Quick to implement (2-3 weeks)
- Limited offline support
- No app store presence
- Poor iOS support

### Proof-of-Concept Scope

Convert the **Assessment Taking** screen to MAUI because it includes:

- Complex UI (question rendering with Markdown, KaTeX, code highlighting)
- Form handling (multiple question types)
- State management (current question, timer, progress)
- API calls (submit answers)
- Local storage (auto-save)

This screen exercises the most challenging aspects of the migration.

---

## Task Decomposition

### Task 1: Set Up MAUI Development Environment

- **Description:** Install .NET MAUI workloads and Android SDK
- **Steps:**

  ```bash
  dotnet workload install maui
  dotnet workload install android
  # Install Android SDK via Visual Studio or Android Studio
  ```

- **Test Devices:**
  - Android Emulator (Pixel 5, API 33)
  - Physical device: Samsung Galaxy (if available)
  - Physical device: Google Pixel (if available)
- **Acceptance:** MAUI template app runs on emulator and device
- **Dependencies:** None

### Task 2: Create MAUI Blazor Hybrid Project

- **Description:** Create new MAUI project with Blazor integration
- **Structure:**

  ```
  src/
  └── EduMind.MobileApp/
      ├── EduMind.MobileApp.csproj
      ├── MauiProgram.cs
      ├── wwwroot/
      │   └── index.html
      ├── Pages/
      │   └── Index.razor
      └── Platforms/
          ├── Android/
          ├── iOS/
          └── Windows/
  ```

- **Configuration:**
  - Reference existing `AcademicAssessment.Core` project
  - Reference existing `AcademicAssessment.StudentApp` components (test reuse)
  - Configure dependency injection
  - Set up HTTP client for API calls
- **Acceptance:** Empty MAUI app builds and runs
- **Dependencies:** Task 1

### Task 3: Port Assessment Taking Screen to MAUI

- **Description:** Convert `AssessmentTaking.razor` component to MAUI
- **Files to Migrate:**
  - `AcademicAssessment.StudentApp/Pages/AssessmentTaking.razor` → MAUI
  - `QuestionRenderer.razor` → Test reusability
  - `AnswerInput.razor` → Test reusability
  - `AssessmentTimer.razor` → Test reusability
- **Changes Needed:**
  - Replace SignalR with REST API polling (MAUI doesn't use persistent connections)
  - Adapt touch interactions (tap vs click)
  - Optimize for small screens
  - Test platform-specific code (Android permissions)
- **Acceptance:** Assessment screen works on Android device
- **Dependencies:** Task 2

### Task 4: Measure Code Reuse Percentage

- **Description:** Quantify how much code was reused vs rewritten
- **Metrics:**

  ```
  Code Reuse % = (Reused Lines / Total Lines) × 100
  
  Categories:
  - 100% Reused: Copy-paste from web, no changes
  - 80-99% Reused: Minor adaptations (styling, touch events)
  - 50-79% Reused: Significant adaptations
  - <50% Reused: Mostly rewritten
  - 0% Reused: Platform-specific (Android services, etc.)
  ```

- **Document:**
  - Total lines of code in original web component
  - Total lines of code in MAUI component
  - Breakdown by reuse category
  - Examples of what needed adaptation
- **Acceptance:** Detailed code reuse report
- **Dependencies:** Task 3

### Task 5: Performance Benchmarking

- **Description:** Measure app performance on real devices
- **Metrics to Collect:**
  - App startup time (cold start, warm start)
  - Screen navigation time
  - API call latency (same as web)
  - UI rendering time (60 FPS target)
  - Memory usage
  - Battery drain (1-hour assessment session)
- **Test Devices:**
  - Android Emulator (baseline)
  - Mid-range Android phone (e.g., Samsung Galaxy A54)
  - Budget Android phone (e.g., 2-3 years old)
- **Comparison:**
  - MAUI performance vs web app in mobile browser
  - Identify any performance bottlenecks
- **Acceptance:** Performance report with benchmarks
- **Dependencies:** Task 3

### Task 6: Design Gamification System

- **Description:** Design XP, streaks, badges, and leaderboard system
- **Database Schema:**

  ```sql
  CREATE TABLE student_gamification (
      student_id UUID PRIMARY KEY REFERENCES students(id),
      total_xp INT DEFAULT 0,
      level INT DEFAULT 1,
      current_streak_days INT DEFAULT 0,
      longest_streak_days INT DEFAULT 0,
      last_activity_date DATE,
      created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
  );
  
  CREATE TABLE achievements (
      id UUID PRIMARY KEY,
      name VARCHAR(100) NOT NULL,
      description TEXT,
      icon_url VARCHAR(255),
      xp_value INT NOT NULL,
      unlock_criteria JSONB -- e.g., {"assessment_count": 10}
  );
  
  CREATE TABLE student_achievements (
      student_id UUID REFERENCES students(id),
      achievement_id UUID REFERENCES achievements(id),
      unlocked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
      PRIMARY KEY (student_id, achievement_id)
  );
  
  CREATE TABLE leaderboards (
      id UUID PRIMARY KEY,
      student_id UUID REFERENCES students(id),
      leaderboard_type VARCHAR(50), -- 'Global', 'School', 'Class'
      scope_id UUID, -- school_id or class_id
      rank INT,
      xp INT,
      week_start DATE,
      updated_at TIMESTAMP
  );
  ```

- **XP Calculations:**
  - Complete assessment: 100 XP base + (accuracy × 50)
  - Daily streak: +10 XP per day
  - Perfect score: +50 XP bonus
  - Time bonus: +20 XP if completed <50% of time limit
- **Achievements (20+ examples):**
  - "First Steps" - Complete first assessment
  - "Perfect Score" - Get 100% on assessment
  - "Hot Streak" - 7-day streak
  - "Marathon" - 30-day streak
  - "Subject Master" - Complete 50 math assessments
  - "Night Owl" - Complete assessment after 10pm
  - "Early Bird" - Complete assessment before 7am
- **Leaderboards:**
  - Global (all students)
  - School (same school)
  - Class (same class)
  - Weekly reset (Monday 00:00 UTC)
- **Acceptance:** Complete gamification design document
- **Dependencies:** None (parallel with Tasks 1-5)

### Task 7: Design Offline Sync Strategy

- **Description:** Architecture for offline assessments with sync
- **Requirements:**
  - Student can download assessment while online
  - Complete assessment offline (airplane mode)
  - Sync answers when back online
  - Handle conflicts (if assessment changed server-side)
- **Architecture:**

  ```
  Online Mode:
  1. Fetch assessment from API
  2. Store in local SQLite database
  3. Mark as "downloaded"
  
  Offline Mode:
  1. Load assessment from SQLite
  2. Save answers to SQLite (pending sync)
  3. Queue sync operation
  
  Sync Mode:
  1. Detect network connectivity
  2. Upload pending answers to API
  3. Resolve conflicts (last-write-wins or prompt user)
  4. Mark as "synced"
  5. Delete local copy
  ```

- **SQLite Schema:**

  ```sql
  CREATE TABLE cached_assessments (
      assessment_id TEXT PRIMARY KEY,
      assessment_data TEXT, -- JSON serialized assessment
      downloaded_at INTEGER, -- Unix timestamp
      expires_at INTEGER
  );
  
  CREATE TABLE pending_responses (
      id TEXT PRIMARY KEY,
      student_id TEXT,
      assessment_id TEXT,
      question_id TEXT,
      response_data TEXT, -- JSON serialized response
      answered_at INTEGER,
      synced BOOLEAN DEFAULT 0
  );
  ```

- **Conflict Resolution:**
  - If assessment modified server-side → Discard local cache, prompt re-download
  - If answer already submitted → Show error, don't overwrite
  - Otherwise → Last-write-wins
- **Acceptance:** Offline sync architecture document with diagrams
- **Dependencies:** None (parallel)

### Task 8: Design Push Notification Strategy

- **Description:** Architecture for daily reminders and progress updates
- **Push Notification Types:**
  - Daily reminder: "Complete today's assessment to keep your streak!"
  - Streak milestone: "🔥 You're on a 7-day streak! Keep it up!"
  - New assessment available: "New Math assessment available"
  - Achievement unlocked: "🏆 Achievement unlocked: Perfect Score!"
  - Leaderboard update: "You moved up to #5 in your class!"
- **Implementation:**
  - **Android:** Firebase Cloud Messaging (FCM)
  - **iOS (future):** Apple Push Notification Service (APNS)
  - **Backend:** Azure Notification Hubs or custom service
- **User Preferences:**
  - Enable/disable notifications
  - Notification time (default: 6pm)
  - Notification types (assessment reminders, achievements, etc.)
- **Acceptance:** Push notification design document
- **Dependencies:** None (parallel)

### Task 9: Compare MAUI vs Alternatives

- **Description:** Objective comparison of MAUI, React Native, and PWA
- **Comparison Matrix:**

| Factor | MAUI | React Native | PWA |
|--------|------|--------------|-----|
| Code reuse from web | 80-90% | 0% (UI), 30% (logic) | 90% (same web code) |
| Development time | 12-16 weeks | 16-20 weeks | 2-3 weeks |
| Performance | Native | Native | Near-native |
| Offline support | Excellent | Excellent | Limited |
| Push notifications | Full support | Full support | Android only |
| App store presence | Yes | Yes | No |
| Ecosystem size | Medium (growing) | Large | N/A |
| Team learning curve | Low (C#) | High (React Native) | Low (web tech) |
| Maintenance | Single codebase | Separate codebase | Same as web |

- **Recommendation:** Document which approach is best for EduMind.AI
- **Acceptance:** Comparison document with recommendation
- **Dependencies:** Tasks 4-5 (need MAUI performance data)

### Task 10: Create Effort Estimate for Full Implementation

- **Description:** Detailed effort estimate for complete mobile app
- **Stories to Estimate:**
  - Story 018: Core MAUI app (assessment taking, results, dashboard)
  - Story 019: Gamification implementation
  - Story 020: Offline sync
  - Story 021: Push notifications
  - Story 022: Google Play Store release
  - Story 023: iOS version (future)
- **Breakdown by Task:**
  - Setup and configuration
  - UI component migration
  - Platform-specific features
  - Testing (unit, integration, E2E)
  - QA and bug fixes
  - App store submission
- **Risk Factors:**
  - Unknown MAUI issues (±20% contingency)
  - Device compatibility testing
  - App store approval delays
- **Acceptance:** Effort estimate spreadsheet with ±20% accuracy
- **Dependencies:** Tasks 4-9

### Task 11: Write Technical Feasibility Report

- **Description:** Comprehensive report documenting investigation findings
- **Sections:**
  1. **Executive Summary** (1 page)
     - Key findings
     - Recommendation (go/no-go)
     - Effort estimate
     - Timeline
  2. **Proof-of-Concept Results**
     - Code reuse analysis
     - Performance benchmarks
     - Screenshots/demo video
  3. **Architecture Design**
     - Gamification system
     - Offline sync strategy
     - Push notifications
  4. **Comparison Analysis**
     - MAUI vs alternatives
     - Pros/cons
  5. **Implementation Plan**
     - Phased approach
     - Story breakdown
     - Dependencies
  6. **Risks and Mitigations**
  7. **Cost-Benefit Analysis**
     - Development cost
     - User acquisition benefit
     - Competitive positioning
  8. **Recommendation**
     - Go/no-go decision
     - Rationale
- **Acceptance:** Professional report (PDF, 15-20 pages)
- **Dependencies:** Tasks 1-10

### Task 12: Create Demo Video

- **Description:** Record demo of POC running on Android device
- **Content:**
  - App startup
  - Navigation to assessment
  - Taking assessment (various question types)
  - Submit and view results
  - Performance comparison (MAUI vs web browser)
- **Length:** 3-5 minutes
- **Format:** MP4, 1080p
- **Acceptance:** Demo video uploaded to internal documentation
- **Dependencies:** Task 3

### Task 13: Present Findings to Stakeholders

- **Description:** Present report and demo to decision makers
- **Attendees:**
  - Technical lead
  - Product owner
  - Stakeholders
- **Agenda:**
  - Demo video (5 min)
  - Key findings (10 min)
  - Q&A (15 min)
  - Go/no-go decision
- **Deliverables:**
  - Presentation slides (10-15 slides)
  - Feasibility report (printed copies)
- **Outcome:** Clear go/no-go decision documented
- **Acceptance:** Meeting held, decision recorded
- **Dependencies:** Tasks 11-12

---

## Acceptance Criteria (Investigation Complete)

### Technical Validation

- [ ] MAUI POC works on Android emulator
- [ ] MAUI POC works on 2 real Android devices
- [ ] Code reuse measured (target: >75%)
- [ ] Performance benchmarks collected
- [ ] No critical blockers identified

### Deliverables

- [ ] Technical feasibility report (PDF)
- [ ] Demo video (3-5 minutes)
- [ ] Gamification design document
- [ ] Offline sync architecture document
- [ ] Push notification design document
- [ ] MAUI vs alternatives comparison
- [ ] Effort estimate for full implementation (Stories 018-023)

### Decision

- [ ] Go/no-go decision documented
- [ ] If GO: Stories 018-023 created in backlog
- [ ] If NO-GO: Alternative approach documented (PWA or React Native)

---

## Context & References

### Documentation

- [ADR 001: Retain Blazor Over React](.github/adr/001-retain-blazor-over-react.md)
- [Story 006: Student Onboarding with Google OAuth](.github/story/006/issue.md)
- [Executive Summary](.github/specification/01-executive-summary.md)

### External References

- [.NET MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)
- [Blazor Hybrid Apps](https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/)
- [Firebase Cloud Messaging](https://firebase.google.com/docs/cloud-messaging)
- [SQLite in .NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/data-cloud/database-sqlite)

### Competitive Analysis

- [Duolingo Mobile Experience](https://www.duolingo.com)
- [Khan Academy Mobile App](https://www.khanacademy.org/about/mobile-apps)
- [Quizlet Mobile](https://quizlet.com/mobile)

---

## Notes

- **This is an investigation story, not implementation:** Goal is to validate feasibility and make informed go/no-go decision
- **Timeline flexibility:** Can extend to 3 weeks if deeper investigation needed
- **Budget for devices:** May need to purchase 1-2 Android devices for testing (~$300-500)
- **Post-investigation:** If GO decision, Stories 018-023 will be created for full implementation
- **Risk tolerance:** Mobile app is strategic but not critical for initial launch; we can proceed cautiously

---

## Success Definition

This investigation is successful if:

1. **Clear recommendation:** Unambiguous go/no-go decision with rationale
2. **Actionable data:** Enough detail to confidently estimate full implementation
3. **Risk identification:** All major technical risks identified and mitigated
4. **Stakeholder buy-in:** Decision makers aligned on mobile strategy

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot  
**Last Updated:** 2025-10-25
