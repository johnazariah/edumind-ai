# Push Notification Design

**Version:** 1.0  
**Date:** 2025-10-27  
**Status:** Draft

---

## Executive Summary

This document defines the push notification strategy for EduMind.AI mobile app. Push notifications will increase engagement, remind students to practice, celebrate achievements, and keep them informed about new content and progress updates.

**Goals:**
- Increase daily active users (DAU) through timely reminders
- Celebrate achievements to reinforce positive behavior
- Re-engage lapsed users
- Provide value without being spammy
- Respect user preferences and privacy

---

## 1. Notification Types

### 1.1 Overview

| Type | Purpose | Frequency | Priority | User Control |
|------|---------|-----------|----------|--------------|
| Daily Reminder | Encourage daily practice | Daily | High | Time preference |
| Streak Alert | Prevent streak loss | Daily (if inactive) | High | On/Off |
| Achievement | Celebrate unlocks | Per achievement | Medium | On/Off |
| New Content | Inform about new assessments | Weekly | Low | On/Off |
| Leaderboard | Share rank changes | Weekly | Low | On/Off |
| Custom | Teacher/parent messages | As needed | High | Cannot disable |

### 1.2 Detailed Notification Types

#### 1.2.1 Daily Reminder

**Purpose:** Remind students to complete daily assessment to maintain streak

**Trigger:** 
- Send at user's preferred time (default: 6:00 PM local time)
- Only if no activity that day
- Skip if user has completed assessment already

**Message Templates:**
```
"🎓 Time to learn! Complete today's assessment to keep your {streak_days}-day streak alive!"

"📚 Don't forget! {minutes_left} minutes until your streak resets. Quick practice?"

"🌟 {display_name}, your daily challenge awaits! Keep your {streak_days}-day streak going."

"🔥 Streak reminder: Complete an assessment today to maintain your momentum!"
```

**Personalization:**
- Include current streak count
- Include time remaining in day
- Use student's display name

#### 1.2.2 Streak Alert

**Purpose:** Urgent reminder when streak is about to break

**Trigger:**
- Send 2 hours before streak reset (10:00 PM local time)
- Only if no activity that day
- Only if streak >= 3 days

**Message Templates:**
```
"⚠️ Streak Alert! Your {streak_days}-day streak ends in 2 hours. Don't lose it!"

"🚨 Last chance! Complete a quick assessment to save your {streak_days}-day streak."

"⏰ {streak_days}-day streak in danger! Just 2 hours left to keep it alive."
```

**Urgency:** High priority notification with sound/vibration

#### 1.2.3 Achievement Unlocked

**Purpose:** Celebrate when student unlocks an achievement

**Trigger:**
- Immediately after achievement unlocks
- While app is in background

**Message Templates:**
```
"🏆 Achievement Unlocked: {achievement_name}! Tap to view your reward."

"🎉 Congratulations! You've earned the '{achievement_name}' badge +{xp_value} XP!"

"💎 New Achievement: {achievement_name}. You're on fire!"
```

**Rich Notification:** Include achievement icon/image

#### 1.2.4 Streak Milestone

**Purpose:** Celebrate reaching streak milestones

**Trigger:**
- When streak reaches milestone (7, 14, 30, 90, 180, 365 days)
- Immediately after completing assessment that reaches milestone

**Message Templates:**
```
"🔥 Amazing! You've reached a {streak_days}-day streak! Keep going!"

"🏅 Milestone Alert: {streak_days} consecutive days! You're unstoppable!"

"👑 Incredible! {streak_days}-day streak achieved. You're a learning champion!"
```

#### 1.2.5 Level Up

**Purpose:** Celebrate when student reaches new level

**Trigger:**
- Immediately after leveling up
- Include both old and new level

**Message Templates:**
```
"⬆️ Level Up! You're now Level {new_level}. +{xp_needed_for_next} XP to next level."

"🚀 Congratulations! You've advanced to Level {new_level}!"

"⭐ Level {new_level} Achieved! You earned {xp_gained} XP this week."
```

#### 1.2.6 New Assessment Available

**Purpose:** Inform students when new assessment is assigned

**Trigger:**
- When teacher assigns new assessment to class
- Batch notifications (send once per day with count)

**Message Templates:**
```
"📋 New assessment available: {assessment_title}. Ready to start?"

"✨ {teacher_name} assigned a new challenge: {assessment_title}"

"📝 {count} new assessments available. Check them out!"
```

#### 1.2.7 Leaderboard Update

**Purpose:** Inform students about leaderboard rank changes

**Trigger:**
- Weekly (Monday morning)
- Only if rank changed significantly (moved up/down 3+ positions)
- Only for students who opted into leaderboards

