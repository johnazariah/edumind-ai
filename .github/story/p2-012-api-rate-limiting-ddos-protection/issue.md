# Story 012: Implement API Rate Limiting & DDoS Protection

**Priority:** P2 - Enhancement  
**Status:** Ready for Implementation  
**Effort:** Medium (1 week)  
**Dependencies:** None


**GitHub Issue:** https://github.com/johnazariah/edumind-ai/issues/17

---

## Problem Statement

No rate limiting or DDoS protection on public APIs. Vulnerable to abuse, API cost overruns, service disruption.

**Risks:**

- Malicious actors can overwhelm API with requests
- Accidental infinite loops drain resources
- LLM API costs spiral out of control
- Legitimate users impacted by resource exhaustion

**Business Impact:** Service outages, unexpected Azure costs, poor user experience during attacks.

---

## Goals & Success Criteria

1. **Rate limiting middleware** (per-user, per-IP, per-tenant)
2. **Azure Front Door** for DDoS protection
3. **Cost controls** on LLM API usage
4. **HTTP 429 responses** with retry-after headers

**Success Criteria:**

- [ ] Rate limits enforced (1000 req/hour per user)
- [ ] LLM API capped at $500/month
- [ ] DDoS protection active (Azure Front Door)
- [ ] Graceful degradation under load
- [ ] API abuse attempts logged and blocked

---

## Technical Approach

### Rate Limiting Strategy

| Endpoint | Rate Limit | Scope |
|----------|------------|-------|
| `/api/auth/*` | 10/minute | Per IP |
| `/api/assessments/*` | 100/hour | Per User |
| `/api/questions/*` | 500/hour | Per Tenant |
| `/api/agents/generate` | 50/day | Per User |

### Implementation: ASP.NET Core Rate Limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromHours(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });
});

app.UseRateLimiter();

[EnableRateLimiting("api")]
public class AssessmentsController : ControllerBase { }
```

### Azure Front Door

- **Web Application Firewall (WAF)**: OWASP Top 10 protection
- **DDoS Protection**: L3/L4 volumetric attacks
- **Geo-filtering**: Block traffic from high-risk regions
- **Bot protection**: Block known bad bots

---

## Task Decomposition

### Task 1: Add ASP.NET Core Rate Limiting

- **Package:** `Microsoft.AspNetCore.RateLimiting` (.NET 9)
- **Files to Modify:** `Program.cs`, API controllers
- **Acceptance:** Rate limits enforced, 429 responses

### Task 2: Create LLM Cost Control Service

- **Files to Create:**
  - `src/AcademicAssessment.Infrastructure/Services/LlmCostControlService.cs`
- **Track:** LLM API calls, token usage, estimated cost
- **Stop:** When monthly limit reached ($500)
- **Acceptance:** LLM calls blocked when budget exceeded

### Task 3: Configure Azure Front Door

- **Location:** Azure Portal → Front Door
- **Setup:** WAF rules, DDoS protection, health probes
- **Acceptance:** Traffic routed through Front Door

### Task 4: Add Rate Limit Logging

- **Log:** User ID, IP, endpoint, timestamp when rate limit hit
- **Acceptance:** Rate limit violations logged for analysis

### Task 5: Update API Documentation

- **Files to Modify:** OpenAPI spec
- **Add:** Rate limit headers, 429 error responses
- **Acceptance:** Developers aware of rate limits

---

## Acceptance Criteria

- [ ] Rate limits enforced on all public APIs
- [ ] 429 responses with retry-after headers
- [ ] LLM API capped at $500/month
- [ ] Azure Front Door active (WAF + DDoS)
- [ ] Rate limit violations logged

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot
