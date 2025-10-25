# AI/Agent Testing Guide

**Purpose:** Test AI-powered features using Ollama LLM integration and multi-agent orchestration.

**Audience:** Developers working on question generation, feedback agents, and LLM integrations.

---

## 🤖 AI Testing Overview

EduMind.AI uses **local Ollama LLM** (llama3.2:3b) for AI-powered assessment features:

✅ Question generation based on topics  
✅ Answer evaluation and scoring  
✅ Personalized feedback generation  
✅ Difficulty adaptation  
✅ Multi-agent orchestration (Question Agent + Feedback Agent)

**AI Testing Challenges:**

- ⚠️ **Non-deterministic:** Same prompt may yield different responses
- ⚠️ **Slow:** LLM inference takes seconds to minutes
- ⚠️ **Quality:** Hard to assert exact outputs
- ⚠️ **Dependencies:** Requires Ollama service running

---

## 🏗️ AI Test Architecture

### Test Strategy Pyramid

```
        /\        E2E: Full assessment with real LLM
       /  \       • Complete user workflow
      /____\      • Manual verification
     /      \     
    / Quality \   Quality Tests: Semantic validation
   /   Tests   \  • Answer relevance
  /____________\  • Feedback usefulness
 /              \ 
/ Integration    \ Integration: Ollama connectivity
/   & Stubs      \ • Service health checks
/__________________\ • Stub responses for speed
```

### Test Types

| Test Type | Speed | LLM | Deterministic | Purpose |
|-----------|-------|-----|---------------|---------|
| **Stub** | Fast | ❌ | ✅ | Unit testing agent logic |
| **Integration** | Medium | ✅ | ❌ | Verify Ollama connectivity |
| **Quality** | Slow | ✅ | ⚠️ | Validate output quality |
| **E2E** | Very Slow | ✅ | ❌ | Full workflow verification |

---

## 🧪 Testing Ollama Integration

### Ollama Health Check Script

We have a shell script to verify Ollama setup:

```bash
# Run integration test script
./tests/test-ollama-integration.sh
```

**Script Output:**

```
🔍 Testing Ollama Integration
========================================

✅ Ollama service is running
✅ Model llama3.2:3b is available
✅ Simple query test passed
✅ Semantic evaluation test passed

========================================
🎉 All tests passed!

Next steps for EduMind.AI integration:
1. Configure appsettings.json with Ollama URL
2. Implement QuestionGenerationAgent
3. Implement FeedbackAgent
4. Test with real assessment workflow
```

### C# Ollama Health Check

```csharp
public class OllamaHealthTests
{
    private const string OllamaBaseUrl = "http://localhost:11434";
    private readonly HttpClient client = new();
    
    [Fact]
    public async Task Ollama_ServiceIsRunning()
    {
        // Act
        var response = await client.GetAsync($"{OllamaBaseUrl}/api/tags");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    
    [Fact]
    public async Task Ollama_HasRequiredModel()
    {
        // Act
        var response = await client.GetAsync($"{OllamaBaseUrl}/api/tags");
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        content.Should().Contain("llama3.2:3b");
    }
    
    [Fact]
    public async Task Ollama_CanGenerateResponse()
    {
        // Arrange
        var request = new
        {
            model = "llama3.2:3b",
            prompt = "What is 2+2? Answer with just the number.",
            stream = false
        };
        
        // Act
        var response = await client.PostAsJsonAsync(
            $"{OllamaBaseUrl}/api/generate",
            request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var text = result.GetProperty("response").GetString();
        text.Should().NotBeNullOrEmpty();
    }
}
```

---

## 🎭 Stubbing AI Responses

For **fast, deterministic unit tests**, stub AI responses:

### Stub LLM Service

```csharp
public class StubOllamaService : IOllamaService
{
    public Task<string> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        // Return predictable responses based on prompt
        if (prompt.Contains("generate question"))
        {
            return Task.FromResult(@"
                {
                    ""question"": ""What is the capital of France?"",
                    ""options"": [""Paris"", ""London"", ""Berlin"", ""Madrid""],
                    ""correctAnswer"": ""Paris"",
                    ""difficulty"": ""Easy""
                }
            ");
        }
        
        if (prompt.Contains("evaluate answer"))
        {
            return Task.FromResult(@"
                {
                    ""isCorrect"": true,
                    ""confidence"": 0.95,
                    ""explanation"": ""The answer is correct.""
                }
            ");
        }
        
        return Task.FromResult("Default stub response");
    }
}

// Usage in tests
[Fact]
public async Task QuestionAgent_GenerateQuestion_ReturnsValidQuestion()
{
    // Arrange
    var stubLlm = new StubOllamaService();
    var agent = new QuestionGenerationAgent(stubLlm);
    
    // Act
    var question = await agent.GenerateQuestionAsync(
        topic: "Geography",
        difficulty: "Easy");
    
    // Assert
    question.Should().NotBeNull();
    question.Question.Should().NotBeEmpty();
    question.Options.Should().HaveCount(4);
    question.CorrectAnswer.Should().NotBeEmpty();
}
```

### Conditional Stubbing

Use stub in tests, real LLM in production:

```csharp
// In Program.cs
if (builder.Environment.IsDevelopment() || builder.Environment.IsStaging())
{
    builder.Services.AddScoped<IOllamaService, OllamaService>();
}
else
{
    builder.Services.AddScoped<IOllamaService, StubOllamaService>();
}

// Or via configuration
var llmProvider = builder.Configuration["LLM:Provider"];  // "Ollama" or "Stub"

if (llmProvider == "Stub")
{
    builder.Services.AddScoped<IOllamaService, StubOllamaService>();
}
else
{
    builder.Services.AddScoped<IOllamaService, OllamaService>();
}
```

---

## 🧠 Testing AI Agents

### Question Generation Agent

```csharp
public class QuestionGenerationAgentTests
{
    [Fact]
    public async Task GenerateQuestion_ValidTopic_ReturnsQuestion()
    {
        // Arrange
        var ollama = new OllamaService(
            new HttpClient(),
            Options.Create(new OllamaConfig 
            { 
                BaseUrl = "http://localhost:11434",
                ModelName = "llama3.2:3b"
            }));
        
        var agent = new QuestionGenerationAgent(ollama);
        
        // Act
        var result = await agent.GenerateQuestionAsync(
            topic: "Algebra",
            difficulty: "Intermediate",
            gradeLevel: "High School");
        
        // Assert
        result.Should().NotBeNull();
        result.Question.Should().NotBeEmpty();
        result.Options.Should().HaveCountGreaterThan(1);
        result.CorrectAnswer.Should().NotBeEmpty();
        result.Options.Should().Contain(result.CorrectAnswer);
        
        // Quality checks
        result.Question.Should().Contain("algebra", 
            because: "question should relate to topic");
        result.Question.Length.Should().BeGreaterThan(10,
            because: "question should be sufficiently detailed");
    }
    
    [Theory]
    [InlineData("Elementary", "Easy")]
    [InlineData("Middle School", "Intermediate")]
    [InlineData("High School", "Advanced")]
    public async Task GenerateQuestion_VariesByDifficulty(
        string gradeLevel, 
        string difficulty)
    {
        // Arrange
        var ollama = new OllamaService(/*...*/);
        var agent = new QuestionGenerationAgent(ollama);
        
        // Act
        var result = await agent.GenerateQuestionAsync(
            topic: "Mathematics",
            difficulty: difficulty,
            gradeLevel: gradeLevel);
        
        // Assert
        result.Should().NotBeNull();
        result.Difficulty.Should().Be(difficulty);
        
        // Could check complexity heuristics
        // e.g., Advanced questions should have more words
    }
}
```

### Feedback Agent

