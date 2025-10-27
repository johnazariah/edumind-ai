# Story 005: Optimize Ollama LLM Performance (Hybrid Azure OpenAI Strategy)

**Priority:** P1 - Production Quality  
**Status:** Ready for Implementation  
**Effort:** Medium (1 week)  
**Dependencies:** None


**GitHub Issue:** https://github.com/johnazariah/edumind-ai/issues/9

---

## Problem Statement

Ollama LLM inference currently takes 60-120 seconds per evaluation, which is **unacceptable for production user experience**. Students and teachers would experience significant delays waiting for AI-generated feedback.

**Current Performance:**

- **Ollama (CPU):** 60-120 seconds per inference
- **Production Target:** <5 seconds per inference
- **User Experience Impact:** Students wait 2+ minutes for feedback

**Options:**

1. GPU acceleration (reduces to 2-5 seconds)
2. Azure OpenAI migration (~1-3 seconds, ~$0.01/evaluation)
3. **Hybrid approach** (Ollama for dev/test, Azure OpenAI for production)

---

## Goals & Success Criteria

### Goals

- Reduce LLM inference time to <5 seconds in production
- Implement provider switching (Ollama ↔ Azure OpenAI)
- Maintain cost-effectiveness (use Ollama where possible)
- Ensure seamless developer experience with local Ollama

### Success Criteria

- [ ] Azure OpenAI integration implemented
- [ ] Configuration-based provider switching
- [ ] Production inference time <5 seconds
- [ ] Development uses local Ollama (no API costs)
- [ ] Staging can test both providers
- [ ] Graceful fallback if primary provider unavailable

---

## Technical Approach

### Hybrid Strategy

```
Environment-Based Provider Selection:

Development → Ollama (local, free, no GPU required)
Testing     → Ollama Stub (fast, deterministic)
Staging     → Azure OpenAI (production-like testing)
Production  → Azure OpenAI (performance + reliability)
```

### Architecture

**Abstraction Layer:**

```csharp
public interface ILLMProvider
{
    Task<Result<string>> GenerateCompletionAsync(string prompt, CancellationToken cancellationToken);
    Task<Result<string>> GenerateFeedbackAsync(string studentResponse, string correctAnswer, CancellationToken cancellationToken);
    string ProviderName { get; }
    bool IsAvailable { get; }
}

// Implementations:
public class OllamaProvider : ILLMProvider { }
public class AzureOpenAIProvider : ILLMProvider { }
public class StubLLMProvider : ILLMProvider { } // Existing
```

**Provider Factory:**

```csharp
public class LLMProviderFactory
{
    public ILLMProvider CreateProvider(LLMProviderType type, IConfiguration config)
    {
        return type switch
        {
            LLMProviderType.Ollama => new OllamaProvider(config),
            LLMProviderType.AzureOpenAI => new AzureOpenAIProvider(config),
            LLMProviderType.Stub => new StubLLMProvider(),
            _ => throw new NotSupportedException($"Provider {type} not supported")
        };
    }
}
```

### Configuration

**appsettings.json:**

```json
{
  "LLM": {
    "Provider": "AzureOpenAI", // "Ollama" | "AzureOpenAI" | "Stub"
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "Model": "llama3.2:3b",
      "TimeoutSeconds": 120
    },
    "AzureOpenAI": {
      "Endpoint": "https://<your-resource>.openai.azure.com/",
      "DeploymentName": "gpt-4o-mini",
      "ApiKey": "<from-key-vault>",
      "TimeoutSeconds": 30
    },
    "FallbackToStub": false
  }
}
```

---

## Task Decomposition

### Task 1: Create LLM Provider Abstraction

- **Description:** Define common interface for all LLM providers
- **Files:**
  - `src/AcademicAssessment.Agents/LLM/ILLMProvider.cs` (new)
  - `src/AcademicAssessment.Agents/LLM/LLMProviderType.cs` (new)
  - `src/AcademicAssessment.Agents/LLM/LLMRequest.cs` (new)
  - `src/AcademicAssessment.Agents/LLM/LLMResponse.cs` (new)
- **Code:**

  ```csharp
  public interface ILLMProvider
  {
      /// <summary>
      /// Generate text completion from prompt
      /// </summary>
      Task<Result<LLMResponse>> GenerateAsync(LLMRequest request, CancellationToken cancellationToken);
      
      /// <summary>
      /// Provider name (e.g., "Ollama", "AzureOpenAI")
      /// </summary>
      string ProviderName { get; }
      
      /// <summary>
      /// Check if provider is available
      /// </summary>
      Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
  }
  
  public record LLMRequest(
      string Prompt,
      int MaxTokens = 1000,
      double Temperature = 0.7,
      string? SystemPrompt = null);
  
  public record LLMResponse(
      string Text,
      int TokensUsed,
      TimeSpan Duration,
      string ModelUsed);
  
  public enum LLMProviderType
  {
      Ollama,
      AzureOpenAI,
      Stub
  }
  ```

- **Acceptance:** Interface compiled, follows Railway-Oriented Programming
- **Dependencies:** None

### Task 2: Refactor Existing OllamaService to Implement ILLMProvider

- **Description:** Adapt existing Ollama implementation to new interface
- **Files:**
  - `src/AcademicAssessment.Agents/Services/OllamaService.cs` (modify)
  - `src/AcademicAssessment.Agents/LLM/OllamaProvider.cs` (new wrapper)
- **Changes:**
  - Keep existing HTTP client logic
  - Wrap in `ILLMProvider` interface
  - Add health check endpoint
  - Implement `IsAvailableAsync()`
- **Acceptance:** Ollama provider implements ILLMProvider interface
- **Dependencies:** Task 1

### Task 3: Implement Azure OpenAI Provider

- **Description:** New provider for Azure OpenAI API
- **Files:**
  - `src/AcademicAssessment.Agents/LLM/AzureOpenAIProvider.cs` (new)
  - `src/AcademicAssessment.Agents/AcademicAssessment.Agents.csproj` (add Azure.AI.OpenAI package)
- **Code:**

  ```csharp
  public class AzureOpenAIProvider : ILLMProvider
  {
      private readonly OpenAIClient _client;
      private readonly string _deploymentName;
      private readonly ILogger<AzureOpenAIProvider> _logger;
      
      public string ProviderName => "AzureOpenAI";
      
      public AzureOpenAIProvider(IConfiguration config, ILogger<AzureOpenAIProvider> logger)
      {
          var endpoint = config["LLM:AzureOpenAI:Endpoint"];
          var apiKey = config["LLM:AzureOpenAI:ApiKey"];
          _deploymentName = config["LLM:AzureOpenAI:DeploymentName"] ?? "gpt-4o-mini";
          
          _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
          _logger = logger;
      }
      
      public async Task<Result<LLMResponse>> GenerateAsync(
          LLMRequest request, 
          CancellationToken cancellationToken)
      {
          var stopwatch = Stopwatch.StartNew();
          
          try
          {
              var chatMessages = new List<ChatMessage>
              {
                  new ChatMessage(ChatRole.System, request.SystemPrompt ?? "You are a helpful AI assistant."),
                  new ChatMessage(ChatRole.User, request.Prompt)
              };
              
              var options = new ChatCompletionsOptions(_deploymentName, chatMessages)
              {
                  MaxTokens = request.MaxTokens,
                  Temperature = (float)request.Temperature
              };
              
              var response = await _client.GetChatCompletionsAsync(options, cancellationToken);
              var completion = response.Value;
              
              stopwatch.Stop();
              
              var llmResponse = new LLMResponse(
                  Text: completion.Choices[0].Message.Content,
                  TokensUsed: completion.Usage.TotalTokens,
                  Duration: stopwatch.Elapsed,
                  ModelUsed: completion.Model);
              
              _logger.LogInformation(
                  "Azure OpenAI completion: {Tokens} tokens in {Duration}ms",
                  llmResponse.TokensUsed,
                  llmResponse.Duration.TotalMilliseconds);
              
              return Result<LLMResponse>.Success(llmResponse);
          }
          catch (Exception ex)
          {
              _logger.LogError(ex, "Azure OpenAI API error");
              return Result<LLMResponse>.Failure(
                  new Error("LLM.AzureOpenAIError", "Failed to generate completion from Azure OpenAI"));
          }
      }
      
      public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
      {
          try
          {
              // Simple health check
              var request = new LLMRequest("Test", MaxTokens: 10);
              var result = await GenerateAsync(request, cancellationToken);
              return result.IsSuccess;
          }
          catch
          {
              return false;
          }
      }
  }
  ```

- **Acceptance:** Azure OpenAI provider functional, tested with API
- **Dependencies:** Task 1

### Task 4: Implement LLM Provider Factory

- **Description:** Factory to create providers based on configuration
- **Files:**
  - `src/AcademicAssessment.Agents/LLM/LLMProviderFactory.cs` (new)
- **Code:**

  ```csharp
  public class LLMProviderFactory
  {
      private readonly IConfiguration _config;
      private readonly IServiceProvider _serviceProvider;
      
      public ILLMProvider CreateProvider()
      {
          var providerType = _config["LLM:Provider"] ?? "Ollama";
          
          return providerType.ToLower() switch
          {
              "ollama" => _serviceProvider.GetRequiredService<OllamaProvider>(),
              "azureopenai" => _serviceProvider.GetRequiredService<AzureOpenAIProvider>(),
              "stub" => _serviceProvider.GetRequiredService<StubLLMProvider>(),
              _ => throw new InvalidOperationException($"Unknown LLM provider: {providerType}")
          };
      }
      
      public async Task<ILLMProvider> CreateWithFallbackAsync(CancellationToken cancellationToken)
      {
          var primary = CreateProvider();
          
          // Check if primary is available
          if (await primary.IsAvailableAsync(cancellationToken))
              return primary;
          
          // Fallback to stub if configured
          if (_config.GetValue<bool>("LLM:FallbackToStub"))
          {
              _logger.LogWarning("Primary LLM provider unavailable, falling back to stub");
              return _serviceProvider.GetRequiredService<StubLLMProvider>();
          }
          
          throw new InvalidOperationException($"LLM provider {primary.ProviderName} is unavailable");
      }
  }
  ```

- **Acceptance:** Factory creates correct provider based on config
- **Dependencies:** Task 2, Task 3

### Task 5: Update Agent Services to Use Provider Abstraction

- **Description:** Refactor agent services to use ILLMProvider interface
- **Files:**
  - `src/AcademicAssessment.Agents/Services/FeedbackGeneratorAgent.cs`
  - `src/AcademicAssessment.Agents/Services/QuestionGeneratorAgent.cs`
  - `src/AcademicAssessment.Agents/Services/DifficultyAnalyzerAgent.cs`
- **Changes:**
  - Replace direct Ollama calls with `ILLMProvider.GenerateAsync()`
  - Remove hardcoded Ollama URLs
  - Add provider name to logs
- **Example:**

  ```csharp
  public class FeedbackGeneratorAgent
  {
      private readonly ILLMProvider _llmProvider; // Changed from OllamaService
      
      public async Task<Result<string>> GenerateFeedbackAsync(
          string studentResponse,
          string correctAnswer,
          CancellationToken cancellationToken)
      {
          var prompt = $"Student answer: {studentResponse}\nCorrect answer: {correctAnswer}\nProvide feedback.";
          
          var request = new LLMRequest(
              Prompt: prompt,
              SystemPrompt: "You are an expert educational feedback generator.",
              MaxTokens: 500,
              Temperature: 0.7);
          
          var result = await _llmProvider.GenerateAsync(request, cancellationToken);
          
          if (result.IsFailure)
              return Result<string>.Failure(result.Error);
          
          return Result<string>.Success(result.Value.Text);
      }
  }
  ```

- **Acceptance:** All agents use ILLMProvider, no direct Ollama dependencies
- **Dependencies:** Task 4

### Task 6: Configure Dependency Injection for Provider Switching

- **Description:** Register providers in DI container based on environment
- **Files:**
  - `src/AcademicAssessment.Web/Program.cs`
  - `src/EduMind.ServiceDefaults/Extensions.cs`
- **Code:**

  ```csharp
  // Register all providers
  builder.Services.AddSingleton<OllamaProvider>();
  builder.Services.AddSingleton<AzureOpenAIProvider>();
  builder.Services.AddSingleton<StubLLMProvider>();
  builder.Services.AddSingleton<LLMProviderFactory>();
  
  // Register active provider based on config
  builder.Services.AddSingleton<ILLMProvider>(sp =>
  {
      var factory = sp.GetRequiredService<LLMProviderFactory>();
      return factory.CreateProvider();
  });
  
  // Log active provider
  var provider = builder.Services.BuildServiceProvider().GetRequiredService<ILLMProvider>();
  builder.Logging.AddConsole().LogInformation("Using LLM provider: {Provider}", provider.ProviderName);
  ```

- **Acceptance:** Correct provider loaded based on appsettings.json
- **Dependencies:** Task 4

### Task 7: Add Azure OpenAI Configuration to appsettings

- **Description:** Add environment-specific LLM configuration
- **Files:**
  - `src/AcademicAssessment.Web/appsettings.json`
  - `src/AcademicAssessment.Web/appsettings.Development.json`
  - `src/AcademicAssessment.Web/appsettings.Production.json`
- **Configuration:**

  ```json
  // appsettings.Development.json
  {
    "LLM": {
      "Provider": "Ollama",
      "Ollama": {
        "BaseUrl": "http://localhost:11434",
        "Model": "llama3.2:3b"
      }
    }
  }
  
  // appsettings.Production.json
  {
    "LLM": {
      "Provider": "AzureOpenAI",
      "AzureOpenAI": {
        "Endpoint": "https://edumind-openai.openai.azure.com/",
        "DeploymentName": "gpt-4o-mini",
        "ApiKey": "@Microsoft.KeyVault(SecretUri=...)"
      },
      "FallbackToStub": false
    }
  }
  ```

- **Acceptance:** Environment-specific provider configuration works
- **Dependencies:** Task 6

### Task 8: Integrate Azure Key Vault for API Keys

- **Description:** Securely store Azure OpenAI API key in Key Vault
- **Files:**
  - `src/EduMind.ServiceDefaults/Extensions.cs`
  - `infra/main.bicep` (add Key Vault resource)
- **Key Vault Setup:**

  ```bicep
  resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
    name: 'edumind-kv-${environmentName}'
    location: location
    properties: {
      sku: { family: 'A', name: 'standard' }
      tenantId: subscription().tenantId
      accessPolicies: [
        {
          tenantId: subscription().tenantId
          objectId: webApp.identity.principalId
          permissions: { secrets: ['get', 'list'] }
        }
      ]
    }
  }
  
  resource openAIApiKey 'Microsoft.KeyVault/vaults/secrets@2023-02-01' = {
    parent: keyVault
    name: 'AzureOpenAI--ApiKey'
    properties: {
      value: '<your-api-key>'
    }
  }
  ```

- **Application Configuration:**

  ```csharp
  builder.Configuration.AddAzureKeyVault(
      new Uri(builder.Configuration["KeyVault:VaultUri"]!),
      new DefaultAzureCredential());
  ```

- **Acceptance:** API key loaded from Key Vault in production
- **Dependencies:** Task 7

### Task 9: Add Performance Monitoring and Metrics

- **Description:** Track LLM provider performance metrics
- **Files:**
  - `src/AcademicAssessment.Agents/Telemetry/LLMMetrics.cs` (new)
- **Metrics:**

  ```csharp
  public class LLMMetrics
  {
      private readonly ILogger<LLMMetrics> _logger;
      
      public void RecordInference(string provider, TimeSpan duration, int tokens, bool success)
      {
          _logger.LogInformation(
              "LLM Inference: Provider={Provider}, Duration={Duration}ms, Tokens={Tokens}, Success={Success}",
              provider, duration.TotalMilliseconds, tokens, success);
          
          // TODO: Add Application Insights custom metrics
      }
  }
  ```

- **Integration:** Call from each provider's `GenerateAsync()` method
- **Acceptance:** Performance metrics logged for all providers
- **Dependencies:** Task 5

### Task 10: Write Unit Tests for Provider Abstraction

- **Description:** Test all provider implementations
- **Files:**
  - `tests/AcademicAssessment.Tests.Unit/LLM/OllamaProviderTests.cs` (new)
  - `tests/AcademicAssessment.Tests.Unit/LLM/AzureOpenAIProviderTests.cs` (new)
  - `tests/AcademicAssessment.Tests.Unit/LLM/LLMProviderFactoryTests.cs` (new)
- **Test Cases:**
  - OllamaProvider generates completion
  - AzureOpenAIProvider generates completion
  - Factory creates correct provider from config
  - Fallback to stub when primary unavailable
  - Error handling for API failures
- **Acceptance:** 90%+ test coverage for LLM providers
- **Dependencies:** Task 4

### Task 11: Write Integration Tests for Azure OpenAI

- **Description:** Integration tests with live Azure OpenAI API
- **Files:**
  - `tests/AcademicAssessment.Tests.Integration/LLM/AzureOpenAIIntegrationTests.cs` (new)
- **Test Scenarios:**
  - Generate feedback for student response
  - Performance: Inference <5 seconds
  - Token usage tracking
  - Error handling (rate limits, invalid keys)
- **Configuration:** Use test API key from Azure Key Vault
- **Acceptance:** Integration tests pass with live API
- **Dependencies:** Task 3

### Task 12: Update Documentation for LLM Configuration

- **Description:** Document LLM provider configuration and switching
- **Files:**
  - `docs/deployment/LLM_CONFIGURATION.md` (new)
  - `.github/specification/06-external-integrations.md`
  - `README.md`
- **Content:**

  ```markdown
  ## LLM Provider Configuration
  
  EduMind.AI supports multiple LLM providers with environment-based switching.
  
  ### Supported Providers
  
  | Provider | Use Case | Performance | Cost |
  |----------|----------|-------------|------|
  | **Ollama** | Local development | 60-120s (CPU) | Free |
  | **Azure OpenAI** | Production | <5s | ~$0.01/eval |
  | **Stub** | Testing | <1s | Free |
  
  ### Configuration
  
  Set provider in `appsettings.json`:
  
  ```json
  {
    "LLM": {
      "Provider": "AzureOpenAI"
    }
  }
  ```
  
  ### Environment Recommendations
  
  - **Development:** Ollama (free, no GPU needed)
  - **CI/CD:** Stub (fast, deterministic)
  - **Staging:** Azure OpenAI (production testing)
  - **Production:** Azure OpenAI (performance + reliability)

  ```
- **Acceptance:** Configuration documented for all environments
- **Dependencies:** Task 11

---

## Acceptance Criteria (Validation)

### Performance Testing

1. **Azure OpenAI Production Performance:**
   - Generate feedback for 10 student responses
   - Expected: Average inference time <5 seconds
   - Expected: No timeouts or errors

2. **Provider Switching:**
   - Change `LLM:Provider` to "Ollama" → Restart → Verify Ollama used
   - Change to "AzureOpenAI" → Restart → Verify Azure OpenAI used
   - Change to "Stub" → Restart → Verify Stub used

3. **Fallback Behavior:**
   - Stop Ollama service
   - Set `FallbackToStub: true`
   - Expected: Application uses Stub provider automatically

### Cost Analysis

- [ ] Track token usage for 100 evaluations
- [ ] Calculate cost: ~100 tokens/eval × $0.0001/token × 100 = ~$0.01 total
- [ ] Verify cost within budget ($0.01-0.02 per evaluation)

### User Experience

- [ ] Student submits response → Feedback appears within 5 seconds
- [ ] No visible difference between providers (from user perspective)
- [ ] Error messages graceful if provider unavailable

---

## Context & References

### Documentation

- [External Integrations](.github/specification/06-external-integrations.md)
- [Known Issues](.github/specification/09e-known-issues-limitations.md)
- [Deployment Guide](docs/deployment/DEPLOYMENT_QUICK_REFERENCE.txt)

### Azure Resources

- [Azure OpenAI Service Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [Azure SDK for .NET](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/openai/Azure.AI.OpenAI)

### Related Code

- `src/AcademicAssessment.Agents/Services/OllamaService.cs` - Existing Ollama implementation
- `src/AcademicAssessment.Agents/Services/StubLLMService.cs` - Existing stub

---

## Notes

- **Cost Management:** Monitor Azure OpenAI usage monthly, set budget alerts
- **Rate Limiting:** Azure OpenAI has token-per-minute limits, implement retry logic
- **Model Selection:** gpt-4o-mini is cost-effective for feedback generation
- **Future Optimization:** Consider caching common feedback patterns

---

**Story Created:** 2025-10-25  
**Created By:** GitHub Copilot  
**Last Updated:** 2025-10-25
