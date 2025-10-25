# ADR 001: Retain Blazor Over React for Web and Mobile Development

**Date:** 2025-10-25  
**Status:** Accepted  
**Decision Makers:** Technical Lead, Product Owner  
**Context:** Production readiness planning and mobile app strategy

---

## Context

During production readiness planning, we evaluated whether to migrate the frontend from **Blazor Server** to **React** for improved web performance, larger ecosystem, and better developer availability. This decision became more critical when considering a **mobile app strategy** (Android/iOS) with Duolingo-style gamification.

### Current State

- **Web Frontend:** Blazor Server (.NET 9)
- **Backend:** ASP.NET Core Web API (.NET 9)
- **Mobile:** None (responsive web only)
- **Team:** C# expertise, no React experience

### Key Requirements

1. Build native mobile apps (Android priority, iOS later)
2. Duolingo-style gamification (XP, streaks, leaderboards)
3. Offline assessment capability
4. Fast time-to-market (6-9 months to mobile)
5. Minimize development and maintenance overhead
6. Type safety and error handling (Railway-Oriented Programming)

---

## Decision

**We will retain Blazor for web development and adopt .NET MAUI Blazor Hybrid for mobile apps.**

We will **NOT** migrate to React.

---

## Rationale

### Code Reuse Analysis

| Approach | Web Framework | Mobile Framework | Code Reuse | Timeline to Mobile |
|----------|--------------|------------------|------------|-------------------|
| **Blazor + MAUI** (chosen) | Blazor Server | .NET MAUI Blazor Hybrid | 80-90% | 3-4 months |
| React + React Native | React | React Native | 30-50% (logic only) | 15+ months |
| Blazor + React Native | Blazor Server | React Native | 0% (no reuse) | 6-9 months |

### Key Factors Favoring Blazor + MAUI

#### 1. **Exceptional Code Reuse (80-90%)**

With MAUI Blazor Hybrid, we can reuse:

- ✅ All Blazor components (with minor mobile adaptations)
- ✅ All domain models (`Student`, `Assessment`, `Question`, etc.)
- ✅ All service interfaces (`IAssessmentService`, `IOnboardingService`)
- ✅ Authentication logic
- ✅ API client code (`HttpClient` patterns)
- ✅ State management
- ✅ Validation logic
- ✅ Railway-Oriented Programming patterns (`Result<T>`)