```csharp
public class FeedbackAgentTests
{
    [Fact]
    public async Task GenerateFeedback_CorrectAnswer_ProvidesEncouragement()
    {
        // Arrange
        var ollama = new OllamaService(/*...*/);
        var agent = new FeedbackAgent(ollama);
        
        var question = CreateTestQuestion();
        var studentAnswer = question.CorrectAnswer;
        
        // Act
        var feedback = await agent.GenerateFeedbackAsync(
            question: question,
            studentAnswer: studentAnswer,
            isCorrect: true);
        
        // Assert
        feedback.Should().NotBeNullOrEmpty();
        feedback.Should().Contain("correct", 
            because: "feedback should acknowledge correctness");
        
        // Sentiment check (optional)
        feedback.Should().MatchRegex("(good|great|excellent|well done)",
            because: "feedback should be encouraging");
    }
    
    [Fact]
    public async Task GenerateFeedback_IncorrectAnswer_ProvidesGuidance()
    {
        // Arrange
        var ollama = new OllamaService(/*...*/);
        var agent = new FeedbackAgent(ollama);
        
        var question = CreateTestQuestion();
        var studentAnswer = question.Options
            .First(o => o != question.CorrectAnswer);  // Wrong answer
        
        // Act
        var feedback = await agent.GenerateFeedbackAsync(
            question: question,
            studentAnswer: studentAnswer,
            isCorrect: false);
        
        // Assert
        feedback.Should().NotBeNullOrEmpty();
        feedback.Should().NotContain(studentAnswer,
            because: "feedback shouldn't repeat wrong answer");
        feedback.Should().ContainAny(new[] 
            { "try", "consider", "think about", "remember" },
            because: "feedback should provide guidance");
    }
}
```

---

## 🎯 Quality Validation Tests

### Semantic Validation

Test that responses are **semantically appropriate**, not exact matches:

```csharp
public class QuestionQualityTests
{
    [Fact]
    public async Task GeneratedQuestion_IsRelevantToTopic()
    {
        // Arrange
        var agent = CreateRealAgent();
        var topic = "Photosynthesis";
        
        // Act
        var question = await agent.GenerateQuestionAsync(
            topic: topic,
            difficulty: "Intermediate");
        
        // Assert - Semantic checks
        var relevantTerms = new[] { "photosynthesis", "plant", "chlorophyll", 
            "sunlight", "CO2", "oxygen", "glucose" };
        
        var questionText = question.Question.ToLower();
        relevantTerms.Should().Contain(term => questionText.Contains(term),
            because: "question should relate to photosynthesis");
    }
    
    [Fact]
    public async Task GeneratedQuestion_HasAppropriateComplexity()
    {
        // Arrange
        var agent = CreateRealAgent();
        
        // Act
        var easyQuestion = await agent.GenerateQuestionAsync(
            topic: "Addition",
            difficulty: "Easy",
            gradeLevel: "Elementary");
        
        var hardQuestion = await agent.GenerateQuestionAsync(
            topic: "Calculus",
            difficulty: "Advanced",
            gradeLevel: "College");
        
        // Assert - Complexity heuristics
        easyQuestion.Question.Split(' ').Length
            .Should().BeLessThan(hardQuestion.Question.Split(' ').Length,
                because: "advanced questions typically more complex");
        
        // Easy questions should use simpler vocabulary
        var simpleWords = new[] { "add", "plus", "sum", "total" };
        simpleWords.Should().Contain(word => 
            easyQuestion.Question.ToLower().Contains(word));
    }
}
```

### Answer Evaluation Quality

```csharp
public class AnswerEvaluationQualityTests
{
    [Fact]
    public async Task EvaluateAnswer_ObviouslyCorrect_HighConfidence()
    {
        // Arrange
        var agent = CreateRealAgent();
        var question = new Question
        {
            Question = "What is the capital of France?",
            CorrectAnswer = "Paris"
        };
        
        // Act
        var evaluation = await agent.EvaluateAnswerAsync(
            question: question,
            studentAnswer: "Paris");
        
        // Assert
        evaluation.IsCorrect.Should().BeTrue();
        evaluation.Confidence.Should().BeGreaterThan(0.9,
            because: "obvious correct answer should have high confidence");
    }
    
    [Fact]
    public async Task EvaluateAnswer_TypographicalVariation_StillCorrect()
    {
        // Arrange
        var agent = CreateRealAgent();
        var question = new Question
        {
            Question = "What is the capital of France?",
            CorrectAnswer = "Paris"
        };
        
        // Act - Test with variations
        var variations = new[] { "paris", "PARIS", "Paris." };
        
        foreach (var answer in variations)
        {
            var evaluation = await agent.EvaluateAnswerAsync(
                question: question,
                studentAnswer: answer);
            
            // Assert
            evaluation.IsCorrect.Should().BeTrue(
                because: $"'{answer}' should be recognized as correct");
        }
    }
}
```

---

## 🔀 Multi-Agent Orchestration Tests

### Orchestration Integration

```csharp
public class MultiAgentOrchestratorTests
{
    [Fact]
    public async Task GenerateAssessmentQuestions_ReturnsMultipleQuestions()
    {
        // Arrange
        var ollama = new OllamaService(/*...*/);
        var questionAgent = new QuestionGenerationAgent(ollama);
        var feedbackAgent = new FeedbackAgent(ollama);
        var orchestrator = new AssessmentOrchestrator(questionAgent, feedbackAgent);
        
        // Act
        var questions = await orchestrator.GenerateQuestionsAsync(
            topics: new[] { "Algebra", "Geometry" },
            count: 5,
            difficulty: "Intermediate");
        
        // Assert
        questions.Should().HaveCount(5);
        questions.Should().OnlyContain(q => 
            q.Question != null && 
            q.Options.Any() && 
            q.CorrectAnswer != null);
        
        // Diversity check
        questions.Select(q => q.Question).Distinct().Should().HaveCount(5,
            because: "questions should be unique");
    }
    
    [Fact]
    public async Task ProcessStudentResponses_GeneratesFeedbackForEach()
    {
        // Arrange
        var orchestrator = CreateRealOrchestrator();
        var questions = await GenerateTestQuestionsAsync(count: 3);
        var responses = questions.Select(q => new StudentResponse
        {
            QuestionId = q.Id,
            Answer = q.CorrectAnswer,  // All correct
            TimeTaken = TimeSpan.FromSeconds(30)
        }).ToList();
        
        // Act
        var results = await orchestrator.ProcessResponsesAsync(
            questions: questions,
            responses: responses);
        
        // Assert
        results.Should().HaveCount(3);
        results.Should().OnlyContain(r => r.Feedback != null);
        results.Should().OnlyContain(r => r.Score > 0);
    }
}
```

### Agent Communication Tests

```csharp
public class AgentCommunicationTests
{
    [Fact]
    public async Task FeedbackAgent_UsesQuestionContext()
    {
        // Arrange
        var orchestrator = CreateRealOrchestrator();
        var question = await GenerateTestQuestionAsync(topic: "Physics");
        
        var studentResponse = new StudentResponse
        {
            QuestionId = question.Id,
            Answer = "Wrong answer",
            IsCorrect = false
        };
        
        // Act
        var feedback = await orchestrator.GenerateFeedbackAsync(
            question: question,
            response: studentResponse);
        
        // Assert
        // Feedback should reference the question topic
        feedback.Should().Contain("physics", 
            because: "feedback should be context-aware");
        
        // Feedback should not just say "wrong"
        feedback.Length.Should().BeGreaterThan(50,
            because: "feedback should provide substantive guidance");
    }
}
```

---

## 📊 Performance & Reliability

### Timeout Handling

```csharp
[Fact]
public async Task GenerateQuestion_SlowLLM_RespectsTimeout()
{
    // Arrange
    var config = new OllamaConfig
    {
        BaseUrl = "http://localhost:11434",
        ModelName = "llama3.2:3b",
        Timeout = TimeSpan.FromSeconds(30)  // Reasonable timeout
    };
    
    var ollama = new OllamaService(new HttpClient(), Options.Create(config));
    var agent = new QuestionGenerationAgent(ollama);
    
    // Act
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(35));
    
    var act = async () => await agent.GenerateQuestionAsync(
        topic: "Complex topic that might take long",
        difficulty: "Advanced",
        cancellationToken: cts.Token);
    
    // Assert - Should complete within timeout or throw
    await act.Should().CompleteWithinAsync(
        TimeSpan.FromSeconds(35),
        because: "LLM operations should respect timeouts");
}
```

### Retry Logic

```csharp
[Fact]
public async Task GenerateQuestion_TransientFailure_RetriesSuccessfully()
{
    // Arrange
    var mockOllama = new Mock<IOllamaService>();
    
    // First call fails, second succeeds
    mockOllama.SetupSequence(o => o.GenerateAsync(It.IsAny<string>(), default))
        .ThrowsAsync(new HttpRequestException("Connection failed"))
        .ReturnsAsync("{ \"question\": \"Test\", \"options\": [...] }");
    
    var agent = new QuestionGenerationAgent(mockOllama.Object);
    
    // Act
    var result = await agent.GenerateQuestionWithRetryAsync(
        topic: "Test",
        maxRetries: 3);
    
    // Assert
    result.Should().NotBeNull();
    mockOllama.Verify(o => o.GenerateAsync(It.IsAny<string>(), default), 
        Times.Exactly(2));
}
```

---

## 🛠️ Testing Strategies

### Local Development

```bash
# 1. Start Ollama
ollama serve

# 2. Ensure model is available
ollama pull llama3.2:3b

# 3. Run AI tests (slow)
dotnet test tests/AcademicAssessment.Tests.Unit/ \
    --filter "FullyQualifiedName~Agent"

# Or use stub for speed
export LLM__Provider=Stub
dotnet test
```

### CI/CD

**Option 1: Skip AI Tests in CI** (faster builds)

```csharp
[Fact]
[Trait("Category", "RequiresOllama")]
public async Task TestThatNeedsOllama() { /*...*/ }
```

```bash
# Skip in CI
dotnet test --filter "Category!=RequiresOllama"
```

**Option 2: Run Ollama in CI** (complete validation)

```yaml
# .github/workflows/ai-tests.yml
jobs:
  ai-tests:
    runs-on: ubuntu-latest
    steps:
      - name: Install Ollama
        run: |
          curl https://ollama.ai/install.sh | sh
          ollama serve &
          sleep 5
          ollama pull llama3.2:3b
      
      - name: Run AI tests
        run: dotnet test --filter "Category=RequiresOllama"
```

---

## 🎓 Best Practices

### 1. Use Stubs for Unit Tests

```csharp
// FAST unit tests with stubs
[Fact]
public async Task AgentLogic_WithStub_Fast()
{
    var stubLlm = new StubOllamaService();
    // Test completes in milliseconds
}
```

### 2. Use Real LLM for Integration Tests

```csharp
// SLOW integration tests with real LLM
[Fact]
[Trait("Category", "RequiresOllama")]
public async Task AgentQuality_WithRealLLM_Validates()
{
    var realLlm = new OllamaService(/*...*/);
    // Test takes seconds but validates real behavior
}
```

### 3. Semantic Assertions, Not Exact Matches

```csharp
// DON'T assert exact text (non-deterministic)
feedback.Should().Be("Great job! You got it right.");

// DO assert semantic content
feedback.Should().MatchRegex("(great|good|correct|right)");
feedback.Length.Should().BeGreaterThan(10);
```

### 4. Test Quality, Not Just Functionality

```csharp
// Not just "does it return something"
result.Should().NotBeNull();

// But "is it good quality"
result.Should().NotContainAny(new[] { "error", "null", "undefined" });
result.Relevance.Should().BeGreaterThan(0.7);
result.Question.Should().EndWith("?");
```

---

## 📚 Test Scripts Reference

**Ollama Integration Test:**

```bash
./tests/test-ollama-integration.sh
```

**Multi-Agent Test:**

```bash
./tests/test-multi-agent-ollama.sh
```

**Phase 5 Agent Tests:**

```bash
./tests/test-phase5-agents.sh
```

---

**Last Updated:** 2025-10-25  
**Related:** [Integration Testing](./04-integration-testing.md) | [Unit Testing](./03-unit-testing.md) | [Troubleshooting](./12-troubleshooting.md)
