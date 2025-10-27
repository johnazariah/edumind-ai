# Gamification System Design

**Version:** 1.0  
**Date:** 2025-10-27  
**Status:** Draft

---

## Executive Summary

This document defines a comprehensive gamification system for EduMind.AI inspired by successful educational platforms like Duolingo, Khan Academy, and Quizlet. The system uses XP (experience points), streaks, achievements/badges, and leaderboards to increase engagement and motivation.

**Key Design Principles:**
- Simple and intuitive for students (ages 13+)
- Balanced rewards that encourage consistent practice without being exploitable
- Privacy-conscious leaderboards with opt-in participation
- Fair competition within peer groups (class, school)
- Positive reinforcement over punishment

---

## 1. Experience Points (XP) System

### 1.1 XP Award Calculations

```
Base XP Formula:
  XP = BaseXP + AccuracyBonus + TimeBonus + StreakBonus + PerfectScoreBonus

Components:
  BaseXP          = 100 (for completing any assessment)
  AccuracyBonus   = (Accuracy% × 50)
  TimeBonus       = (CompletionTime < 50% of TimeLimit) ? 20 : 0
  StreakBonus     = CurrentStreak × 10
  PerfectScoreBonus = (Accuracy == 100%) ? 50 : 0

Example:
  - Student completes assessment with 85% accuracy in 20 min (limit: 60 min)
  - Current streak: 5 days
  - XP = 100 + (0.85 × 50) + 20 + (5 × 10) + 0 = 212.5 ≈ 213 XP
```

### 1.2 Level System

Students progress through levels based on total accumulated XP:

| Level | XP Required | XP Range | Average Assessments |
|-------|-------------|----------|---------------------|
| 1     | 0           | 0-99     | Starting level      |
| 2     | 100         | 100-249  | 1 assessment        |
| 3     | 250         | 250-499  | 2-3 assessments     |
| 4     | 500         | 500-999  | 4-6 assessments     |
| 5     | 1,000       | 1K-1.9K  | 7-12 assessments    |
| 10    | 5,000       | 5K-9.9K  | ~30 assessments     |
| 15    | 15,000      | 15K-29K  | ~90 assessments     |
| 20    | 50,000      | 50K-99K  | ~300 assessments    |
| 25    | 100,000     | 100K+    | ~600+ assessments   |

**Level Formula:** `Level = floor(1 + log2(TotalXP / 100))`

This creates a logarithmic curve where levels become progressively harder, maintaining long-term engagement.

---

## 2. Streak System

### 2.1 Streak Rules

- **Daily Activity:** Complete at least 1 assessment per day (00:00-23:59 local time)
- **Streak Count:** Number of consecutive days with activity
- **Streak Freeze:** Students can "freeze" their streak for 1 day (premium feature or achievement reward)
- **Streak Recovery:** Students lose streak if inactive for 24+ hours (no recovery)

### 2.2 Streak Milestones

| Streak Days | Achievement | Bonus XP | Badge |
|-------------|-------------|----------|-------|
| 3           | Hot Start   | 30       | 🔥    |
| 7           | One Week    | 70       | 🔥🔥  |
| 14          | Two Weeks   | 140      | 🔥🔥🔥 |
| 30          | One Month   | 300      | 🏆    |
| 90          | Three Months| 900      | 💎    |
| 180         | Half Year   | 1,800    | 👑    |
| 365         | One Year    | 3,650    | 🌟    |

### 2.3 Streak Calculation Logic

```typescript
function updateStreak(student: Student, today: Date): void {
  const lastActivity = student.lastActivityDate;
  const daysSinceActivity = daysBetween(lastActivity, today);
  
  if (daysSinceActivity === 0) {
    // Same day, no change
    return;
  } else if (daysSinceActivity === 1) {
    // Consecutive day, increment streak
    student.currentStreakDays++;
    student.longestStreakDays = Math.max(student.currentStreakDays, student.longestStreakDays);
  } else if (daysSinceActivity > 1 && !student.hasStreakFreeze) {
    // Missed a day, reset streak
    student.currentStreakDays = 1;
  } else if (daysSinceActivity > 1 && student.hasStreakFreeze) {
    // Used streak freeze
    student.hasStreakFreeze = false;
    student.currentStreakDays++;
  }
  
  student.lastActivityDate = today;
}
```

