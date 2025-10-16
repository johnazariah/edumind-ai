# GitHub Project Setup Guide

This guide shows how to set up a GitHub Project board to track the 6-week sprint.

## Quick Setup with GitHub CLI

### Step 1: Create the Project

```bash
# Create project
gh project create --owner johnazariah --title "EduMind AI - 6 Week Sprint" --body "Complete orchestrator, build student UI, integration testing, and Azure deployment"
```

### Step 2: Add Custom Fields

```bash
# Get project number from the URL after creation, then:
PROJECT_NUMBER=<your-project-number>

# Add custom fields
gh project field-create $PROJECT_NUMBER --owner johnazariah --name "Priority" --data-type "SINGLE_SELECT" --single-select-options "P1 - Critical,P2 - High,P3 - Medium,P4 - Low"
gh project field-create $PROJECT_NUMBER --owner johnazariah --name "Week" --data-type "SINGLE_SELECT" --single-select-options "Week 1,Week 2,Week 3,Week 4,Week 5,Week 6"
gh project field-create $PROJECT_NUMBER --owner johnazariah --name "Effort" --data-type "SINGLE_SELECT" --single-select-options "1 day,2 days,3 days,1 week"
gh project field-create $PROJECT_NUMBER --owner johnazariah --name "Sprint Phase" --data-type "SINGLE_SELECT" --single-select-options "Orchestrator,Student UI,Testing,Deployment"
```

### Step 3: Create Issues from Tasks

Run the script below to create all issues:

