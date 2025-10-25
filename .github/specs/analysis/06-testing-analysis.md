# Analysis Story: Testing Guide

## Objective

Create a comprehensive `testing.md` that clearly describes how the system should be tested locally and in the cloud, including unit tests, integration tests, E2E tests, and manual testing procedures.

## Context

The EduMind.AI project has various testing approaches and tools scattered across:

- Unit test projects
- Integration test setup
- UI test framework (recently created)
- Manual testing procedures in deployment docs
- Test scripts

## Task Instructions

### 1. Review Test Project Structure

Analyze all test projects:

```bash
find tests/ -type f -name "*.csproj" -exec echo {} \; -exec cat {} \;
find tests/ -type f -name "*.cs" | head -20
```

Projects to analyze:

- `tests/AcademicAssessment.Tests.Unit/`
- `tests/AcademicAssessment.Tests.Integration/`
- `tests/AcademicAssessment.Tests.Performance/`
- `tests/AcademicAssessment.Tests.UI/`

### 2. Extract Testing Patterns from Code

Review test implementations:

```bash
# Find example tests
cat tests/AcademicAssessment.Tests.Unit/**/*.cs | head -100
cat tests/AcademicAssessment.Tests.UI/BasicFunctionalityTests.cs
```

Identify:

- Test naming conventions
- Assertion libraries (FluentAssertions, xUnit asserts)
- Mocking patterns
- Test data setup
- Test organization (AAA pattern, etc.)

### 3. Review Test Configuration

Analyze:

- `coverlet.runsettings` - Coverage configuration
- Test project references
- Test dependencies (Moq, FluentAssertions, etc.)
- Test database setup

### 4. Extract Test Execution Methods

Review:

```bash
# Check for test scripts
ls -la tests/*.sh
cat tests/*.sh

# Check CI/CD test execution
grep -A 20 "test" .github/workflows/*.yml
```

### 5. Analyze Integration Test Setup

Review:

- `tests/AcademicAssessment.Tests.Integration/`
- Docker compose for test infrastructure
- Test database initialization
- External service mocking/stubbing

### 6. Review UI Testing Approach

Analyze recent UI test work:

- `tests/AcademicAssessment.Tests.UI/BasicFunctionalityTests.cs`
- Playwright configuration
- WebApplicationFactory usage
- Test factories and fixtures

### 7. Extract Performance Test Strategy

Review:

- `tests/AcademicAssessment.Tests.Performance/`
- Load testing approach
- Performance benchmarks
- Monitoring during tests

### 8. Review Manual Testing Procedures

Check deployment docs for manual test steps:

- `docs/deployment/LOCAL_DEPLOYMENT_TEST_RESULTS.md`
- `docs/deployment/LOCAL_VALIDATION_SUCCESS.md`
- Smoke test procedures
- UAT procedures

### 9. Analyze Test Data Management

Review:

- Seed data scripts
- Test data generators
- Database fixtures
- Data cleanup strategies

### 10. Extract Coverage Requirements

Check:

- `coverage-output.txt` - Current coverage metrics
- Coverage thresholds in configuration
- Areas with low coverage
- Coverage reporting

### 11. Review Ollama/Agent Testing

Analyze:

```bash
cat tests/test-ollama-integration.sh
cat tests/test-multi-agent-ollama.sh
cat tests/test-phase5-agents.sh
```

Understand:

- How AI agents are tested
- Ollama integration testing
- Prompt testing strategies

## Expected Output Structure

Create `.github/testing.md` with these sections:

### 1. Overview

- Testing philosophy
- Test pyramid strategy
- Coverage goals
- Testing tools and frameworks

### 2. Local Development Testing

#### 2.1 Prerequisites

- .NET SDK for testing
- Docker for integration tests
- Ollama for AI tests
- Playwright browsers
- Test databases

#### 2.2 Running Tests Locally

```bash
# Commands to run all tests
# Commands to run specific test categories
# Commands with coverage
```

#### 2.3 IDE Integration

- Visual Studio test runner
- VS Code test explorer
- ReSharper test runner
- Debugging tests

### 3. Unit Testing