---

## 3. Achievement System

### 3.1 Achievement Categories

#### 3.1.1 Milestone Achievements
- **First Steps** - Complete first assessment (10 XP)
- **Getting Started** - Complete 5 assessments (50 XP)
- **Committed Learner** - Complete 25 assessments (250 XP)
- **Dedicated Student** - Complete 100 assessments (1,000 XP)
- **Master Scholar** - Complete 500 assessments (5,000 XP)

#### 3.1.2 Performance Achievements
- **Perfect Score** - Get 100% on any assessment (50 XP)
- **Perfect Streak** - Get 100% on 3 consecutive assessments (150 XP)
- **Speed Demon** - Complete assessment in <25% of time limit (75 XP)
- **Accuracy Master** - Get >95% accuracy on 10 assessments (500 XP)
- **Perfectionist** - Get 100% on 10 assessments (1,000 XP)

#### 3.1.3 Subject-Specific Achievements
- **Math Novice** - Complete 10 math assessments (100 XP)
- **Math Expert** - Complete 50 math assessments (500 XP)
- **Math Master** - Complete 100 math assessments (1,000 XP)
- (Similar for Science, English, History, etc.)

#### 3.1.4 Streak Achievements
- **Hot Start** - 3-day streak (30 XP)
- **One Week Warrior** - 7-day streak (70 XP)
- **Monthly Dedication** - 30-day streak (300 XP)
- **Unstoppable** - 100-day streak (1,000 XP)
- **Legendary** - 365-day streak (3,650 XP)

#### 3.1.5 Time-of-Day Achievements
- **Early Bird** - Complete assessment before 7 AM (25 XP)
- **Night Owl** - Complete assessment after 10 PM (25 XP)
- **Lunch Break Learner** - Complete assessment between 12-1 PM (15 XP)

#### 3.1.6 Social Achievements
- **Team Player** - Join a class leaderboard (20 XP)
- **Top 10** - Reach top 10 in class leaderboard (100 XP)
- **Champion** - Reach johnazariah/edumind-ai#1 in class leaderboard (500 XP)

#### 3.1.7 Special Achievements
- **Weekend Warrior** - Complete assessments on both Saturday and Sunday (50 XP)
- **Marathon Session** - Complete 5 assessments in one day (250 XP)
- **Diverse Learner** - Complete assessments in 5 different subjects (200 XP)

### 3.2 Achievement Data Model

```json
{
  "id": "uuid",
  "name": "Perfect Score",
  "description": "Achieve 100% accuracy on an assessment",
  "icon": "🏆",
  "iconUrl": "/icons/achievements/perfect-score.svg",
  "category": "Performance",
  "xpValue": 50,
  "unlockCriteria": {
    "type": "single_assessment",
    "condition": {
      "accuracy": 100
    }
  },
  "rarity": "Uncommon"
}
```

---

## 4. Leaderboard System

### 4.1 Leaderboard Types

| Type | Scope | Reset Period | Privacy |
|------|-------|--------------|---------|
| Global | All students | Weekly (Monday 00:00 UTC) | Opt-in |
| School | Same school | Weekly | Default |
| Class | Same class | Weekly | Default |
| Personal | Individual progress | Never | Private |

### 4.2 Leaderboard Ranking Logic

**Weekly XP Calculation:**
```sql
SELECT 
    s.id,
    s.display_name,
    SUM(ar.xp_earned) as weekly_xp,
    COUNT(ar.id) as assessments_completed,
    RANK() OVER (ORDER BY SUM(ar.xp_earned) DESC) as rank
FROM students s
JOIN assessment_responses ar ON s.id = ar.student_id
WHERE ar.submitted_at >= date_trunc('week', CURRENT_TIMESTAMP)
  AND s.school_id = @schoolId
GROUP BY s.id, s.display_name
ORDER BY weekly_xp DESC
LIMIT 100;
```

### 4.3 Privacy Controls

- **Display Name:** Students can choose a display name (not real name)
- **Opt-Out:** Students can hide from global leaderboard
- **Class Default:** Students visible to classmates by default (can opt-out)
- **Avatar:** Students choose an avatar (no photos for minors)

### 4.4 Leaderboard Tiers

To prevent discouragement, divide leaderboards into tiers:

