# Integration Testing Plan - Phase 5 Multi-Agent Workflow

**Date**: October 15, 2025  
**Scope**: Test all 5 subject agents with OLLAMA semantic evaluation  
**Status**: Ready to Execute

---

## Objectives

1. ✅ Verify all 5 agents registered and discoverable
2. ✅ Test OLLAMA semantic evaluation across subjects
3. ✅ Validate StudentProgressOrchestrator task routing
4. ✅ Confirm multi-subject assessment workflow
5. ✅ Measure OLLAMA performance per subject

---

## Test Environment

### Prerequisites Checklist

- [x] OLLAMA installed and running (localhost:11434)
- [x] Llama 3.2 3B model pulled
- [x] All 5 subject agents implemented and registered
- [x] StudentProgressOrchestrator configured
- [x] Web API building successfully
- [x] Database schema up-to-date (metadata migration applied)

### Quick Verification

```bash
# Check OLLAMA is running
curl http://localhost:11434/api/tags

# Verify Llama 3.2 3B model
# Should return: {"models":[{"name":"llama3.2:3b",...}]}

# Build solution
cd /workspaces/edumind-ai
dotnet build EduMind.AI.sln

# Expected: Build succeeded, 0 errors
```

---

## Test Suite 1: Agent Discovery & Registration

### Test 1.1: Verify All Agents Registered

**Purpose**: Confirm all 5 subject agents are registered in DI container

**Execution**:

```bash
# Start Web API
cd /workspaces/edumind-ai
dotnet run --project src/AcademicAssessment.Web
```

**Expected Log Output**:

```
[INF] LLM Service configured: OllamaService (local AI, zero cost)
[INF] A2A Agent infrastructure, orchestrator, and 5 LLM-enhanced subject agents configured (Math, Physics, Chemistry, Biology, English)
```

**Verification**: Check startup logs for agent registration message

**Status**: ⏳ Pending

---

### Test 1.2: Agent Discovery via Orchestrator

**Purpose**: Verify StudentProgressOrchestrator can discover all agents

**Test Code**:

```csharp
// In integration test or manual verification
var orchestrator = app.Services.GetRequiredService<StudentProgressOrchestrator>();
var agentCards = await orchestrator.DiscoverAgentsAsync();

// Expected: 5 agents returned
Assert.Equal(5, agentCards.Count);
Assert.Contains(agentCards, c => c.Name == "MathematicsAssessmentAgent");
Assert.Contains(agentCards, c => c.Name == "PhysicsAssessmentAgent");
Assert.Contains(agentCards, c => c.Name == "ChemistryAssessmentAgent");
Assert.Contains(agentCards, c => c.Name == "BiologyAssessmentAgent");
Assert.Contains(agentCards, c => c.Name == "EnglishAssessmentAgent");
```

**Status**: ⏳ Pending

---

## Test Suite 2: OLLAMA Semantic Evaluation

### Test 2.1: Mathematics Agent - Semantic Evaluation

**Purpose**: Test OLLAMA understands mathematical equivalence

**Test Cases**:

| Question | Correct Answer | Student Answer | Expected Score | Reason |
|----------|----------------|----------------|----------------|--------|
| "2x + 5 = 15, solve for x" | "5" | "5" | 1.0 | Exact match |
| "2x + 5 = 15, solve for x" | "5" | "x = 5" | 0.9-1.0 | Semantically equivalent |
| "2x + 5 = 15, solve for x" | "5" | "five" | 0.8-1.0 | Word form |
| "2x + 5 = 15, solve for x" | "5" | "7" | 0.0 | Incorrect |
| "2x + 5 = 15, solve for x" | "5" | "5 (solved by subtracting 5 then dividing by 2)" | 0.9-1.0 | Correct with explanation |

**Execution**:

```bash
# Manual test via API
curl -X POST http://localhost:5001/api/v1/agents/mathematics/evaluate \
  -H "Content-Type: application/json" \
  -d '{
    "questionText": "Solve for x: 2x + 5 = 15",
    "correctAnswer": "5",
    "studentAnswer": "x = 5",
    "points": 10
  }'
```

**Expected Response**:

```json
{
  "isCorrect": true,
  "pointsEarned": 9.5,
  "feedback": "Excellent! Your answer is correct. You correctly solved the linear equation.",
  "evaluationMethod": "semantic_llm"
}
```

**Status**: ⏳ Pending

---

### Test 2.2: Physics Agent - Units & Notation

**Purpose**: Test OLLAMA handles physics units and scientific notation

**Test Cases**:

| Question | Correct Answer | Student Answer | Expected Score | Reason |
|----------|----------------|----------------|----------------|--------|
| "Speed of light in m/s?" | "299792458 m/s" | "3×10^8 m/s" | 0.9-1.0 | Scientific notation |
| "Speed of light in m/s?" | "299792458 m/s" | "299,792,458 m/s" | 1.0 | Formatting difference |
| "Speed of light in m/s?" | "299792458 m/s" | "300000000 m/s" | 0.7-0.9 | Close approximation |
| "Kinetic energy formula?" | "KE = 1/2 mv²" | "KE = 0.5 × mass × velocity²" | 0.9-1.0 | Equivalent expression |

**Status**: ⏳ Pending

---

### Test 2.3: Chemistry Agent - Chemical Formulas

**Purpose**: Test OLLAMA recognizes chemical notation

**Test Cases**:

| Question | Correct Answer | Student Answer | Expected Score | Reason |
|----------|----------------|----------------|----------------|--------|
| "Water formula?" | "H2O" | "H₂O" | 1.0 | Unicode subscripts |
| "Water formula?" | "H2O" | "H2O" | 1.0 | ASCII notation |
| "Water formula?" | "H2O" | "water" | 0.5-0.7 | Partial credit |
| "Balanceequation: H2 + O2 → H2O" | "2H2 + O2 → 2H2O" | "2H₂ + O₂ → 2H₂O" | 1.0 | Unicode notation |

**Status**: ⏳ Pending

---

### Test 2.4: Biology Agent - Concept Understanding

**Purpose**: Test OLLAMA understands biological terminology

**Test Cases**:

| Question | Correct Answer | Student Answer | Expected Score | Reason |
|----------|----------------|----------------|----------------|--------|
| "Powerhouse of the cell?" | "mitochondria" | "mitochondrion" | 1.0 | Singular/plural |
| "Powerhouse of the cell?" | "mitochondria" | "the mitochondria" | 1.0 | Article difference |
| "Powerhouse of the cell?" | "mitochondria" | "Mitochondria" | 1.0 | Case insensitive |
| "Photosynthesis location?" | "chloroplast" | "in the chloroplasts of plant cells" | 0.9-1.0 | More detailed answer |

**Status**: ⏳ Pending

---

### Test 2.5: English Agent - Semantic Understanding

**Purpose**: Test OLLAMA recognizes equivalent literary concepts

**Test Cases**:

| Question | Correct Answer | Student Answer | Expected Score | Reason |
|----------|----------------|----------------|----------------|--------|
| "What is a simile?" | "comparison using like or as" | "A simile compares things using 'like' or 'as'" | 0.9-1.0 | Equivalent definition |
| "What is a simile?" | "comparison using like or as" | "comparing two things with like/as" | 0.9-1.0 | Shortened form |
| "Identify the verb" | "run" | "running" | 0.7-0.9 | Different form |
| "Shakespeare play with Romeo?" | "Romeo and Juliet" | "Romeo & Juliet" | 1.0 | Formatting difference |

**Status**: ⏳ Pending

---

## Test Suite 3: Performance Metrics

### Test 3.1: OLLAMA Response Times

**Purpose**: Measure OLLAMA evaluation latency per subject

**Execution**:

```bash
# Measure 10 evaluations per agent
for agent in mathematics physics chemistry biology english; do
  echo "Testing $agent agent..."
  for i in {1..10}; do
    time curl -s -X POST http://localhost:5001/api/v1/agents/$agent/evaluate \
      -H "Content-Type: application/json" \
      -d @test_data/${agent}_question_${i}.json \
      > /dev/null
  done
done
```

**Expected Results** (CPU-only):

| Agent | Avg Response Time | Max Response Time |
|-------|-------------------|-------------------|
| Mathematics | 15-20s | <25s |
| Physics | 15-20s | <25s |
| Chemistry | 15-20s | <25s |
| Biology | 15-20s | <25s |
| English | 15-20s | <25s |