```bash
#!/bin/bash
# File: scripts/create-sprint-issues.sh

REPO="johnazariah/edumind-ai"

# Week 1: Orchestrator Logic
echo "Creating Week 1 issues..."

gh issue create --repo $REPO --title "Task 1.1: Implement orchestrator decision-making algorithm" \
  --body "Implement intelligent subject agent selection based on student performance, difficulty adjustment logic, and learning path optimization.

Files: src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs

Acceptance Criteria:
- [ ] Agent selection algorithm implemented
- [ ] Difficulty adjustment working
- [ ] Learning path optimization active
- [ ] Unit tests passing

Week: 1, Days: 1-2, Priority: P1" \
  --label "orchestrator,week-1,priority-1" \
  --assignee "@me"

gh issue create --repo $REPO --title "Task 1.2: Complete task routing implementation" \
  --body "Implement RouteTaskToAgent() method with agent capability matching, fallback selection, and task priority queuing.

Files: src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs

Acceptance Criteria:
- [ ] RouteTaskToAgent() implemented
- [ ] Capability matching working
- [ ] Fallback logic in place
- [ ] Priority queue functional

Week: 1, Days: 1-2, Priority: P1" \
  --label "orchestrator,week-1,priority-1" \
  --assignee "@me"

gh issue create --repo $REPO --title "Task 1.3: Add state management" \
  --body "Implement orchestrator state persistence, checkpoint/resume functionality, and transaction boundaries.

Files: src/AcademicAssessment.Orchestration/StudentProgressOrchestrator.cs

Acceptance Criteria:
- [ ] State persistence implemented
- [ ] Checkpoint/resume working
- [ ] Transaction boundaries defined
- [ ] Tests verify state management

Week: 1, Days: 1-2, Priority: P1" \
  --label "orchestrator,week-1,priority-1" \
  --assignee "@me"

gh issue create --repo $REPO --title "Task 1.4: Complete A2A protocol implementation" \
  --body "Implement agent discovery, message passing, request/response patterns, and broadcast capabilities.

Files: src/AcademicAssessment.Agents/Shared/A2ABaseAgent.cs

Acceptance Criteria:
- [ ] Agent discovery working
- [ ] Message passing implemented
- [ ] Request/response patterns functional
- [ ] Broadcast capabilities added

Week: 1, Days: 3-4, Priority: P1" \
  --label "agents,a2a,week-1,priority-1" \
  --assignee "@me"

gh issue create --repo $REPO --title "Task 1.5: Implement SignalR hub for real-time updates" \
  --body "Add progress notification methods, connection management, and group-based messaging.

Files: src/AcademicAssessment.Web/Hubs/AgentProgressHub.cs

Acceptance Criteria:
- [ ] Progress notifications working
- [ ] Connection management robust
- [ ] Group messaging functional
- [ ] Real-time updates verified

Week: 1, Days: 3-4, Priority: P1" \
  --label "signalr,week-1,priority-1" \
  --assignee "@me"

gh issue create --repo $REPO --title "Task 1.6: Add agent health monitoring" \
  --body "Implement heartbeat mechanism, availability tracking, and automatic failover.

Files: src/AcademicAssessment.Agents/Shared/A2ABaseAgent.cs

Acceptance Criteria:
- [ ] Heartbeat mechanism active
- [ ] Availability tracking working
- [ ] Automatic failover tested

Week: 1, Days: 3-4, Priority: P1" \
  --label "agents,monitoring,week-1,priority-1" \
  --assignee "@me"

gh issue create --repo $REPO --title "Task 1.7: Implement progress tracking" \
  --body "Add milestone tracking, performance metrics collection, and learning velocity calculations.

Files: src/AcademicAssessment.Orchestration/Services/ProgressTracker.cs

Acceptance Criteria:
- [ ] Milestone tracking implemented
- [ ] Metrics collection working
- [ ] Learning velocity calculated
- [ ] Dashboard shows progress

Week: 1, Day: 5, Priority: P1" \
  --label "orchestrator,analytics,week-1,priority-1" \
  --assignee "@me"

gh issue create --repo $REPO --title "Task 1.8: Add orchestrator analytics" \
  --body "Track decision-making effectiveness, measure agent utilization, implement A/B testing framework.

Files: src/AcademicAssessment.Orchestration/Services/OrchestratorAnalytics.cs

Acceptance Criteria:
- [ ] Decision tracking implemented
- [ ] Agent utilization measured
- [ ] A/B testing framework ready

Week: 1, Day: 5, Priority: P2" \
  --label "orchestrator,analytics,week-1,priority-2" \
  --assignee "@me"

gh issue create --repo $REPO --title "Task 1.9: Implement comprehensive error handling" \
  --body "Add retry logic with exponential backoff, circuit breaker pattern, graceful degradation, and dead letter queue.

Files: src/AcademicAssessment.Orchestration/

Acceptance Criteria:
- [ ] Retry logic with backoff working
- [ ] Circuit breaker implemented
- [ ] Graceful degradation tested
- [ ] Dead letter queue functional

Week: 1, Day: 6, Priority: P1" \
  --label "orchestrator,resilience,week-1,priority-1" \
  --assignee "@me"

gh issue create --repo $REPO --title "Task 1.10: Add logging and observability" \
  --body "Implement structured logging, distributed tracing with OpenTelemetry, and custom metrics.

Files: src/AcademicAssessment.Orchestration/

Acceptance Criteria:
- [ ] Structured logging throughout
- [ ] Distributed tracing active
- [ ] Custom metrics published
- [ ] Dashboards created

Week: 1, Day: 6, Priority: P2" \
  --label "orchestrator,observability,week-1,priority-2" \
  --assignee "@me"

gh issue create --repo $REPO --title "Task 1.11: Unit tests for orchestrator" \
  --body "Test decision-making algorithms, state transitions, and error scenarios.

Files: tests/AcademicAssessment.Orchestration.Tests/

Acceptance Criteria:
- [ ] Decision-making tests passing
- [ ] State transition tests passing
- [ ] Error scenario tests passing
- [ ] >80% code coverage

Week: 1, Day: 7, Priority: P1" \
  --label "orchestrator,testing,week-1,priority-1" \
  --assignee "@me"

gh issue create --repo $REPO --title "Task 1.12: Integration tests for A2A communication" \
  --body "Test multi-agent workflows, orchestrator→agent and agent→agent communication.

Files: tests/AcademicAssessment.Tests.Integration/

Acceptance Criteria:
- [ ] Multi-agent workflows tested
- [ ] Orchestrator communication verified
- [ ] Agent-to-agent communication working
- [ ] All tests passing

Week: 1, Day: 7, Priority: P1" \
  --label "agents,testing,week-1,priority-1" \
  --assignee "@me"

gh issue create --repo $REPO --title "Task 1.13: Update orchestrator documentation" \
  --body "Document orchestration algorithms, create architecture diagrams, add API documentation.

Files: docs/

Acceptance Criteria:
- [ ] Algorithm documentation complete
- [ ] Architecture diagrams created
- [ ] API docs published
- [ ] Examples provided

Week: 1, Day: 7, Priority: P2" \
  --label "documentation,week-1,priority-2" \
  --assignee "@me"

echo "Week 1 issues created! ✅"
echo ""
echo "Continue with Week 2-6 issues..."
echo "For brevity, showing pattern. Repeat for all tasks in SPRINT_ROADMAP.md"
```