| Tier | XP Range | Rank Range |
|------|----------|------------|
| Diamond | 2,000+ weekly XP | Top 1-10 |
| Platinum | 1,000-1,999 XP | Top 11-25 |
| Gold | 500-999 XP | Top 26-50 |
| Silver | 250-499 XP | Top 51-100 |
| Bronze | 100-249 XP | Top 101-200 |
| Participant | <100 XP | 201+ |

Students compete within their tier and can move up/down based on performance.

---

## 5. Database Schema

### 5.1 Core Tables

```sql
-- Gamification profile for each student
CREATE TABLE student_gamification (
    student_id UUID PRIMARY KEY REFERENCES students(id) ON DELETE CASCADE,
    total_xp INT NOT NULL DEFAULT 0,
    level INT NOT NULL DEFAULT 1,
    current_streak_days INT NOT NULL DEFAULT 0,
    longest_streak_days INT NOT NULL DEFAULT 0,
    last_activity_date DATE,
    has_streak_freeze BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Indexes
    INDEX idx_level (level),
    INDEX idx_streak (current_streak_days),
    INDEX idx_last_activity (last_activity_date)
);

-- Achievement definitions (seeded data)
CREATE TABLE achievements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    description TEXT NOT NULL,
    icon VARCHAR(10), -- Emoji or icon code
    icon_url VARCHAR(255),
    category VARCHAR(50) NOT NULL, -- 'Milestone', 'Performance', 'Streak', etc.
    xp_value INT NOT NULL,
    unlock_criteria JSONB NOT NULL,
    rarity VARCHAR(20) NOT NULL DEFAULT 'Common', -- 'Common', 'Uncommon', 'Rare', 'Epic', 'Legendary'
    is_secret BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Indexes
    INDEX idx_category (category),
    INDEX idx_rarity (rarity)
);

-- Student achievement unlocks
CREATE TABLE student_achievements (
    student_id UUID REFERENCES students(id) ON DELETE CASCADE,
    achievement_id UUID REFERENCES achievements(id) ON DELETE CASCADE,
    unlocked_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    seen BOOLEAN NOT NULL DEFAULT FALSE, -- For "new achievement" notifications
    
    PRIMARY KEY (student_id, achievement_id),
    
    -- Indexes
    INDEX idx_unlocked_at (unlocked_at),
    INDEX idx_unseen (student_id, seen) WHERE seen = FALSE
);

-- Leaderboard entries (materialized view, updated hourly)
CREATE TABLE leaderboards (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID REFERENCES students(id) ON DELETE CASCADE,
    leaderboard_type VARCHAR(50) NOT NULL, -- 'Global', 'School', 'Class'
    scope_id UUID, -- school_id or class_id (null for Global)
    rank INT NOT NULL,
    weekly_xp INT NOT NULL,
    assessments_completed INT NOT NULL,
    week_start DATE NOT NULL,
    tier VARCHAR(20), -- 'Diamond', 'Platinum', 'Gold', 'Silver', 'Bronze', 'Participant'
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Indexes
    INDEX idx_leaderboard_lookup (leaderboard_type, scope_id, week_start, rank),
    INDEX idx_student_leaderboards (student_id, week_start),
    
    -- Unique constraint
    UNIQUE (student_id, leaderboard_type, scope_id, week_start)
);

-- XP transaction log (audit trail)
CREATE TABLE xp_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID REFERENCES students(id) ON DELETE CASCADE,
    xp_amount INT NOT NULL,
    source_type VARCHAR(50) NOT NULL, -- 'Assessment', 'Achievement', 'Streak', 'Bonus'
    source_id UUID, -- assessment_response_id or achievement_id
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- Indexes
    INDEX idx_student_transactions (student_id, created_at DESC),
    INDEX idx_source (source_type, source_id)
);
```

### 5.2 Existing Table Modifications

Add XP tracking to existing assessment responses:

```sql
ALTER TABLE assessment_responses 
ADD COLUMN xp_earned INT NOT NULL DEFAULT 0,
ADD COLUMN achievement_ids UUID[] DEFAULT '{}'; -- Achievements unlocked by this response
```

---

## 6. Backend Services

### 6.1 GamificationService