#### 3.1 Unit Test Strategy

- What should be unit tested
- Isolation approach
- Mocking strategy
- Test data patterns

#### 3.2 Unit Test Structure

```csharp
// Example of well-structured unit test
// Following project conventions
```

#### 3.3 Naming Conventions

- Test class naming
- Test method naming
- Test data naming

#### 3.4 Assertion Patterns

- FluentAssertions usage
- Custom assertions
- Error message patterns

#### 3.5 Mocking Guidelines

- When to mock
- Moq patterns used
- Test doubles strategy

#### 3.6 Running Unit Tests

```bash
dotnet test tests/AcademicAssessment.Tests.Unit/
# With coverage
# With specific filters
```

### 4. Integration Testing

#### 4.1 Integration Test Strategy

- What needs integration testing
- Test scope boundaries
- External dependencies

#### 4.2 Test Infrastructure Setup

- Docker containers for tests
- Test database initialization
- Redis cache for tests
- Ollama for AI tests

#### 4.3 WebApplicationFactory Pattern

- How it's used in the project
- Test server configuration
- Service overrides for testing

#### 4.4 Database Testing

- Test database creation
- Migration execution
- Data seeding
- Cleanup between tests

#### 4.5 API Integration Tests

```csharp
// Example API integration test
// Following project patterns
```

#### 4.6 Running Integration Tests

```bash
dotnet test tests/AcademicAssessment.Tests.Integration/
# Prerequisites
# Setup steps
# Teardown
```

### 5. End-to-End Testing

#### 5.1 E2E Test Strategy

- Critical user workflows
- Browser testing approach
- Test data management

#### 5.2 Playwright Setup

- Browser installation
- Configuration
- Page object patterns
- Element selectors

#### 5.3 UI Test Scenarios

Document key scenarios:

- Student assessment workflow
- Admin dashboard workflows
- Authentication flows

#### 5.4 UI Test Implementation

```csharp
// Example E2E test with Playwright
// Following project patterns
```

#### 5.5 Running E2E Tests

```bash
# Install Playwright
# Run UI tests
# Headed vs headless mode
# Test reporting
```

#### 5.6 Visual Regression Testing

- If implemented
- Screenshot comparison
- Baseline management

### 6. Performance Testing

#### 6.1 Performance Test Strategy

- Load testing approach
- Stress testing scenarios
- Performance benchmarks

#### 6.2 Performance Test Setup

- Load generation tools
- Monitoring during tests
- Results analysis

#### 6.3 Key Performance Metrics

- Response time targets
- Throughput requirements
- Resource utilization limits

#### 6.4 Running Performance Tests

```bash
# Load test execution
# Interpreting results
```

### 7. AI/Agent Testing

#### 7.1 Agent Test Strategy

- Prompt testing
- Response validation
- Multi-agent orchestration testing

#### 7.2 Ollama Integration Testing

```bash
# From test-ollama-integration.sh
# How to test AI components
```

#### 7.3 Question Generation Testing

- Quality validation
- Difficulty calibration
- Content accuracy

#### 7.4 Feedback Generation Testing

- Relevance checks
- Tone validation
- Accuracy verification

### 8. Test Data Management

#### 8.1 Test Data Strategy

- Static test data
- Generated test data
- Production-like data

#### 8.2 Seed Data Scripts

- Location and usage of seed scripts
- Data relationships
- Data volume

#### 8.3 Test Data Builders

- Builder patterns used
- Fixtures and factories
- Data cleanup

### 9. Test Coverage

#### 9.1 Coverage Goals

- Overall coverage target
- Per-component targets
- Critical path coverage

#### 9.2 Running Coverage Reports

```bash
dotnet test --collect:"XPlat Code Coverage"
# Viewing coverage reports
# Understanding metrics
```

#### 9.3 Coverage Analysis

- Interpreting coverage reports
- Identifying gaps
- Improving coverage

#### 9.4 Current Coverage Status

- Document current state from `coverage-output.txt`
- Areas with good coverage
- Areas needing improvement

### 10. CI/CD Testing

#### 10.1 Automated Test Execution