### Step 4: Create Project Views

1. **Board View by Status**
   - Columns: Backlog, Todo, In Progress, In Review, Done
   
2. **Timeline View by Week**
   - Group by: Week
   - Sort by: Priority

3. **Table View - All Tasks**
   - Show all fields
   - Filter and sort capabilities

## Manual Setup (Alternative)

If you prefer the web UI:

1. Go to https://github.com/johnazariah/edumind-ai
2. Click "Projects" tab
3. Click "New project"
4. Choose "Board" template
5. Name it "EduMind AI - 6 Week Sprint"
6. Add columns: Backlog, Todo, In Progress, In Review, Done
7. Add custom fields as listed above
8. Create issues manually from SPRINT_ROADMAP.md

## Daily Workflow

### Morning Standup
```bash
# See today's tasks
gh issue list --repo johnazariah/edumind-ai --assignee "@me" --state open --label "in-progress"

# Move task to "In Progress"
gh issue edit <issue-number> --add-label "in-progress"
```

### Task Completion
```bash
# When done, move to "In Review"
gh issue edit <issue-number> --remove-label "in-progress" --add-label "in-review"

# After review, close
gh issue close <issue-number> --comment "Completed and merged to main"
```

### End of Week Review
```bash
# See week's completed tasks
gh issue list --repo johnazariah/edumind-ai --state closed --label "week-1"

# See remaining tasks
gh issue list --repo johnazariah/edumind-ai --state open --label "week-1"
```

## Integration with VS Code

Install the GitHub Pull Requests and Issues extension:
```bash
code --install-extension GitHub.vscode-pull-request-github
```

Then you can:
- View and create issues directly in VS Code
- Link commits to issues
- Track progress without leaving the editor

## Automation

### Auto-close on Merge
Add to commit messages:
```
fix: Implement orchestrator decision-making algorithm

Closes #123
```

### Progress Updates
Use issue comments for daily updates:
```bash
gh issue comment <issue-number> --body "Progress: Implemented agent selection logic. Next: difficulty adjustment."
```

## Useful Queries

### Burndown Chart Data
```bash
# Issues completed this week
gh issue list --repo johnazariah/edumind-ai --state closed --label "week-1" --json number,title,closedAt

# Issues remaining
gh issue list --repo johnazariah/edumind-ai --state open --label "week-1" --json number,title
```

### Blocked Tasks
```bash
# See blocked items
gh issue list --repo johnazariah/edumind-ai --state open --label "blocked"
```

## References

- GitHub Projects Docs: https://docs.github.com/en/issues/planning-and-tracking-with-projects
- GitHub CLI Docs: https://cli.github.com/manual/
- Sprint Roadmap: `/docs/SPRINT_ROADMAP.md`

---

**Ready to start tracking! Let's ship this! 🚀**