```csharp
public interface IGamificationService
{
    // XP Management
    Task<Result<int>> AwardXPAsync(Guid studentId, int xpAmount, string source, Guid? sourceId = null, CancellationToken cancellationToken = default);
    Task<Result<StudentGamificationProfile>> GetProfileAsync(Guid studentId, CancellationToken cancellationToken = default);
    
    // Streaks
    Task<Result> UpdateStreakAsync(Guid studentId, DateOnly activityDate, CancellationToken cancellationToken = default);
    Task<Result<bool>> UseStreakFreezeAsync(Guid studentId, CancellationToken cancellationToken = default);
    
    // Achievements
    Task<Result<List<Achievement>>> CheckAchievementsAsync(Guid studentId, AssessmentResponse response, CancellationToken cancellationToken = default);
    Task<Result<List<StudentAchievement>>> GetStudentAchievementsAsync(Guid studentId, bool includeUnlocked = true, CancellationToken cancellationToken = default);
    Task<Result> MarkAchievementAsSeenAsync(Guid studentId, Guid achievementId, CancellationToken cancellationToken = default);
    
    // Leaderboards
    Task<Result<List<LeaderboardEntry>>> GetLeaderboardAsync(LeaderboardType type, Guid? scopeId = null, DateOnly? weekStart = null, int limit = 100, CancellationToken cancellationToken = default);
    Task<Result<LeaderboardEntry>> GetStudentRankAsync(Guid studentId, LeaderboardType type, Guid? scopeId = null, DateOnly? weekStart = null, CancellationToken cancellationToken = default);
    Task<Result> RefreshLeaderboardsAsync(CancellationToken cancellationToken = default);
}
```

### 6.2 Achievement Evaluation Engine

```csharp
public class AchievementEvaluator
{
    public async Task<List<Achievement>> EvaluateAsync(
        Student student,
        AssessmentResponse response,
        StudentGamificationProfile profile)
    {
        var unlockedAchievements = new List<Achievement>();
        var allAchievements = await _achievementRepository.GetAllAsync();
        var studentAchievements = await _achievementRepository.GetStudentAchievementsAsync(student.Id);
        var studentAchievementIds = new HashSet<Guid>(studentAchievements.Select(a => a.AchievementId));
        
        foreach (var achievement in allAchievements.Where(a => !studentAchievementIds.Contains(a.Id)))
        {
            if (await IsAchievementUnlockedAsync(achievement, student, response, profile))
            {
                unlockedAchievements.Add(achievement);
                await _achievementRepository.UnlockAchievementAsync(student.Id, achievement.Id);
            }
        }
        
        return unlockedAchievements;
    }
    
    private async Task<bool> IsAchievementUnlockedAsync(
        Achievement achievement,
        Student student,
        AssessmentResponse response,
        StudentGamificationProfile profile)
    {
        var criteria = JsonSerializer.Deserialize<AchievementCriteria>(achievement.UnlockCriteria);
        
        return criteria.Type switch
        {
            "single_assessment" => EvaluateSingleAssessment(criteria, response),
            "assessment_count" => await EvaluateAssessmentCount(criteria, student.Id),
            "streak" => EvaluateStreak(criteria, profile),
            "time_of_day" => EvaluateTimeOfDay(criteria, response),
            "subject_mastery" => await EvaluateSubjectMastery(criteria, student.Id),
            _ => false
        };
    }
}
```

---

## 7. UI Components

### 7.1 Mobile UI Screens

#### Dashboard Widget
```
┌─────────────────────────────────┐
│ 🔥 5-Day Streak                 │
│ ⭐ Level 12 (8,450 XP)          │
│ 📊 johnazariah/edumind-ai#3 in Class Leaderboard      │
│ [View Profile →]                │
└─────────────────────────────────┘
```

#### Post-Assessment Summary
```
┌─────────────────────────────────┐
│ ✅ Assessment Complete!         │
│                                 │
│ Score: 85%                      │
│ +213 XP 🎉                      │
│                                 │
│ 🏆 Achievement Unlocked!        │
│ "Speed Demon"                   │
│ +75 XP                          │
│                                 │
│ Current Streak: 6 days 🔥       │
│ [Continue →]                    │
└─────────────────────────────────┘
```