**React + React Native provides ZERO UI code reuse.** Even logic reuse requires maintaining two language ecosystems (C# backend, TypeScript frontend).

#### 2. **Faster Time-to-Market**

- **Blazor → MAUI:** 12-16 weeks to mobile app
- **React migration + React Native:** 36-44 weeks (9-11 months)

Mobile app is critical for B2C market and competing with Duolingo. A 6-month delay is strategically unacceptable.

#### 3. **Lower Risk**

- **Blazor + MAUI:** No risky migration, proven technology
- **React migration:** High risk of regressions, bugs, and delays during rewrite

We're currently in production readiness phase (Stories 001-016). A React migration would block all progress for 6+ months.

#### 4. **Team Expertise**

- Current team has deep C# expertise
- No React/TypeScript experience
- Single language (C#) = faster development, fewer bugs
- Railway-Oriented Programming patterns already established in C#

#### 5. **Type Safety and Error Handling**

- C# compiler provides strong type safety across web, API, and mobile
- Railway-Oriented Programming (`Result<T>`) works consistently everywhere
- TypeScript requires discipline and doesn't prevent runtime errors at API boundaries

#### 6. **Ecosystem Parity for Mobile**

While React has a larger web ecosystem, **React Native and .NET MAUI have comparable mobile ecosystems**:

- Both have mature component libraries
- Both have good performance
- Both support offline storage (SQLite)
- Both integrate with native APIs
- Microsoft's commitment to MAUI is strong (.NET 9 brings significant improvements)

React's web ecosystem advantage disappears for mobile development.

#### 7. **Maintenance Overhead**

- **Blazor + MAUI:** Single codebase, single language, shared components
- **React + React Native:** Two codebases, two languages, duplicate logic

Maintenance cost doubles with React approach.

---

## Consequences

### Positive

1. **Fast mobile delivery:** Mobile app in 3-4 months after P1 stories
2. **Code efficiency:** 80-90% code reuse between web and mobile
3. **Lower risk:** No risky migration during production readiness
4. **Consistent patterns:** Railway-Oriented Programming everywhere
5. **Team productivity:** Single language, no context switching
6. **Type safety:** C# compiler catches errors at compile time
7. **Lower TCO:** Single codebase to maintain

### Negative

1. **Smaller ecosystem:** Fewer Blazor/MAUI components than React/React Native
2. **Developer availability:** Fewer .NET mobile developers than React Native (mitigated by remote work)
3. **Blazor Server latency:** SignalR adds 100-200ms latency (can migrate to Blazor WASM if needed)
4. **MAUI maturity:** MAUI is newer than React Native (but .NET 9 addresses early issues)

### Mitigations

- **Ecosystem size:** Build custom components as needed; .NET MAUI ecosystem is growing rapidly
- **Developer hiring:** Remote work gives access to global .NET talent pool
- **Latency concerns:** Monitor performance; can migrate to Blazor WebAssembly if Blazor Server proves problematic
- **MAUI maturity:** Use .NET 9 (stable release); wait for community feedback before starting mobile development

---

## Alternatives Considered

### Alternative 1: Migrate to React + React Native

**Rejected because:**

- 36-44 weeks total effort (vs 12-16 weeks for MAUI)
- Zero UI code reuse between web and mobile
- High migration risk during production readiness
- Team learning curve for React/TypeScript
- Doubles maintenance overhead (C# backend + TypeScript frontend)

### Alternative 2: Keep Blazor Web + Build React Native Mobile

**Rejected because:**

- Zero code reuse between web and mobile
- Maintain two completely different frontends (Blazor + React Native)
- Different state management, API clients, validation logic
- Hard to maintain feature parity
- Team would need React Native expertise anyway

### Alternative 3: Progressive Web App (PWA)

**Considered for Phase 1, but not a replacement for native mobile:**

- PWA provides quick mobile access (add to home screen)
- Limited offline support
- No app store presence
- Poor iOS support for PWA features
- Not competitive with Duolingo's native app experience

**Decision:** Evaluate PWA as interim solution (2-3 weeks effort) while building MAUI app.

---

## Implementation Plan

### Phase 1: Production Readiness (Current - Month 3)

- Complete Stories 001-009 (P0 + P1 critical features)
- No frontend changes

### Phase 2: Mobile Investigation (Month 4)

- **Story 017:** Mobile app investigation (MAUI proof-of-concept)
- Deliverable: Technical feasibility report + POC demo
- Go/no-go decision on full MAUI development

### Phase 3: Mobile Development (Month 5-7)

- Build MAUI Blazor Hybrid app
- Core assessment features
- Gamification (XP, streaks, badges)
- Google Play Store release

### Phase 4: Mobile Enhancement (Month 8-9)

- Offline mode
- Push notifications
- Advanced gamification
- iOS version

---

## Metrics for Success

We will consider this decision successful if:

1. **Code reuse:** >75% of web components reused in mobile app
2. **Time-to-market:** Mobile app released within 4 months of starting development
3. **Performance:** Mobile app performs as well as React Native alternatives
4. **Maintenance:** Single team can maintain both web and mobile
5. **User satisfaction:** Mobile app achieves >4.0 rating on Google Play

---

## Review Schedule

- **6 months:** Review mobile app progress; assess if MAUI meets expectations
- **12 months:** Evaluate ecosystem maturity; reassess if React Native offers compelling advantages
- **Annually:** Review decision as .NET MAUI ecosystem evolves

---

## Related Documents

- [Story 017: Mobile App Investigation (MAUI + Gamification)](.github/story/017/issue.md)
- [Architecture Summary](docs/architecture/ARCHITECTURE_SUMMARY.md)
- [System Specification](.github/specification/README.md)

---

## References

- [.NET MAUI Documentation](https://learn.microsoft.com/en-us/dotnet/maui/)
- [Blazor Hybrid Apps](https://learn.microsoft.com/en-us/aspnet/core/blazor/hybrid/)
- [MAUI vs React Native Comparison](https://learn.microsoft.com/en-us/dotnet/maui/what-is-maui#net-maui-and-react-native)
- [.NET 9 MAUI Improvements](https://devblogs.microsoft.com/dotnet/announcing-dotnet-9/)

---

**Decision Status:** ✅ Accepted  
**Date:** 2025-10-25  
**Next Review:** 2026-04-25 (6 months)