**Message Templates:**
```
"📊 You moved up to johnazariah/edumind-ai#{new_rank} in your class leaderboard! Keep it up!"

"🏆 Great work! You're now johnazariah/edumind-ai#{new_rank} in your class (+{positions_gained} spots)."

"📉 You dropped to johnazariah/edumind-ai#{new_rank}. Time to catch up with some practice!"
```

**Sentiment:**
- Positive for rank improvements
- Encouraging (not negative) for rank drops

#### 1.2.8 Re-engagement

**Purpose:** Bring back inactive users

**Trigger:**
- User hasn't opened app in 7 days
- Send once per week maximum
- Stop after 4 attempts

**Message Templates:**
```
"👋 We miss you! Your classmates have completed {count} assessments this week."

"📚 Come back and continue your learning journey! New content is waiting."

"🎓 It's been a while! Let's get back on track together."
```

#### 1.2.9 Custom/Teacher Messages

**Purpose:** Allow teachers to send custom messages to students

**Trigger:**
- Teacher initiates from dashboard
- Can target individual, class, or all students

**Examples:**
```
"📢 {teacher_name}: Class project due Friday. Don't forget to complete the review assessment!"

"🎉 {teacher_name}: Great job on yesterday's quiz, everyone! Keep up the good work."
```

**Priority:** High (cannot be disabled by students)

---

## 2. Technical Architecture

### 2.1 Platform-Specific Implementation

```
┌─────────────────────────────────────────────────────┐
│             EduMind.AI Backend                       │
│  ┌───────────────────────────────────────────────┐  │
│  │      Notification Service                      │  │
│  │  • Schedule notifications                      │  │
│  │  • Manage templates                            │  │
│  │  • Track delivery/opens                        │  │
│  └───────────────┬───────────────────────────────┘  │
│                  ↓                                   │
│  ┌───────────────────────────────────────────────┐  │
│  │   Azure Notification Hubs / Firebase           │  │
│  │   • Route to FCM/APNS                          │  │
│  │   • Handle device registration                 │  │
│  │   • Manage notification tags                   │  │
│  └───────────────┬───────────────────────────────┘  │
└──────────────────┼──────────────────────────────────┘
                   ↓
         ┌─────────┴─────────┐
         ↓                   ↓
┌────────────────┐   ┌────────────────┐
│ Firebase Cloud │   │ Apple Push     │
│ Messaging (FCM)│   │ Notification   │
│ (Android)      │   │ Service (APNS) │
│                │   │ (iOS - Future) │
└───────┬────────┘   └────────┬───────┘
        ↓                     ↓
┌────────────────┐   ┌────────────────┐
│ Android Device │   │  iOS Device    │
└────────────────┘   └────────────────┘
```

### 2.2 Firebase Cloud Messaging (FCM) Setup

**Step 1: Register Device**

```csharp
public class NotificationService : INotificationService
{
    private readonly IFirebaseMessaging _firebaseMessaging;
    private readonly IHttpClient _httpClient;
    
    public async Task<Result<string>> RegisterDeviceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get FCM token
            var token = await _firebaseMessaging.GetTokenAsync();
            
            if (string.IsNullOrEmpty(token))
                return Result.Failure<string>("Failed to get FCM token");
            
            // Register token with backend
            var request = new
            {
                DeviceToken = token,
                Platform = "Android",
                StudentId = _authService.CurrentUserId
            };
            
            var response = await _httpClient.PostAsync("/api/notifications/register", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
                return Result.Failure<string>("Failed to register device with backend");
            
            return Result.Success(token);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Device registration failed: {ex.Message}");
        }
    }
}
```

**Step 2: Handle Incoming Notifications**

```csharp
[Service(Exported = true)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class FirebaseNotificationService : FirebaseMessagingService
{
    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);
        
        // Extract notification data
        var title = message.GetNotification()?.Title ?? "EduMind.AI";
        var body = message.GetNotification()?.Body ?? "";
        var data = message.Data;
        
        // Create local notification
        var notificationBuilder = new NotificationCompat.Builder(this, "edumind_default")
            .SetContentTitle(title)
            .SetContentText(body)
            .SetSmallIcon(Resource.Drawable.ic_notification)
            .SetAutoCancel(true)
            .SetPriority(NotificationCompat.PriorityHigh);
        
        // Add action based on notification type
        if (data.ContainsKey("notification_type"))
        {
            var notificationType = data["notification_type"];
            var intent = CreateIntentForNotificationType(notificationType, data);
            
            var pendingIntent = PendingIntent.GetActivity(
                this, 
                0, 
                intent, 
                PendingIntentFlags.Immutable);
            
            notificationBuilder.SetContentIntent(pendingIntent);
        }
        
        // Show notification
        var notificationManager = NotificationManagerCompat.From(this);
        notificationManager.Notify(GenerateNotificationId(), notificationBuilder.Build());
        
        // Track notification received
        TrackNotificationReceived(message);
    }
    
    private Intent CreateIntentForNotificationType(string notificationType, IDictionary<string, string> data)
    {
        var intent = new Intent(this, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
        
        return notificationType switch
        {
            "daily_reminder" => intent.PutExtra("navigate_to", "assessment_list"),
            "achievement" => intent.PutExtra("navigate_to", "achievements")
                                   .PutExtra("achievement_id", data.GetValueOrDefault("achievement_id")),
            "leaderboard" => intent.PutExtra("navigate_to", "leaderboard"),
            "new_assessment" => intent.PutExtra("navigate_to", "assessment_detail")
                                       .PutExtra("assessment_id", data.GetValueOrDefault("assessment_id")),
            _ => intent
        };
    }
}
```

---

## 3. Backend Implementation

### 3.1 Database Schema

```sql
-- Device registration
CREATE TABLE notification_devices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    device_token TEXT NOT NULL,
    platform VARCHAR(20) NOT NULL, -- 'Android', 'iOS'
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_used_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(device_token),
    INDEX idx_student_devices (student_id, is_active)
);

-- Notification preferences
CREATE TABLE notification_preferences (
    student_id UUID PRIMARY KEY REFERENCES students(id) ON DELETE CASCADE,
    daily_reminder_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    daily_reminder_time TIME NOT NULL DEFAULT '18:00:00',
    streak_alerts_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    achievement_notifications_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    new_content_notifications_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    leaderboard_notifications_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    timezone VARCHAR(50) NOT NULL DEFAULT 'UTC',
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Scheduled notifications (for daily reminders, etc.)
CREATE TABLE scheduled_notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    notification_type VARCHAR(50) NOT NULL,
    scheduled_at TIMESTAMP NOT NULL,
    sent_at TIMESTAMP,
    status VARCHAR(20) NOT NULL DEFAULT 'pending', -- 'pending', 'sent', 'failed', 'cancelled'
    message_data JSONB NOT NULL,
    error_message TEXT,
    
    INDEX idx_scheduled_pending (status, scheduled_at) WHERE status = 'pending',
    INDEX idx_student_scheduled (student_id, scheduled_at)
);

-- Notification delivery log
CREATE TABLE notification_deliveries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    student_id UUID NOT NULL REFERENCES students(id) ON DELETE CASCADE,
    notification_type VARCHAR(50) NOT NULL,
    title TEXT NOT NULL,
    body TEXT NOT NULL,
    sent_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    opened_at TIMESTAMP,
    device_token TEXT,
    status VARCHAR(20) NOT NULL, -- 'sent', 'delivered', 'opened', 'failed'
    
    INDEX idx_student_deliveries (student_id, sent_at DESC),
    INDEX idx_notification_analytics (notification_type, sent_at, status)
);
```

### 3.2 Notification Scheduling Service