#### Leaderboard Screen
```
┌─────────────────────────────────┐
│ Class Leaderboard (This Week)   │
│ [Global] [School] [Class] ←     │
├─────────────────────────────────┤
│ 🥇 AliceM        2,450 XP       │
│ 🥈 BobJ          2,120 XP       │
│ 🥉 YOU           1,890 XP ⭐    │
│ 4️⃣  CharlieK     1,650 XP       │
│ 5️⃣  DianaL       1,420 XP       │
│ ...                             │
└─────────────────────────────────┘
```

### 7.2 Web UI Integration

Add gamification widgets to existing web app:
- XP progress bar in header
- Streak counter in sidebar
- Achievement notifications (toast)
- Leaderboard page

---

## 8. Implementation Phases

### Phase 1: Core XP System (2 weeks)
- Database schema
- `GamificationService` implementation
- XP award logic
- Level calculation
- Basic UI widget

### Phase 2: Streak System (1 week)
- Streak tracking logic
- Daily activity check
- Streak freeze feature
- Streak milestone achievements

### Phase 3: Achievement System (2 weeks)
- Achievement seeding (20+ achievements)
- Achievement evaluation engine
- Unlock notifications
- Achievement gallery UI

### Phase 4: Leaderboard System (2 weeks)
- Leaderboard calculation
- Weekly reset logic
- Tier system
- Leaderboard UI
- Privacy controls

### Phase 5: Polish &amp; Testing (1 week)
- UI polish
- Performance optimization
- Analytics integration
- A/B testing setup

**Total Estimated Effort:** 8 weeks

---

## 9. Analytics &amp; Metrics

### 9.1 Key Metrics to Track

- **Engagement:**
  - Daily Active Users (DAU)
  - Weekly Active Users (WAU)
  - Average session duration
  - Assessments per user per week
  
- **Gamification:**
  - Average streak length
  - Achievement unlock rate
  - Leaderboard participation rate
  - XP distribution (histogram)
  
- **Retention:**
  - 7-day retention
  - 30-day retention
  - Churn rate
  - Streaks broken vs maintained

### 9.2 Success Criteria

- **30% increase in DAU** after gamification launch
- **50% of students** reach 7-day streak within first month
- **70% participation** in class leaderboards
- **20% decrease** in churn rate

---

## 10. Privacy &amp; Compliance

### 10.1 COPPA Compliance

For students under 13:
- No global leaderboard participation (only class)
- No display names visible outside class
- No social features (friend requests, direct messages)
- Parental consent required for leaderboard participation

### 10.2 FERPA Compliance

- Leaderboard data not considered educational records
- Students use pseudonyms, not real names
- Opt-out available at any time
- No academic performance data visible (only XP)

### 10.3 GDPR Compliance

- Right to erasure: Delete all gamification data
- Right to data portability: Export gamification profile
- Right to opt-out: Disable leaderboards
- Minimal data collection: Only XP, streaks, achievements

---

## 11. Risks &amp; Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Students game the system (repeated easy assessments) | High | Medium | Cap XP per subject per day, diminishing returns |
| Discouragement from low leaderboard rank | High | Medium | Tier system, focus on personal progress |
| Privacy concerns | High | Low | Strong opt-in/opt-out, pseudonyms, no photos |
| Performance issues (leaderboard queries) | Medium | Medium | Materialized views, caching, pagination |
| Achievement spam (too many unlocks) | Low | Medium | Rarity system, balance unlock frequency |

---

## 12. Future Enhancements

- **Social Features:** Friend leaderboards, challenge friends
- **Badges:** Visual badges for profiles (collect and display)
- **Seasons:** Quarterly competitions with special rewards
- **Avatars:** Customizable avatars unlocked with achievements
- **Power-Ups:** Temporary boosts (2x XP, streak freeze)
- **Guilds/Teams:** Students form teams to compete together

---

## Conclusion

This gamification system provides a comprehensive framework for increasing student engagement through XP, streaks, achievements, and leaderboards. The design balances motivation with fairness, privacy, and compliance with educational regulations.

**Recommendation:** Implement in phases, starting with Core XP and Streak systems. Gather user feedback and iterate on achievement and leaderboard designs.

**Next Steps:**
1. Review and approve this design
2. Create database migration scripts
3. Implement `GamificationService`
4. Design and implement UI components
5. Launch beta to select classes for feedback
