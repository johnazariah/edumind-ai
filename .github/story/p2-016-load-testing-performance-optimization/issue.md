# Story 016: Load Testing & Performance Optimization

**Priority:** P2 - Enhancement  
**Status:** Ready for Implementation  
**Effort:** Medium (1 week)  
**Dependencies:** None


**GitHub Issue:** https://github.com/johnazariah/edumind-ai/issues/21

---

## Problem Statement

No load testing performed. Unknown capacity limits, bottlenecks, or performance under stress. Cannot confidently scale for production traffic.

**Unknowns:**

- How many concurrent users can system handle?
- What's the bottleneck (DB, API, LLM)?
- When does performance degrade?
- What's the breaking point?

**Business Impact:** Cannot plan capacity, risk of outages under load, poor user experience during peak traffic.

---

## Goals & Success Criteria

1. **Load test critical endpoints** with k6
2. **Establish performance baselines** (throughput, latency, error rate)
3. **Identify bottlenecks** (database, API, LLM, network)
4. **Optimize critical paths** (N+1 queries, caching, indexing)
5. **Document capacity limits** (max users, max requests/sec)

**Success Criteria:**

- [ ] Load tests for 5 critical endpoints
- [ ] System handles 1000 concurrent users
- [ ] P95 latency <2s for all APIs
- [ ] Zero errors at 80% capacity
- [ ] Performance baselines documented

---

## Technical Approach

### Load Testing Tool: k6

**Why k6?**

- JavaScript-based test scripts
- Cloud-native (runs in CI/CD)
- Excellent reporting (Grafana integration)
- Realistic traffic simulation

### Endpoints to Test

1. **GET /api/assessments** - List assessments (read-heavy)
2. **POST /api/assessments/{id}/start** - Start assessment (session creation)
3. **POST /api/responses** - Submit answer (write-heavy)
4. **GET /api/analytics/student/{id}** - View analytics (complex queries)
5. **POST /api/agents/generate** - Generate feedback (LLM call, slowest)

### Load Test Scenarios

**Scenario 1: Normal Load**

- 100 concurrent users
- 10 minute duration
- Target: <1s P95 latency, 0% errors

**Scenario 2: Peak Load**

- 500 concurrent users
- 5 minute duration
- Target: <2s P95 latency, <1% errors

**Scenario 3: Stress Test**

- Ramp up to 2000 users over 10 minutes
- Find breaking point
- Target: Identify bottleneck before failure

**Scenario 4: Soak Test**

- 200 concurrent users
- 4 hour duration
- Target: Detect memory leaks, connection pool exhaustion

---

## Task Decomposition

### Task 1: Set Up k6 Load Testing

- **Install:** `brew install k6` or download binary
- **Files to Create:**
  - `tests/load/k6-config.js`
  - `tests/load/scenarios/*.js` (one per scenario)
- **Acceptance:** k6 runs simple test

### Task 2: Write Load Test Scripts for 5 Endpoints

- **Files to Create:**
  - `tests/load/scenarios/list-assessments.js`
  - `tests/load/scenarios/start-assessment.js`
  - `tests/load/scenarios/submit-response.js`
  - `tests/load/scenarios/view-analytics.js`
  - `tests/load/scenarios/generate-feedback.js`
- **Example:**

  ```javascript
  import http from 'k6/http';
  import { check, sleep } from 'k6';
  
  export let options = {
    stages: [
      { duration: '2m', target: 100 },  // Ramp up
      { duration: '5m', target: 100 },  // Steady state
      { duration: '2m', target: 0 },    // Ramp down
    ],
    thresholds: {
      http_req_duration: ['p(95)<2000'], // 95% < 2s
      http_req_failed: ['rate<0.01'],    // Error rate < 1%
    },
  };
  
  export default function () {
    let res = http.get('https://api.edumind.ai/api/assessments');
    check(res, {
      'status is 200': (r) => r.status === 200,
      'response time < 2s': (r) => r.timings.duration < 2000,
    });
    sleep(1);
  }
  ```

- **Acceptance:** All 5 scenarios executable

### Task 3: Run Load Tests and Collect Baselines

- **Execute:** All 4 scenarios (normal, peak, stress, soak)
- **Collect Metrics:**
  - Requests per second (RPS)
  - P50, P95, P99 latency
  - Error rate
  - CPU/memory usage
- **Document:** Results in `docs/performance/LOAD_TEST_RESULTS.md`
- **Acceptance:** Baselines established

### Task 4: Identify Performance Bottlenecks

- **Analysis:** Where does system slow down first?
  - Database queries (N+1 problem, missing indexes)
  - API serialization (large payloads)
  - LLM calls (timeout, rate limiting)
  - Network latency
- **Tools:** Application Insights, database query logs
- **Acceptance:** Top 3 bottlenecks identified

### Task 5: Optimize Database Queries

- **Find:** Slow queries (>500ms)
- **Fix:**
  - Add missing indexes
  - Eliminate N+1 queries (use `.Include()`)
  - Cache frequent queries (Redis)
  - Paginate large result sets
- **Example:**

  ```sql
  -- Before (slow)
  SELECT * FROM assessments WHERE tenant_id = '...' ORDER BY created_at DESC;
  
  -- After (fast)
  CREATE INDEX idx_assessments_tenant_created 
  ON assessments(tenant_id, created_at DESC);
  ```

- **Acceptance:** Database query P95 <100ms

### Task 6: Add Response Caching

- **Cache:**
  - Assessment list (5 min TTL)
  - Question bank (10 min TTL)
  - Analytics reports (1 hour TTL)
- **Invalidate:** On create/update/delete
- **Tool:** Redis
- **Acceptance:** Cache hit rate >70%

### Task 7: Optimize LLM API Calls

- **Strategies:**
  - Batch multiple requests
  - Use cheaper model for simple tasks (llama3.2:1b)
  - Cache common feedback (e.g., "Great job!")
  - Timeout after 10 seconds
- **Acceptance:** LLM P95 latency <5s

### Task 8: Re-Run Load Tests and Compare

- **Execute:** Same scenarios after optimizations
- **Compare:** Before vs after metrics
- **Target Improvements:**
  - 50% reduction in P95 latency
  - 2x increase in throughput (RPS)
  - 10x reduction in error rate
- **Acceptance:** Significant performance gains

### Task 9: Document Capacity Limits

- **Files to Create:**
  - `docs/performance/CAPACITY_PLANNING.md`
- **Document:**
  - Max concurrent users: X
  - Max requests/second: Y
  - Max LLM calls/hour: Z
  - Bottleneck: [Database/API/LLM]
  - Scaling strategy: [Vertical/Horizontal]
- **Acceptance:** Capacity limits known

### Task 10: Integrate Load Tests into CI/CD

- **Files to Create:**
  - `.github/workflows/load-tests.yml`
- **Schedule:** Weekly (not on every PR - too slow)
- **Alert:** If performance regresses >20%
- **Acceptance:** Load tests run automatically

---

## Acceptance Criteria

- [ ] Load tests for 5 critical endpoints
- [ ] Performance baselines documented
- [ ] System handles 1000 concurrent users
- [ ] P95 latency <2s for all APIs
- [ ] Zero errors at 80% capacity
- [ ] Bottlenecks identified and optimized
- [ ] Load tests integrated into CI/CD

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot
