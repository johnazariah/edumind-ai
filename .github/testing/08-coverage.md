# Code Coverage Guide

**Purpose:** Understand, measure, and improve test coverage in EduMind.AI.

**Audience:** All developers, especially when working on new features or refactoring.

---

## 🎯 Coverage Goals

### Target Coverage by Component

| Component | Target | Current | Priority |
|-----------|--------|---------|----------|
| **Core Models** | 90%+ | TBD | Critical |
| **Repositories** | 80%+ | TBD | High |
| **API Controllers** | 80%+ | TBD | High |
| **Services/Agents** | 75%+ | TBD | High |
| **Blazor Components** | 60%+ | TBD | Medium |
| **Infrastructure** | 50%+ | TBD | Low |
| **Overall Project** | **70%+** | TBD | **Required** |

### Coverage Philosophy

✅ **Coverage is a guide, not a goal**  
✅ **Quality over quantity** - 70% of critical code > 90% of trivial code  
✅ **Focus on business logic** - Don't test framework internals  
✅ **100% coverage ≠ bug-free code**

---

## 🛠️ Coverage Tools

### coverlet (Primary Tool)

We use **coverlet** for .NET code coverage collection:

```xml
<!-- Already installed in all test projects -->
<PackageReference Include="coverlet.collector" Version="6.0.0">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

### Configuration: coverlet.runsettings

```xml
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat code coverage">
        <Configuration>
          <!-- Output formats -->
          <Format>cobertura,opencover</Format>
          
          <!-- Exclude test assemblies -->
          <Exclude>[*.Tests.*]*</Exclude>
          <Exclude>[*]*.Program</Exclude>
          <Exclude>[*]*.Startup</Exclude>
          
          <!-- Exclude by attribute -->
          <ExcludeByAttribute>Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute</ExcludeByAttribute>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

---

## 📊 Collecting Coverage

### Basic Coverage Collection

```bash
# Collect coverage for all tests
dotnet test --collect:"XPlat Code Coverage"

# With specific runsettings
dotnet test --collect:"XPlat Code Coverage" --settings tests/coverlet.runsettings

# Coverage files generated in:
# tests/*/TestResults/{guid}/coverage.cobertura.xml
```

### Coverage by Test Project

```bash
# Unit tests only
dotnet test tests/AcademicAssessment.Tests.Unit/ \
  --collect:"XPlat Code Coverage"

# Integration tests
dotnet test tests/AcademicAssessment.Tests.Integration/ \
  --collect:"XPlat Code Coverage"

# Combine later with report generator
```

### Output Formats

**Cobertura (XML):**

- ✅ Human-readable XML
- ✅ CI/CD integration
- ✅ Report generation

**OpenCover (XML):**

- ✅ Detailed line/branch coverage
- ✅ IDE integration (VS, Rider)

---

## 📈 Generating Coverage Reports

### Install ReportGenerator

```bash
# Install globally
dotnet tool install -g dotnet-reportgenerator-globaltool

# Or locally
dotnet tool install dotnet-reportgenerator-globaltool
```

### Generate HTML Report

```bash
# 1. Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# 2. Generate report
reportgenerator \
  -reports:"tests/**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html

# 3. Open in browser
# Linux/Mac
xdg-open coverage-report/index.html
# Windows
start coverage-report/index.html
```

### Report Types

```bash
# HTML (Interactive)
-reporttypes:Html

# HTML Summary (Single page)
-reporttypes:HtmlSummary

# Badge (For README)
-reporttypes:Badges

# Multiple formats
-reporttypes:Html;Badges;Cobertura
```

### Example Report Output

```
├── coverage-report/
│   ├── index.html                   # Main report
│   ├── summary.html                 # Overview
│   ├── AcademicAssessment.Core/
│   │   ├── Models/
│   │   │   └── Assessment.cs.html   # Line-by-line coverage
│   │   └── index.html
│   └── badge_linecoverage.svg       # Coverage badge
```

---

## 🔍 Analyzing Coverage

### Reading Coverage Reports

**Summary View:**

```
Assembly           | Line    | Branch  | Method
-------------------|---------|---------|--------
Core               | 85.3%   | 78.2%   | 90.1%
Infrastructure     | 72.1%   | 65.5%   | 75.3%
Agents             | 68.9%   | 60.2%   | 71.2%
Web (API)          | 80.5%   | 73.8%   | 82.7%
Overall            | 76.7%   | 69.4%   | 79.8%
```

**Detailed File View:**

