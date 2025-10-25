# End-to-End (E2E) Testing Guide

**Purpose:** Test complete user workflows through the browser using Playwright.

**Audience:** Developers testing UI functionality and critical user journeys.

---

## 🎯 What are E2E Tests?

End-to-End tests verify **complete user workflows** from the browser:

✅ User authentication and navigation  
✅ Multi-page workflows (start assessment → answer questions → submit → view results)  
✅ Form interactions and validation  
✅ Real-time updates (SignalR/Blazor interactivity)  
✅ Cross-browser compatibility  
✅ Visual regression (optional)

**When to Write E2E Tests:**

- ✅ Critical user journeys (student takes assessment)
- ✅ Complex multi-step workflows
- ✅ Cross-browser compatibility needed
- ✅ Visual behavior important

**When NOT to Write E2E Tests:**

- ❌ Testing business logic (use unit tests)
- ❌ Testing API contracts (use integration tests)
- ❌ Edge cases and error handling (slower feedback)

---

## 🏗️ Current E2E Test Setup

### Test Project Structure

```
tests/AcademicAssessment.Tests.UI/
├── BasicFunctionalityTests.cs    # Current HTTP-based tests
├── Fixtures/                     # Test data and helpers
└── Workflows/                    # (Future) Full Playwright scenarios
```

### Current State: HTTP-Based Tests

**Currently**, UI tests use **simple HTTP requests** (not full Playwright automation):

```csharp
using System.Net;
using FluentAssertions;
using Xunit;

public class BasicFunctionalityTests
{
    [Fact]
    public async Task StudentApp_HomeShouldLoad()
    {
        // Arrange
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(15);
        
        // Act
        var response = await client.GetAsync("http://localhost:5049");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("EduMind.AI");
        content.Should().NotContain("unhandled error");
    }
    
    [Fact]
    public async Task StudentApp_AssessmentDetailShouldNotError()
    {
        // Arrange
        using var client = new HttpClient();
        var assessmentId = "6c8d46f0-9cf6-4ba6-8361-5a6dc7bb9e38";
        
        // Act
        var response = await client.GetAsync(
            $"http://localhost:5049/assessment/{assessmentId}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Introduction to Algebra");
    }
}
```

**Limitations:**

- ❌ No browser automation (can't click buttons, fill forms)
- ❌ No JavaScript execution
- ❌ No real user interaction testing
- ✅ Fast smoke tests for page loads

---

## 🎭 Playwright Setup

### Installation

Playwright is already installed via NuGet:

```xml
<PackageReference Include="Microsoft.Playwright" Version="1.48.0" />
```

**Install Playwright browsers:**

```bash
# Install browsers (Chromium, Firefox, WebKit)
pwsh bin/Debug/net9.0/playwright.ps1 install

# Or on Linux/Mac
./bin/Debug/net9.0/playwright.sh install
```

### Basic Playwright Test Structure

```csharp
using Microsoft.Playwright;
using Xunit;

public class StudentAssessmentWorkflowTests : IAsyncLifetime
{
    private IPlaywright? playwright;
    private IBrowser? browser;
    private IPage? page;
    
    public async Task InitializeAsync()
    {
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,  // Set to false for debugging
            SlowMo = 100      // Slow down actions for visibility
        });
        page = await browser.NewPageAsync();
    }
    
    public async Task DisposeAsync()
    {
        await page?.CloseAsync()!;
        await browser?.DisposeAsync()!;
        playwright?.Dispose();
    }
    
    [Fact]
    public async Task StudentTakesAssessment_EndToEnd_Success()
    {
        // Navigate to app
        await page!.GotoAsync("https://localhost:5049");
        
        // Verify home page
        await Expect(page.Locator("h1")).ToContainTextAsync("Your Assessments");
        
        // Click assessment
        await page.ClickAsync("text=Introduction to Algebra");
        
        // Verify detail page loaded
        await Expect(page.Locator(".assessment-title"))
            .ToContainTextAsync("Introduction to Algebra");
        
        // Start assessment
        await page.ClickAsync("text=Start Assessment");
        
        // Answer questions
        await page.ClickAsync("input[value='A']");
        await page.ClickAsync("text=Next");
        
        // ... more interactions ...
        
        // Submit
        await page.ClickAsync("text=Submit Assessment");
        
        // Verify results
        await Expect(page.Locator(".success-message")).ToBeVisibleAsync();
        await Expect(page.Locator(".score")).ToContainTextAsync("%");
    }
}
```

---

## 🎬 Common Playwright Patterns

### Navigation

```csharp
// Navigate to URL
await page.GotoAsync("https://localhost:5049");

// Wait for navigation
await page.ClickAsync("text=Start Assessment");
await page.WaitForNavigationAsync();

// Go back/forward
await page.GoBackAsync();
await page.GoForwardAsync();

// Reload
await page.ReloadAsync();
```

### Locators

```csharp
// By text
var button = page.Locator("text=Start Assessment");

// By CSS selector
var title = page.Locator(".assessment-title");
var input = page.Locator("input[type='text']");

// By data-testid (recommended)
var startButton = page.Locator("[data-testid='start-button']");

// By role
var heading = page.GetByRole(AriaRole.Heading, new() { Name = "Assessments" });
var link = page.GetByRole(AriaRole.Link, new() { Name = "Start" });

// Chaining
var specificItem = page
    .Locator(".assessment-list")
    .Locator("text=Algebra");
```

### Interactions

```csharp
// Click
await page.ClickAsync("text=Start");

// Fill input
await page.FillAsync("input[name='answer']", "42");

// Select dropdown
await page.SelectOptionAsync("select[name='grade']", "10");

// Check/uncheck
await page.CheckAsync("input[type='checkbox']");
await page.UncheckAsync("input[type='checkbox']");

// Keyboard
await page.PressAsync("input", "Enter");
await page.Keyboard.TypeAsync("Hello World", new() { Delay = 100 });

// Mouse
await page.HoverAsync(".tooltip-trigger");
await page.Mouse.ClickAsync(100, 200);
```

### Assertions (Expect)

```csharp
// Visibility
await Expect(page.Locator(".success")).ToBeVisibleAsync();
await Expect(page.Locator(".error")).ToBeHiddenAsync();

// Text content
await Expect(page.Locator("h1")).ToContainTextAsync("Welcome");
await Expect(page.Locator(".score")).ToHaveTextAsync("85%");

// Attributes
await Expect(page.Locator("input")).ToHaveAttributeAsync("disabled", "");
await Expect(page.Locator("button")).ToHaveClassAsync("btn-primary");

// Count
await Expect(page.Locator(".question")).ToHaveCountAsync(10);

// URL
await Expect(page).ToHaveURLAsync(/.*\/assessment\/.*\/session/);
await Expect(page).ToHaveTitleAsync("EduMind.AI - Assessment");
```

### Waiting

```csharp
// Wait for element
await page.WaitForSelectorAsync(".results-loaded");

// Wait for response
await page.WaitForResponseAsync(response => 
    response.Url.Contains("/api/assessment") && 
    response.Status == 200);

// Wait for timeout
await page.WaitForTimeoutAsync(2000);  // 2 seconds

// Wait for function
await page.WaitForFunctionAsync("() => document.querySelector('.ready')");
```

---

## 🎯 Critical User Workflows

### 1. Student Takes Assessment

```csharp
[Fact]
public async Task StudentTakesAssessment_AllCorrect_ShowsHighScore()
{
    // Navigate to student app
    await page!.GotoAsync("https://localhost:5049");
    
    // Find and click assessment
    await page.ClickAsync("text=Introduction to Algebra");
    await page.WaitForURLAsync("**/assessment/**");
    
    // Start assessment
    await page.ClickAsync("[data-testid='start-button']");
    await page.WaitForURLAsync("**/session/**");
    
    // Answer all questions correctly
    var questionCount = await page.Locator(".question-number").CountAsync();
    
    for (int i = 0; i < questionCount; i++)
    {
        // Select correct answer (assuming first option is correct for test)
        await page.ClickAsync("input[type='radio']:first-of-type");
        
        if (i < questionCount - 1)
        {
            await page.ClickAsync("text=Next");
        }
        else
        {
            await page.ClickAsync("text=Submit");
        }
        
        await Task.Delay(500);  // Allow for transitions
    }
    
    // Verify results page
    await Expect(page.Locator(".results-title"))
        .ToContainTextAsync("Assessment Complete");
    
    var score = await page.Locator("[data-testid='final-score']").TextContentAsync();
    int.Parse(score!.Replace("%", "")).Should().BeGreaterThan(90);
    
    // Verify feedback is shown
    await Expect(page.Locator(".feedback-section")).ToBeVisibleAsync();
}
```

### 2. Teacher Creates Assessment

```csharp
[Fact]
public async Task TeacherCreatesAssessment_Valid_SuccessfullyCreated()
{
    // Login as teacher
    await LoginAsTeacherAsync();
    
    // Navigate to assessment creation
    await page!.GotoAsync("https://localhost:5050/assessments/create");
    
    // Fill in assessment details
    await page.FillAsync("input[name='Title']", "New Math Assessment");
    await page.SelectOptionAsync("select[name='Subject']", "Mathematics");
    await page.SelectOptionAsync("select[name='GradeLevel']", "HighSchool");
    await page.SelectOptionAsync("select[name='Difficulty']", "Intermediate");
    await page.FillAsync("input[name='Duration']", "45");
    
    // Add topics
    await page.FillAsync("input[name='Topic']", "Algebra");
    await page.ClickAsync("text=Add Topic");
    await page.FillAsync("input[name='Topic']", "Geometry");
    await page.ClickAsync("text=Add Topic");
    
    // Submit
    await page.ClickAsync("button[type='submit']");
    
    // Verify success
    await Expect(page.Locator(".toast-success"))
        .ToContainTextAsync("Assessment created successfully");
    
    await page.WaitForURLAsync("**/assessments");
    
    // Verify assessment appears in list
    await Expect(page.Locator("text=New Math Assessment")).ToBeVisibleAsync();
}
```

### 3. Navigation and Error Handling

```csharp
[Fact]
public async Task InvalidAssessmentId_Shows404Page()
{
    // Navigate to non-existent assessment
    var invalidId = Guid.NewGuid();
    await page!.GotoAsync($"https://localhost:5049/assessment/{invalidId}");
    
    // Verify 404 handling
    await Expect(page.Locator("h1")).ToContainTextAsync("Not Found");
    await Expect(page.Locator(".error-message"))
        .ToContainTextAsync("assessment could not be found");
    
    // Verify back to home link works
    await page.ClickAsync("text=Back to Home");
    await Expect(page).ToHaveURLAsync("https://localhost:5049");
}
```

---

## 🐛 Debugging E2E Tests

### Headed Mode (See Browser)

```csharp
browser = await playwright.Chromium.LaunchAsync(new()
{
    Headless = false,  // Show browser
    SlowMo = 1000      // Slow down by 1 second per action
});
```

### Screenshots

```csharp
// Take screenshot on failure
try
{
    await page.ClickAsync("text=Submit");
    await Expect(page.Locator(".success")).ToBeVisibleAsync();
}
catch
{
    await page.ScreenshotAsync(new() 
    { 
        Path = $"test-failure-{DateTime.Now:yyyyMMddHHmmss}.png" 
    });
    throw;
}

// Or automatically on failure
[Fact]
public async Task TestWithAutoScreenshot()
{
    try
    {
        // Test code
    }
    catch (Exception)
    {
        await TakeScreenshotOnFailureAsync();
        throw;
    }
}
```

### Video Recording

```csharp
browser = await playwright.Chromium.LaunchAsync();
var context = await browser.NewContextAsync(new()
{
    RecordVideoDir = "videos/",
    RecordVideoSize = new() { Width = 1280, Height = 720 }
});
page = await context.NewPageAsync();

// Video saved automatically on context close
```

### Trace Viewer

```csharp
var context = await browser.NewContextAsync();
await context.Tracing.StartAsync(new()
{
    Screenshots = true,
    Snapshots = true
});

// Run test...

await context.Tracing.StopAsync(new() { Path = "trace.zip" });

// View with: pwsh playwright.ps1 show-trace trace.zip
```

---

## 🎨 Page Object Model (Recommended)

Encapsulate page interactions:

```csharp
public class AssessmentListPage
{
    private readonly IPage page;
    
    public AssessmentListPage(IPage page) => this.page = page;
    
    public async Task NavigateAsync()
    {
        await page.GotoAsync("https://localhost:5049");
    }
    
    public async Task<AssessmentDetailPage> SelectAssessmentAsync(string title)
    {
        await page.ClickAsync($"text={title}");
        return new AssessmentDetailPage(page);
    }
    
    public async Task<int> GetAssessmentCountAsync()
    {
        return await page.Locator(".assessment-card").CountAsync();
    }
}

public class AssessmentDetailPage
{
    private readonly IPage page;
    
    public AssessmentDetailPage(IPage page) => this.page = page;
    
    public async Task<string> GetTitleAsync()
    {
        return await page.Locator("[data-testid='assessment-title']")
            .TextContentAsync() ?? "";
    }
    
    public async Task<AssessmentSessionPage> StartAssessmentAsync()
    {
        await page.ClickAsync("[data-testid='start-button']");
        return new AssessmentSessionPage(page);
    }
}

// Usage in test
[Fact]
public async Task StudentWorkflow_UsingPageObjects()
{
    var listPage = new AssessmentListPage(page!);
    await listPage.NavigateAsync();
    
    var detailPage = await listPage.SelectAssessmentAsync("Introduction to Algebra");
    var title = await detailPage.GetTitleAsync();
    title.Should().Contain("Algebra");
    
    var sessionPage = await detailPage.StartAssessmentAsync();
    // ... continue workflow
}
```

---

## 🏃 Running E2E Tests

### Local Execution

```bash
# Ensure services are running
docker-compose up -d

# Start student app
dotnet run --project src/AcademicAssessment.StudentApp/

# In another terminal, run E2E tests
dotnet test tests/AcademicAssessment.Tests.UI/
```

### Specific Browser

```csharp
// Test with different browsers
[Theory]
[InlineData("chromium")]
[InlineData("firefox")]
[InlineData("webkit")]
public async Task CrossBrowser_AssessmentWorks(string browserType)
{
    var browser = browserType switch
    {
        "chromium" => await playwright!.Chromium.LaunchAsync(),
        "firefox" => await playwright!.Firefox.LaunchAsync(),
        "webkit" => await playwright!.WebKit.LaunchAsync(),
        _ => throw new ArgumentException()
    };
    
    var page = await browser.NewPageAsync();
    
    // Test logic...
    
    await page.CloseAsync();
    await browser.DisposeAsync();
}
```

### CI/CD Execution

GitHub Actions runs E2E tests with all services:

```yaml
jobs:
  e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - name: Start services
        run: docker-compose up -d
      
      - name: Install Playwright browsers
        run: pwsh playwright.ps1 install --with-deps
      
      - name: Run E2E tests
        run: dotnet test tests/AcademicAssessment.Tests.UI/
      
      - name: Upload screenshots on failure
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: test-screenshots
          path: screenshots/
```

---

## ⚠️ E2E Testing Best Practices

### 1. Use data-testid Attributes

```razor
@* In Blazor component *@
<button @onclick="StartAssessment" data-testid="start-button">
    Start Assessment
</button>

<div class="assessment-title" data-testid="assessment-title">
    @Assessment.Title
</div>
```

```csharp
// In test - resilient to CSS/text changes
await page.ClickAsync("[data-testid='start-button']");
```

### 2. Wait for Network Idle

```csharp
await page.GotoAsync("https://localhost:5049", new()
{
    WaitUntil = WaitUntilState.NetworkIdle
});
```

### 3. Test Critical Paths Only

E2E tests are slow - focus on:

- ✅ Happy path user workflows
- ✅ Most common user actions
- ✅ Business-critical features
- ❌ Every edge case (use unit/integration tests)

### 4. Isolate Test Data

```csharp
// Create test-specific data before test
await SeedTestAssessmentAsync(title: $"E2E Test {Guid.NewGuid()}");

// Clean up after test
await CleanupTestDataAsync();
```

---

## 📊 Current vs Future State

### Current (HTTP Tests)

```csharp
✅ Fast smoke tests
✅ Page load verification
❌ No user interactions
❌ No JavaScript execution
```

### Future (Full Playwright)

```csharp
✅ Complete workflow testing
✅ Form interactions
✅ JavaScript execution
✅ Visual validation
✅ Cross-browser testing
```

---

**Last Updated:** 2025-10-25  
**Related:** [Integration Testing](./04-integration-testing.md) | [AI Testing](./06-ai-agent-testing.md) | [Troubleshooting](./12-troubleshooting.md)