**With GPU**: Should be <3s per evaluation

**Status**: ⏳ Pending

---

### Test 3.2: Evaluation Quality Assessment

**Purpose**: Measure OLLAMA accuracy vs exact match

**Test Data**: 50 questions per subject (250 total)

**Metrics**:

- **Precision**: Correct positive evaluations / Total positive evaluations
- **Recall**: Correct positive evaluations / Total actually correct answers
- **F1 Score**: Harmonic mean of precision and recall
- **Agreement with Human Graders**: % matching human judgment

**Target**:

- Precision: >90%
- Recall: >85%
- F1 Score: >87%
- Human Agreement: >85%

**Status**: ⏳ Pending

---

## Test Suite 4: Multi-Agent Workflow

### Test 4.1: Cross-Subject Assessment

**Purpose**: Test orchestrator routing to multiple agents

**Test Scenario**:

1. Create assessment with questions from all 5 subjects
2. Student completes assessment
3. Orchestrator routes each question to appropriate agent
4. Each agent evaluates using OLLAMA
5. Results aggregated and returned

**Test Data**:

- 2 Math questions
- 2 Physics questions
- 2 Chemistry questions
- 2 Biology questions
- 2 English questions
- Total: 10 questions

**Expected Behavior**:

- Orchestrator identifies correct agent for each subject
- All evaluations use OLLAMA semantic evaluation
- Results include per-subject breakdown
- Overall score calculated correctly

**API Endpoint**:

```bash
POST /api/v1/assessments/{assessmentId}/submit
```

**Status**: ⏳ Pending

---

### Test 4.2: Concurrent Agent Execution

**Purpose**: Verify agents can process questions concurrently

**Test Scenario**:

- Submit 10-question assessment (2 per subject)
- Orchestrator should process questions concurrently
- Measure total evaluation time

**Expected**:

- Sequential: ~150-200s (10 questions × 15-20s each)
- Concurrent: ~20-30s (max single agent time + overhead)

**Status**: ⏳ Pending

---

## Test Suite 5: Error Handling & Fallback

### Test 5.1: OLLAMA Service Unavailable

**Purpose**: Test fallback to exact match when OLLAMA fails

**Test Steps**:

1. Stop OLLAMA service
2. Submit evaluation request
3. Verify fallback to exact match
4. Check error logging

**Expected Behavior**:

- Agent detects OLLAMA failure
- Falls back to exact match evaluation
- Returns evaluation with `evaluationMethod: "exact_match_fallback"`
- Logs warning about OLLAMA unavailability

**Status**: ⏳ Pending

---

### Test 5.2: OLLAMA Timeout

**Purpose**: Test timeout handling for slow OLLAMA responses

**Test Steps**:

1. Configure short timeout (5s)
2. Submit complex evaluation
3. Verify timeout triggers fallback

**Expected Behavior**:

- Timeout after configured duration
- Fallback to exact match
- No hanging requests
- Timeout logged

**Status**: ⏳ Pending

---

## Test Suite 6: Metadata Integration

### Test 6.1: Board-Specific Content

**Purpose**: Test metadata filtering by educational board

**Test Data**:

- Create 5 CBSE questions (one per subject)
- Create 5 ICSE questions (one per subject)
- Create 5 IB questions (one per subject)

**Test Queries**:

```csharp
// Get all CBSE questions
var cbseQuestions = await questionRepository.GetByBoardAsync("CBSE");

// Get CBSE Math questions for Grade 8
var cbseMath8 = await questionRepository.GetByBoardAndSubjectAsync(
    "CBSE", Subject.Mathematics, GradeLevel.Grade8
);
```

**Expected**:

- Correct filtering by board
- Fast query performance (indexed)
- Mixed-board assessments supported

**Status**: ⏳ Pending

---

### Test 6.2: Module-Based Organization

**Purpose**: Test content organization by module

**Test Scenarios**:

- Create "Algebra" module questions
- Create "Geometry" module questions
- Filter assessments by module
- Generate module-specific reports

**Status**: ⏳ Pending

---

## Success Criteria

### Must Pass (Blocking)