- Tests run in CI/CD
- Test parallelization
- Test result publishing

#### 10.2 Test Reports

- Where reports are published
- How to access
- Trend analysis

#### 10.3 Failed Test Handling

- Re-running failed tests
- Flaky test management
- Failure investigation

### 11. Manual Testing

#### 11.1 Smoke Testing

Checklist for smoke tests:

- [ ] Web API health endpoint
- [ ] Student App loads
- [ ] Can create/take assessment
- [ ] Results are displayed
- [ ] Dashboard accessible

#### 11.2 UAT Procedures

- User acceptance test scenarios
- Test accounts and data
- Sign-off procedures

#### 11.3 Exploratory Testing

- When to do exploratory testing
- Areas that benefit most
- Documenting findings

#### 11.4 Manual Test Cases

Key manual test scenarios:

- Full assessment workflow
- Edge cases
- Error handling
- Cross-browser testing

### 12. Cloud Environment Testing

#### 12.1 Azure Test Environment

- Test environment setup
- Differences from production
- Test data in cloud

#### 12.2 Cloud Integration Testing

- Testing Azure-specific integrations
- Managed service testing
- Network testing

#### 12.3 Production Smoke Tests

- Safe tests for production
- Monitoring during tests
- Rollback triggers

### 13. Test Debugging

#### 13.1 Debugging Failed Tests

- Common failure patterns
- Diagnostic approaches
- Log analysis

#### 13.2 Test Isolation Issues

- Identifying shared state
- Fixing test interdependencies
- Parallel execution issues

#### 13.3 Flaky Tests

- Identifying flaky tests
- Root cause analysis
- Stabilization strategies

### 14. Test Maintenance

#### 14.1 Keeping Tests Updated

- When to update tests
- Refactoring test code
- Removing obsolete tests

#### 14.2 Test Code Quality

- Code review for tests
- Test readability
- Test maintainability

#### 14.3 Test Infrastructure Maintenance

- Updating test dependencies
- Container image updates
- Test database schema

### 15. Testing Best Practices

#### 15.1 Project-Specific Practices

Best practices observed in codebase:

- Testing patterns established
- Anti-patterns to avoid
- Lessons learned

#### 15.2 Test Writing Guidelines

- One assertion per test (or not?)
- Test independence
- Readable test names
- Clear failure messages

#### 15.3 Test Performance

- Test execution speed
- Parallelization strategies
- Test suite optimization

### 16. Testing Tools Reference

#### 16.1 Testing Frameworks

- xUnit configuration and usage
- FluentAssertions patterns
- Moq usage

#### 16.2 Integration Testing Tools

- WebApplicationFactory
- TestContainers (if used)
- Docker compose for tests

#### 16.3 UI Testing Tools

- Playwright API reference
- Browser automation patterns
- Screenshot and video capture

#### 16.4 Coverage Tools

- coverlet configuration
- Report generators
- CI/CD integration

### 17. Known Issues and Limitations

#### 17.1 Current Testing Gaps

Areas lacking test coverage:

- Specific components
- Edge cases
- Performance scenarios

#### 17.2 Testing Challenges

- Difficult-to-test areas
- Workarounds in place
- Planned improvements

#### 17.3 Test Infrastructure Issues

Known issues:

- Blazor SignalR testing challenges (from recent work)
- Container startup timing
- Test data management

## Success Criteria

- Complete testing procedures for all test types
- Clear instructions for running each test category
- Well-documented testing patterns and conventions
- Examples from actual project tests
- Troubleshooting guidance for common issues
- Integration with CI/CD is explained
- Both local and cloud testing covered
- Practical for developers at all levels
- 60-80 pages equivalent of comprehensive testing guide

## Notes

- Include actual test code examples from the project
- Document current state honestly (including gaps)
- Provide both quick reference and detailed explanations
- Link to tool documentation where appropriate
- Include timing expectations (how long tests take)
- Note any environment-specific considerations
- Document test data requirements clearly
- Include screenshots or command outputs where helpful
- Focus on practical, actionable guidance
- Note recent fixes (like UI test framework setup)