```csharp
public class NotificationScheduler : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScheduleDailyRemindersAsync(stoppingToken);
                await ProcessScheduledNotificationsAsync(stoppingToken);
                await CheckStreakAlertsAsync(stoppingToken);
                
                // Wait 1 minute before next run
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification scheduler");
            }
        }
    }
    
    private async Task ScheduleDailyRemindersAsync(CancellationToken cancellationToken)
    {
        // Get all students with daily reminders enabled
        var students = await _dbContext.Students
            .Include(s => s.NotificationPreferences)
            .Include(s => s.GamificationProfile)
            .Where(s => s.NotificationPreferences.DailyReminderEnabled)
            .ToListAsync(cancellationToken);
        
        foreach (var student in students)
        {
            var prefs = student.NotificationPreferences;
            var scheduledTime = CalculateScheduledTime(prefs.DailyReminderTime, prefs.Timezone);
            
            // Check if notification already scheduled for today
            var existing = await _dbContext.ScheduledNotifications
                .Where(sn => sn.StudentId == student.Id 
                    && sn.NotificationType == "daily_reminder"
                    && sn.ScheduledAt.Date == DateOnly.FromDateTime(scheduledTime).ToDateTime(TimeOnly.MinValue))
                .FirstOrDefaultAsync(cancellationToken);
            
            if (existing == null)
            {
                // Schedule new notification
                var notification = new ScheduledNotification
                {
                    StudentId = student.Id,
                    NotificationType = "daily_reminder",
                    ScheduledAt = scheduledTime,
                    MessageData = JsonSerializer.Serialize(new
                    {
                        StreakDays = student.GamificationProfile.CurrentStreakDays,
                        DisplayName = student.DisplayName
                    })
                };
                
                await _dbContext.ScheduledNotifications.AddAsync(notification, cancellationToken);
            }
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
    
    private async Task ProcessScheduledNotificationsAsync(CancellationToken cancellationToken)
    {
        // Get notifications ready to send
        var now = DateTime.UtcNow;
        var notifications = await _dbContext.ScheduledNotifications
            .Where(sn => sn.Status == "pending" && sn.ScheduledAt <= now)
            .OrderBy(sn => sn.ScheduledAt)
            .Take(100)
            .ToListAsync(cancellationToken);
        
        foreach (var notification in notifications)
        {
            // Check if student already completed assessment today (for daily reminders)
            if (notification.NotificationType == "daily_reminder")
            {
                var hasActivity = await CheckStudentActivityTodayAsync(notification.StudentId, cancellationToken);
                if (hasActivity)
                {
                    // Cancel notification
                    notification.Status = "cancelled";
                    continue;
                }
            }
            
            // Send notification
            var result = await _notificationSender.SendNotificationAsync(
                notification.StudentId,
                notification.NotificationType,
                notification.MessageData,
                cancellationToken);
            
            notification.Status = result.IsSuccess ? "sent" : "failed";
            notification.SentAt = DateTime.UtcNow;
            notification.ErrorMessage = result.IsSuccess ? null : result.Error;
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
```

### 3.3 Notification Sender

```csharp
public class NotificationSender : INotificationSender
{
    public async Task<Result> SendNotificationAsync(
        Guid studentId,
        string notificationType,
        string messageDataJson,
        CancellationToken cancellationToken = default)
    {
        // Get student's devices
        var devices = await _dbContext.NotificationDevices
            .Where(nd => nd.StudentId == studentId && nd.IsActive)
            .ToListAsync(cancellationToken);
        
        if (!devices.Any())
            return Result.Failure("No active devices registered");
        
        // Generate message from template
        var message = await GenerateMessageAsync(notificationType, messageDataJson, cancellationToken);
        
        // Send to all devices
        var tasks = devices.Select(device => SendToDeviceAsync(device, message, cancellationToken));
        var results = await Task.WhenAll(tasks);
        
        // Log delivery
        foreach (var device in devices)
        {
            await LogNotificationDeliveryAsync(studentId, device.DeviceToken, notificationType, message, results.Any(r => r.IsSuccess));
        }
        
        return results.Any(r => r.IsSuccess) ? Result.Success() : Result.Failure("Failed to send to any device");
    }
    
    private async Task<Result> SendToDeviceAsync(
        NotificationDevice device,
        NotificationMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            if (device.Platform == "Android")
            {
                return await SendViaFCMAsync(device.DeviceToken, message, cancellationToken);
            }
            else if (device.Platform == "iOS")
            {
                return await SendViaAPNSAsync(device.DeviceToken, message, cancellationToken);
            }
            else
            {
                return Result.Failure($"Unsupported platform: {device.Platform}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to device {DeviceToken}", device.DeviceToken);
            return Result.Failure($"Send failed: {ex.Message}");
        }
    }
    
    private async Task<Result> SendViaFCMAsync(
        string deviceToken,
        NotificationMessage message,
        CancellationToken cancellationToken)
    {
        var fcmMessage = new Message
        {
            Token = deviceToken,
            Notification = new Notification
            {
                Title = message.Title,
                Body = message.Body
            },
            Data = message.Data,
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    ChannelId = "edumind_default",
                    Sound = "default"
                }
            }
        };
        
        var response = await FirebaseMessaging.DefaultInstance.SendAsync(fcmMessage, cancellationToken);
        return Result.Success();
    }
}
```

---

## 4. User Preferences UI

### 4.1 Settings Screen

```
┌────────────────────────────────────┐
│ Notification Settings              │
├────────────────────────────────────┤
│ Daily Reminder                     │
│ Remind me to practice every day    │
│ [ON]            Time: 6:00 PM →   │
│                                    │
│ Streak Alerts                      │
│ Alert me when my streak is at risk │
│ [ON]                               │
│                                    │
│ Achievement Notifications          │
│ Celebrate when I unlock badges     │
│ [ON]                               │
│                                    │
│ New Content                        │
│ Notify me about new assessments    │
│ [ON]                               │
│                                    │
│ Leaderboard Updates                │
│ Weekly updates on my rank          │
│ [OFF]                              │
│                                    │
│ Teacher Messages                   │
│ Always receive (cannot disable)    │
│ [Always ON]                        │
└────────────────────────────────────┘
```

### 4.2 Time Picker

```
┌────────────────────────────────────┐
│ Daily Reminder Time                │
├────────────────────────────────────┤
│ When should we remind you?         │
│                                    │
│      ┌────┐   ┌────┐              │
│      │ 06 │ : │ 00 │ [PM ▼]       │
│      └────┘   └────┘              │
│                                    │
│ We'll send a reminder at this time │
│ if you haven't practiced yet today.│
│                                    │
│ [Cancel]              [Save]       │
└────────────────────────────────────┘
```

---

## 5. Analytics &amp; Optimization

### 5.1 Key Metrics

- **Delivery Rate:** Percentage of notifications successfully delivered
- **Open Rate:** Percentage of notifications opened
- **Conversion Rate:** Percentage leading to app open
- **Opt-Out Rate:** Percentage of users disabling notifications
- **Time to Open:** Average time between send and open

### 5.2 A/B Testing

Test different message templates:

```
Variant A: "🎓 Time to learn! Complete today's assessment."
Variant B: "🔥 Keep your 5-day streak alive! Practice now."
Variant C: "📚 Your daily challenge is ready. Start learning!"

Track:
- Open rate per variant
- Conversion to assessment completion
- Statistical significance (p < 0.05)
```

### 5.3 Optimization Strategies

1. **Send Time Optimization:**
   - Track open rates by hour of day
   - Adjust default time based on user behavior
   - Personalize send time per user

2. **Message Personalization:**
   - Include student name
   - Reference current streak
   - Mention specific subjects they're studying

3. **Frequency Capping:**
   - Max 3 notifications per day
   - Respect "Do Not Disturb" hours (10 PM - 7 AM)
   - Batch notifications when possible

---

## 6. Compliance &amp; Privacy

### 6.1 COPPA (Students Under 13)

- Require parental consent for push notifications
- Limited notification types (no social/leaderboard)
- No personalized data in notifications

### 6.2 GDPR

- Opt-in required (not opt-out)
- Clear explanation of what notifications will be sent
- Easy way to disable all notifications
- Data retention: Notification logs deleted after 90 days

### 6.3 Platform Requirements

**Android:**
- Request POST_NOTIFICATIONS permission (Android 13+)
- Create notification channels
- Respect notification importance

**iOS (Future):**
- Request notification authorization
- Handle provisional authorization
- Critical alerts (requires special entitlement)

---

## 7. Testing Strategy

### 7.1 Unit Tests

- Notification scheduling logic
- Message template generation
- Time zone conversions
- Preference handling

### 7.2 Integration Tests

- FCM message sending
- Device registration/deregistration
- Notification delivery tracking
- Background job execution

### 7.3 Manual Testing

1. **Notification Flow:**
   - Register device
   - Trigger notification from backend
   - Verify device receives notification
   - Open notification
   - Verify deep link works

2. **Preferences:**
   - Change notification settings
   - Verify notifications respect preferences
   - Disable all notifications
   - Verify no notifications sent

3. **Time Zones:**
   - Set different time zones
   - Verify notifications sent at correct local time

---

## 8. Implementation Phases

### Phase 1: Basic Notifications (2 weeks)
- Firebase setup
- Device registration
- Daily reminder notifications
- Basic preferences UI

### Phase 2: Achievement Notifications (1 week)
- Achievement unlock notifications
- Level up notifications
- Streak milestone notifications

### Phase 3: Advanced Notifications (1 week)
- Leaderboard update notifications
- New content notifications
- Re-engagement notifications

### Phase 4: Optimization (1 week)
- A/B testing framework
- Analytics dashboard
- Send time optimization

**Total Estimated Effort:** 5 weeks

---

## 9. Risks &amp; Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| Spam complaints | High | Medium | Frequency caps, easy opt-out |
| Low open rates | Medium | Medium | A/B testing, personalization |
| FCM service disruption | High | Low | Retry logic, fallback to email |
| Privacy concerns | High | Low | Clear consent, minimal data |
| Battery drain | Medium | Low | Batch notifications, optimize frequency |

---

## Conclusion

This push notification design provides a comprehensive strategy to increase engagement while respecting user preferences and privacy. The phased implementation allows for iterative development and optimization.

**Recommendation:** Start with daily reminders and achievement notifications. Gather user feedback and optimize send times and message templates based on analytics.

**Next Steps:**
1. Set up Firebase project and obtain credentials
2. Implement device registration in mobile app
3. Build notification scheduling service
4. Create preferences UI
5. Monitor analytics and iterate