- [ ] All 5 agents register successfully
- [ ] Orchestrator discovers all agents
- [ ] OLLAMA semantic evaluation works for each agent
- [ ] Fallback to exact match works when OLLAMA unavailable
- [ ] No crashes or unhandled exceptions
- [ ] Web API starts without errors

### Should Pass (Important)

- [ ] OLLAMA response time <25s per evaluation (CPU)
- [ ] Evaluation quality >85% agreement with human graders
- [ ] Concurrent agent execution reduces total time
- [ ] Metadata filtering returns correct results
- [ ] Performance acceptable for pilot deployment

### Nice to Have (Optimization)

- [ ] OLLAMA response time <3s per evaluation (with GPU)
- [ ] Evaluation quality >90% agreement
- [ ] Comprehensive error logging
- [ ] Performance metrics dashboard

---

## Test Execution Schedule

### Phase 1: Basic Functionality (30 minutes)

- [ ] Test 1.1: Agent registration
- [ ] Test 1.2: Agent discovery
- [ ] Test 5.1: OLLAMA fallback

### Phase 2: Semantic Evaluation (2 hours)

- [ ] Test 2.1: Mathematics evaluation
- [ ] Test 2.2: Physics evaluation
- [ ] Test 2.3: Chemistry evaluation
- [ ] Test 2.4: Biology evaluation
- [ ] Test 2.5: English evaluation

### Phase 3: Performance & Workflow (1 hour)

- [ ] Test 3.1: Response times
- [ ] Test 4.1: Cross-subject assessment
- [ ] Test 4.2: Concurrent execution

### Phase 4: Advanced Features (1 hour)

- [ ] Test 6.1: Board-specific content
- [ ] Test 6.2: Module organization
- [ ] Test 3.2: Quality assessment

**Total Estimated Time**: 4-5 hours

---

## Tools & Scripts

### Quick Test Script

```bash
#!/bin/bash
# test-agents.sh

echo "=== EduMind.AI Agent Integration Tests ==="
echo

# 1. Check OLLAMA
echo "1. Checking OLLAMA service..."
if curl -s http://localhost:11434/api/tags > /dev/null; then
    echo "✅ OLLAMA is running"
else
    echo "❌ OLLAMA is not running - starting..."
    ollama serve &
    sleep 5
fi

# 2. Build solution
echo
echo "2. Building solution..."
cd /workspaces/edumind-ai
dotnet build EduMind.AI.sln --verbosity quiet
if [ $? -eq 0 ]; then
    echo "✅ Build successful"
else
    echo "❌ Build failed"
    exit 1
fi

# 3. Start Web API (background)
echo
echo "3. Starting Web API..."
dotnet run --project src/AcademicAssessment.Web &
API_PID=$!
sleep 10

# 4. Test agent discovery
echo
echo "4. Testing agent discovery..."
curl -s http://localhost:5001/api/v1/agents | jq '.agents[].name'

# 5. Test single evaluation (Math)
echo
echo "5. Testing Mathematics agent evaluation..."
curl -s -X POST http://localhost:5001/api/v1/agents/mathematics/evaluate \
  -H "Content-Type: application/json" \
  -d '{
    "questionText": "Solve for x: 2x + 5 = 15",
    "correctAnswer": "5",
    "studentAnswer": "x = 5",
    "points": 10
  }' | jq

# Cleanup
echo
echo "Stopping Web API..."
kill $API_PID

echo
echo "=== Tests Complete ==="
```

---

## Next Steps After Testing

1. **Document Results**: Create `INTEGRATION_TEST_RESULTS.md`
2. **Fix Issues**: Address any failures found
3. **Performance Tuning**: Optimize slow evaluations
4. **Production Planning**: Prepare for pilot deployment

---

**Status**: ⏳ **READY TO EXECUTE**  
**Estimated Duration**: 4-5 hours  
**Priority**: HIGH (blocks pilot deployment)

---

**Related Documents:**

- [Metadata Migration Complete](./METADATA_MIGRATION_COMPLETE.md)
- [OLLAMA Integration Complete](./OLLAMA_INTEGRATION_COMPLETE.md)
- [Phase 5 Complete](./A2A_AGENT_INTEGRATION_PLAN.md)