```csharp
// Green lines = covered
1: public class Assessment
2: {
3:     public Guid Id { get; init; }
4:     public string Title { get; init; }
5:     
6:     public int QuestionCount => QuestionIds.Count;  // ✅ Covered
7:     
8:     public bool IsAdaptive =>                       // ⚠️ Partially covered
9:         AssessmentType == AssessmentType.Adaptive;  // ✅ Covered (true branch)
10:                                                      // ❌ Not covered (false branch)
11: }
```

### Key Metrics

**Line Coverage:** % of lines executed

```
Covered Lines / Total Lines = 850 / 1000 = 85%
```

**Branch Coverage:** % of decision branches taken

```csharp
if (score > 70)           // ✅ True branch covered
{                         // ❌ False branch NOT covered
    return "Pass";
}
else
{
    return "Fail";        // ← This line never executed
}
```

**Method Coverage:** % of methods called

```
Covered Methods / Total Methods = 180 / 200 = 90%
```

---

## 🎯 Improving Coverage

### 1. Identify Uncovered Code

```bash
# Generate report
reportgenerator -reports:"coverage.cobertura.xml" -targetdir:"report"

# Open report and look for red/orange areas
# Prioritize:
# 1. Critical business logic
# 2. Error handling paths
# 3. Complex conditions
```

### 2. Add Missing Tests

**Example: Uncovered Branch**

```csharp
// Current code (50% branch coverage)
public string EvaluateScore(double score)
{
    if (score >= 90)           // ✅ Tested
        return "Excellent";
    else if (score >= 70)      // ❌ NOT tested
        return "Good";
    else                       // ❌ NOT tested
        return "Needs Work";
}

// Add tests for missing branches
[Theory]
[InlineData(95, "Excellent")]   // ✅ Already had
[InlineData(75, "Good")]        // ⭐ Add this
[InlineData(50, "Needs Work")]  // ⭐ Add this
public void EvaluateScore_VariousScores_ReturnsAppropriate(
    double score, string expected)
{
    var result = calculator.EvaluateScore(score);
    result.Should().Be(expected);
}
// Now 100% branch coverage
```

### 3. Test Error Paths

```csharp
// Often missed: exception handling
[Fact]
public async Task SaveAsync_NullAssessment_ThrowsArgumentNull()
{
    // Arrange
    var repository = CreateRepository();
    
    // Act
    Func<Task> act = async () => await repository.SaveAsync(null!);
    
    // Assert
    await act.Should().ThrowAsync<ArgumentNullException>();
}
```

### 4. Test Edge Cases

```csharp
[Theory]
[InlineData(0)]        // Minimum
[InlineData(1)]        // Boundary
[InlineData(100)]      // Maximum
[InlineData(-1)]       // Invalid (should throw)
public void TestEdgeCases(int input)
{
    // Test boundary conditions
}
```

---

## ⚠️ Coverage Anti-Patterns

### 1. Testing for Coverage, Not Quality

❌ **Bad - Meaningless tests:**

```csharp
[Fact]
public void Constructor_Creates()
{
    var obj = new Assessment();  // No assertions!
    // 100% line coverage, 0% value
}
```

✅ **Good - Tests behavior:**

```csharp
[Fact]
public void Constructor_SetsRequiredProperties()
{
    var obj = new Assessment { Id = guid, Title = "Test" };
    obj.Id.Should().Be(guid);
    obj.Title.Should().Be("Test");
}
```

### 2. Testing Private Implementation

❌ **Bad - Testing internals:**

```csharp
// Making private method public just for testing
public void InternalHelperMethod() { /* ... */ }
```

✅ **Good - Test through public API:**

```csharp
// Test the public method that calls private helper
[Fact]
public void PublicMethod_UsesHelperCorrectly() { /* ... */ }
```

### 3. Over-Testing Framework Code

❌ **Bad - Testing EF Core:**

```csharp
[Fact]
public void DbContext_CanQueryDatabase()
{
    var context = CreateContext();
    var result = context.Assessments.ToList();
    // Just testing EF Core works, not our code
}
```

✅ **Good - Test our repository logic:**

```csharp
[Fact]
public async Task GetByCourseId_FiltersCorrectly()
{
    // Tests our query logic, not EF Core
}
```

---

## 🔧 Coverage in CI/CD

### GitHub Actions

Coverage is collected in CI/CD automatically:

```yaml
# .github/workflows/_reusable-dotnet-build.yml
- name: Run tests with coverage
  run: |
    dotnet test \
      --collect:"XPlat Code Coverage" \
      --settings tests/coverlet.runsettings \
      --logger "trx"

- name: Generate coverage report
  run: |
    reportgenerator \
      -reports:"tests/**/coverage.cobertura.xml" \
      -targetdir:"coverage" \
      -reporttypes:"HtmlInline_AzurePipelines;Cobertura"

- name: Upload coverage
  uses: codecov/codecov-action@v3
  with:
    files: ./coverage/Cobertura.xml
```

### Coverage Badges

Generate badges for README:

```bash
reportgenerator \
  -reports:"coverage.cobertura.xml" \
  -targetdir:"." \
  -reporttypes:Badges

# Creates:
# - badge_linecoverage.svg
# - badge_branchcoverage.svg
# - badge_methodcoverage.svg
```

**In README.md:**

```markdown
![Code Coverage](./badge_linecoverage.svg)
```

---

## 📱 IDE Integration

### Visual Studio Code (Coverage Gutters)

1. Install "Coverage Gutters" extension
2. Run tests with coverage
3. Command Palette → "Coverage Gutters: Display Coverage"
4. Green/red gutters show covered/uncovered lines

```json
// .vscode/settings.json
{
  "coverage-gutters.coverageFileNames": [
    "**/coverage.cobertura.xml"
  ],
  "coverage-gutters.showLineCoverage": true,
  "coverage-gutters.showRulerCoverage": true
}
```

### Visual Studio 2022

1. Test → Analyze Code Coverage for All Tests
2. Coverage Results window shows percentages
3. Double-click to see line-by-line

### JetBrains Rider

1. Right-click test → "Cover Unit Tests"
2. Coverage window shows results
3. File editor highlights covered/uncovered code

---

## 📊 Coverage Reporting

### Generate Text Summary

```bash
dotnet test --collect:"XPlat Code Coverage"

reportgenerator \
  -reports:"tests/**/coverage.cobertura.xml" \
  -targetdir:"report" \
  -reporttypes:TextSummary

cat report/Summary.txt
```

**Output:**

```
Summary
=======
Generated on: 2025-10-25 12:00:00
Parser: MultiReportParser (3x Cobertura)

Assemblies: 8
Classes: 145
Files: 145
Line coverage: 76.7% (1850 of 2412)
Branch coverage: 69.4% (420 of 605)
Method coverage: 79.8% (180 of 225)

Coverage Quota: Core: 85.3%, Infrastructure: 72.1%, Web: 80.5%
```

### Coverage Trends

Track coverage over time:

```bash
# Generate history report
reportgenerator \
  -reports:"coverage.cobertura.xml" \
  -targetdir:"report" \
  -reporttypes:HtmlChart \
  -historydir:"history"

# View trends in report/index.html
```

---

## 🎯 Coverage Review Checklist

Before merging PR:

- [ ] Overall coverage ≥ 70%
- [ ] Core models coverage ≥ 90%
- [ ] New code is covered
- [ ] Critical paths have tests
- [ ] Error paths are tested
- [ ] No tests added just for coverage metrics

---

## 🐛 Troubleshooting

### Coverage Not Generating

**Problem:** No `coverage.cobertura.xml` files

```bash
# 1. Verify coverlet.collector is installed
grep coverlet tests/**/*.csproj

# 2. Run with explicit collector
dotnet test --collect:"XPlat Code Coverage"

# 3. Check TestResults directories
find tests -name "coverage.cobertura.xml"
```

### Wrong Coverage Numbers

**Problem:** Coverage includes test code

```xml
<!-- Add to coverlet.runsettings -->
<Exclude>[*.Tests.*]*</Exclude>
<Exclude>[AcademicAssessment.Tests.*]*</Exclude>
```

### Report Generator Fails

**Problem:** `reportgenerator` command not found

```bash
# Install globally
dotnet tool install -g dotnet-reportgenerator-globaltool

# Or use with dotnet tool
dotnet tool run reportgenerator -- <args>
```

---

## 📚 Best Practices

1. **Run coverage locally before PR** - Ensure you haven't decreased coverage
2. **Review uncovered code** - Understand what's not tested and why
3. **Focus on critical code** - 100% coverage not necessary everywhere
4. **Don't game metrics** - Tests should provide value, not just coverage
5. **Track trends** - Is coverage improving or declining?

---

**Last Updated:** 2025-10-25  
**Related:** [Unit Testing](./03-unit-testing.md) | [CI/CD Testing](./09-cicd-testing.md) | [Local Testing](./02-local-testing.md)
