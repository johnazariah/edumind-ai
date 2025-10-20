using System.Net;
using System.Net.Http.Json;
using AcademicAssessment.Core.Models.Dtos;
using AcademicAssessment.Tests.Integration.Helpers;
using FluentAssertions;

namespace AcademicAssessment.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for AssessmentController endpoints.
/// Tests assessment browsing, session management, and results retrieval.
/// Aims for >80% branch coverage.
/// </summary>
public class AssessmentControllerTests : IClassFixture<AuthenticatedWebApplicationFactory<Program>>
{
    private readonly AuthenticatedWebApplicationFactory<Program> _factory;
    private readonly Guid _testSchoolId = Guid.Parse("00000000-0000-0000-0000-000000000010");
    private readonly string _studentToken;

    // Known assessment IDs from mock data
    private readonly Guid _algebraAssessmentId = Guid.Parse("6c8d46f0-9cf6-4ba6-8361-5a6dc7bb9e38");
    private readonly Guid _chemistryAssessmentId = Guid.Parse("b3f7c93a-0b18-4865-8bd9-dbbd4fd438ea");
    private readonly Guid _physicsAssessmentId = Guid.Parse("2c20f965-d1a7-456c-9e36-820a0a64f9da");
    private readonly Guid _englishAssessmentId = Guid.Parse("0c7f9505-8644-41da-8535-9d7e98f3aa4f");
    private readonly Guid _biologyAssessmentId = Guid.Parse("a3d3fb97-19d6-4959-a075-4ca0b8b57d81");

    public AssessmentControllerTests(AuthenticatedWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _studentToken = JwtTokenGenerator.GenerateStudentToken(
            Guid.NewGuid(), "student@test.com", "Test Student", _testSchoolId);
    }

    #region GetAssessments Tests

    [Fact]
    public async Task GetAssessments_ReturnsOkStatus()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var response = await client.GetAsync("/api/v1.0/Assessment");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAssessments_ReturnsListOfAssessments()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var assessments = await client.GetFromJsonAsync<List<AssessmentSummary>>("/api/v1.0/Assessment");

        // Assert
        assessments.Should().NotBeNull();
        assessments.Should().HaveCount(5); // We have 5 mock assessments
    }

    [Fact]
    public async Task GetAssessments_ContainsAlgebraAssessment()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var assessments = await client.GetFromJsonAsync<List<AssessmentSummary>>("/api/v1.0/Assessment");

        // Assert
        assessments.Should().Contain(a => a.Id == _algebraAssessmentId);
        assessments.Should().Contain(a => a.Title == "Introduction to Algebra");
    }

    [Fact]
    public async Task GetAssessments_HasCorrectSubjects()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var assessments = await client.GetFromJsonAsync<List<AssessmentSummary>>("/api/v1.0/Assessment");

        // Assert
        assessments.Should().Contain(a => a.Subject == "Mathematics");
        assessments.Should().Contain(a => a.Subject == "Chemistry");
        assessments.Should().Contain(a => a.Subject == "Physics");
        assessments.Should().Contain(a => a.Subject == "English");
        assessments.Should().Contain(a => a.Subject == "Biology");
    }

    [Fact]
    public async Task GetAssessments_HasDifferentDifficultyLevels()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var assessments = await client.GetFromJsonAsync<List<AssessmentSummary>>("/api/v1.0/Assessment");

        // Assert
        assessments.Should().Contain(a => a.Difficulty == "Beginner");
        assessments.Should().Contain(a => a.Difficulty == "Intermediate");
        assessments.Should().Contain(a => a.Difficulty == "Advanced");
    }

    [Fact]
    public async Task GetAssessments_ContainsProgressInformation()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var assessments = await client.GetFromJsonAsync<List<AssessmentSummary>>("/api/v1.0/Assessment");

        // Assert
        var algebraAssessment = assessments!.First(a => a.Id == _algebraAssessmentId);
        algebraAssessment.IsInProgress.Should().BeTrue();
        algebraAssessment.ProgressPercentage.Should().BeGreaterThan(0);
        algebraAssessment.LastAttemptedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAssessments_ContainsAssessmentsNotStarted()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var assessments = await client.GetFromJsonAsync<List<AssessmentSummary>>("/api/v1.0/Assessment");

        // Assert
        var chemistryAssessment = assessments!.First(a => a.Id == _chemistryAssessmentId);
        chemistryAssessment.IsInProgress.Should().BeFalse();
        chemistryAssessment.ProgressPercentage.Should().BeNull();
        chemistryAssessment.LastAttemptedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetAssessments_ReturnsJsonContentType()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var response = await client.GetAsync("/api/v1.0/Assessment");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    #endregion

    #region GetAssessment Tests

    [Fact]
    public async Task GetAssessment_WithValidId_ReturnsOkStatus()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var response = await client.GetAsync($"/api/v1.0/Assessment/{_algebraAssessmentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAssessment_WithValidId_ReturnsCorrectAssessment()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var assessment = await client.GetFromJsonAsync<AssessmentSummary>(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}");

        // Assert
        assessment.Should().NotBeNull();
        assessment!.Id.Should().Be(_algebraAssessmentId);
        assessment.Title.Should().Be("Introduction to Algebra");
        assessment.Subject.Should().Be("Mathematics");
    }

    [Fact]
    public async Task GetAssessment_WithValidId_ContainsDescription()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var assessment = await client.GetFromJsonAsync<AssessmentSummary>(
            $"/api/v1.0/Assessment/{_chemistryAssessmentId}");

        // Assert
        assessment!.Description.Should().NotBeNullOrEmpty();
        // Just verify description exists and has meaningful content
        assessment.Description.Length.Should().BeGreaterThan(10);
    }

    [Fact]
    public async Task GetAssessment_WithValidId_ContainsLearningObjectives()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var assessment = await client.GetFromJsonAsync<AssessmentSummary>(
            $"/api/v1.0/Assessment/{_physicsAssessmentId}");

        // Assert
        assessment!.LearningObjectives.Should().NotBeNullOrEmpty();
        assessment.LearningObjectives.Should().HaveCountGreaterThan(0);
        assessment.LearningObjectives.Should().Contain(obj => obj.Contains("Newton"));
    }

    [Fact]
    public async Task GetAssessment_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var invalidId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1.0/Assessment/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAssessment_WithEmptyGuid_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var response = await client.GetAsync($"/api/v1.0/Assessment/{Guid.Empty}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAssessment_DifferentAssessments_ReturnDifferentData()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var algebra = await client.GetFromJsonAsync<AssessmentSummary>(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}");
        var chemistry = await client.GetFromJsonAsync<AssessmentSummary>(
            $"/api/v1.0/Assessment/{_chemistryAssessmentId}");

        // Assert
        algebra.Should().NotBeEquivalentTo(chemistry);
        algebra!.Subject.Should().NotBe(chemistry!.Subject);
    }

    #endregion

    #region GetAssessmentSession Tests

    [Fact]
    public async Task GetAssessmentSession_WithValidId_ReturnsOkStatus()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var response = await client.GetAsync($"/api/v1.0/Assessment/{_algebraAssessmentId}/session");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAssessmentSession_WithValidId_ReturnsSessionDto()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var session = await client.GetFromJsonAsync<AssessmentSessionDto>(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session");

        // Assert
        session.Should().NotBeNull();
        session!.AssessmentId.Should().Be(_algebraAssessmentId);
        session.AssessmentTitle.Should().Be("Introduction to Algebra");
    }

    [Fact]
    public async Task GetAssessmentSession_ContainsQuestions()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var session = await client.GetFromJsonAsync<AssessmentSessionDto>(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session");

        // Assert
        session!.Questions.Should().NotBeNullOrEmpty();
        session.Questions.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAssessmentSession_AlgebraHasThreeQuestions()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var session = await client.GetFromJsonAsync<AssessmentSessionDto>(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session");

        // Assert
        session!.Questions.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAssessmentSession_QuestionsHaveValidIds()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var session = await client.GetFromJsonAsync<AssessmentSessionDto>(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session");

        // Assert
        session!.Questions.Should().AllSatisfy(q => q.Id.Should().NotBe(Guid.Empty));
    }

    [Fact]
    public async Task GetAssessmentSession_QuestionsHavePrompts()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var session = await client.GetFromJsonAsync<AssessmentSessionDto>(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session");

        // Assert
        session!.Questions.Should().AllSatisfy(q => q.Prompt.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task GetAssessmentSession_QuestionsHaveQuestionTypes()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var session = await client.GetFromJsonAsync<AssessmentSessionDto>(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session");

        // Assert
        session!.Questions.Should().AllSatisfy(q =>
            q.QuestionType.Should().BeDefined());
    }

    [Fact]
    public async Task GetAssessmentSession_ContainsTimingInformation()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var session = await client.GetFromJsonAsync<AssessmentSessionDto>(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session");

        // Assert
        session!.StartedAt.Should().NotBe(default);
        session.ExpiresAt.Should().NotBe(default);
        session.ExpiresAt.Should().BeAfter(session.StartedAt);
        session.EstimatedDurationMinutes.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAssessmentSession_ExpirationIsBasedOnEstimatedDuration()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var session = await client.GetFromJsonAsync<AssessmentSessionDto>(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session");

        // Assert
        var expectedExpiration = session!.StartedAt.AddMinutes(session.EstimatedDurationMinutes);
        session.ExpiresAt.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetAssessmentSession_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var invalidId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1.0/Assessment/{invalidId}/session");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAssessmentSession_Chemistry_HasCorrectQuestions()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var session = await client.GetFromJsonAsync<AssessmentSessionDto>(
            $"/api/v1.0/Assessment/{_chemistryAssessmentId}/session");

        // Assert
        session!.Questions.Should().HaveCount(2);
        session.Questions.Should().Contain(q => q.Prompt.Contains("SN1"));
    }

    [Fact]
    public async Task GetAssessmentSession_Physics_HasCodeSnippet()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var session = await client.GetFromJsonAsync<AssessmentSessionDto>(
            $"/api/v1.0/Assessment/{_physicsAssessmentId}/session");

        // Assert
        session!.Questions.Should().Contain(q => !string.IsNullOrEmpty(q.CodeSnippet));
        session.Questions.Should().Contain(q => q.CodeLanguage == "csharp");
    }

    [Fact]
    public async Task GetAssessmentSession_English_HasImageQuestion()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var session = await client.GetFromJsonAsync<AssessmentSessionDto>(
            $"/api/v1.0/Assessment/{_englishAssessmentId}/session");

        // Assert
        session!.Questions.Should().Contain(q => !string.IsNullOrEmpty(q.ImageUrl));
        session.Questions.Should().Contain(q => !string.IsNullOrEmpty(q.ImageAltText));
    }

    [Fact]
    public async Task GetAssessmentSession_Biology_HasMultipleQuestionTypes()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);

        // Act
        var session = await client.GetFromJsonAsync<AssessmentSessionDto>(
            $"/api/v1.0/Assessment/{_biologyAssessmentId}/session");

        // Assert
        session!.Questions.Should().HaveCount(3);
        var questionTypes = session.Questions.Select(q => q.QuestionType).Distinct().ToList();
        questionTypes.Should().HaveCountGreaterThan(1);
    }

    #endregion

    #region SaveSession Tests

    [Fact]
    public async Task SaveSession_WithValidRequest_ReturnsOkStatus()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var request = new SaveAssessmentSessionRequest
        {
            AssessmentId = _algebraAssessmentId,
            Answers = new Dictionary<Guid, QuestionAnswerDto>
            {
                [Guid.NewGuid()] = new QuestionAnswerDto
                {
                    QuestionId = Guid.NewGuid(),
                    SelectedOptions = new HashSet<string> { "A" },
                    LastModified = DateTimeOffset.UtcNow
                }
            },
            ReviewFlags = new HashSet<int>()
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session/save", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SaveSession_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var questionId = Guid.NewGuid();
        var request = new SaveAssessmentSessionRequest
        {
            AssessmentId = _algebraAssessmentId,
            Answers = new Dictionary<Guid, QuestionAnswerDto>
            {
                [questionId] = new QuestionAnswerDto
                {
                    QuestionId = Guid.NewGuid(),
                    SelectedOptions = new HashSet<string> { "B" },
                    LastModified = DateTimeOffset.UtcNow
                }
            },
            ReviewFlags = new HashSet<int>()
        };

        // Act
        var result = await client.PostAsJsonAsync(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session/save", request);
        var saveResponse = await result.Content.ReadFromJsonAsync<SaveAssessmentSessionResponse>();

        // Assert
        saveResponse.Should().NotBeNull();
        saveResponse!.Success.Should().BeTrue();
        saveResponse.AnswersSaved.Should().Be(1);
        saveResponse.SavedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SaveSession_WithMultipleAnswers_ReturnsSaveCorrectCount()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var request = new SaveAssessmentSessionRequest
        {
            AssessmentId = _algebraAssessmentId,
            Answers = new Dictionary<Guid, QuestionAnswerDto>
            {
                [Guid.NewGuid()] = new QuestionAnswerDto { QuestionId = Guid.NewGuid(), SelectedOptions = new HashSet<string> { "A" } },
                [Guid.NewGuid()] = new QuestionAnswerDto { QuestionId = Guid.NewGuid(), SelectedOptions = new HashSet<string> { "B" } },
                [Guid.NewGuid()] = new QuestionAnswerDto { QuestionId = Guid.NewGuid(), FreeResponse = "Answer text" }
            },
            ReviewFlags = new HashSet<int>()
        };

        // Act
        var result = await client.PostAsJsonAsync(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session/save", request);
        var saveResponse = await result.Content.ReadFromJsonAsync<SaveAssessmentSessionResponse>();

        // Assert
        saveResponse!.AnswersSaved.Should().Be(3);
    }

    [Fact]
    public async Task SaveSession_WithEmptyAnswers_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var request = new SaveAssessmentSessionRequest
        {
            AssessmentId = _algebraAssessmentId,
            Answers = new Dictionary<Guid, QuestionAnswerDto>(),
            ReviewFlags = new HashSet<int>()
        };

        // Act
        var result = await client.PostAsJsonAsync(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session/save", request);
        var saveResponse = await result.Content.ReadFromJsonAsync<SaveAssessmentSessionResponse>();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        saveResponse!.Success.Should().BeTrue();
        saveResponse.AnswersSaved.Should().Be(0);
    }

    [Fact]
    public async Task SaveSession_WithInvalidAssessmentId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var invalidId = Guid.NewGuid();
        var request = new SaveAssessmentSessionRequest
        {
            AssessmentId = invalidId,
            Answers = new Dictionary<Guid, QuestionAnswerDto>(),
            ReviewFlags = new HashSet<int>()
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/v1.0/Assessment/{invalidId}/session/save", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SaveSession_WithMismatchedAssessmentId_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var request = new SaveAssessmentSessionRequest
        {
            AssessmentId = _chemistryAssessmentId, // Different from route
            Answers = new Dictionary<Guid, QuestionAnswerDto>(),
            ReviewFlags = new HashSet<int>()
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session/save", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SaveSession_WithFreeResponseAnswer_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var request = new SaveAssessmentSessionRequest
        {
            AssessmentId = _algebraAssessmentId,
            Answers = new Dictionary<Guid, QuestionAnswerDto>
            {
                [Guid.NewGuid()] = new QuestionAnswerDto
                {
                    QuestionId = Guid.NewGuid(),
                    FreeResponse = "This is my essay answer with detailed explanation.",
                    LastModified = DateTimeOffset.UtcNow
                }
            },
            ReviewFlags = new HashSet<int>()
        };

        // Act
        var result = await client.PostAsJsonAsync(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session/save", request);
        var saveResponse = await result.Content.ReadFromJsonAsync<SaveAssessmentSessionResponse>();

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        saveResponse!.Success.Should().BeTrue();
    }

    #endregion

    #region SubmitSession Tests

    [Fact]
    public async Task SubmitSession_WithValidRequest_ReturnsOkStatus()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var request = new SubmitAssessmentSessionRequest
        {
            AssessmentId = _algebraAssessmentId,
            Answers = new Dictionary<Guid, QuestionAnswerDto>
            {
                [Guid.NewGuid()] = new QuestionAnswerDto { QuestionId = Guid.NewGuid(), SelectedOptions = new HashSet<string> { "A" } }
            },
            SubmittedAt = DateTimeOffset.UtcNow,
            TimeTakenSeconds = 1800
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session/submit", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SubmitSession_WithValidRequest_ReturnsSessionId()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var request = new SubmitAssessmentSessionRequest
        {
            AssessmentId = _algebraAssessmentId,
            Answers = new Dictionary<Guid, QuestionAnswerDto>(),
            SubmittedAt = DateTimeOffset.UtcNow,
            TimeTakenSeconds = 900
        };

        // Act
        var result = await client.PostAsJsonAsync(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session/submit", request);
        var submitResponse = await result.Content.ReadFromJsonAsync<SubmitAssessmentSessionResponse>();

        // Assert
        submitResponse.Should().NotBeNull();
        submitResponse!.SessionId.Should().NotBe(Guid.Empty);
        submitResponse.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitSession_ReturnsSubmittedTimestamp()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var submittedTime = DateTimeOffset.UtcNow;
        var request = new SubmitAssessmentSessionRequest
        {
            AssessmentId = _algebraAssessmentId,
            Answers = new Dictionary<Guid, QuestionAnswerDto>(),
            SubmittedAt = submittedTime,
            TimeTakenSeconds = 2400
        };

        // Act
        var result = await client.PostAsJsonAsync(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session/submit", request);
        var submitResponse = await result.Content.ReadFromJsonAsync<SubmitAssessmentSessionResponse>();

        // Assert
        submitResponse!.SubmittedAt.Should().Be(submittedTime);
    }

    [Fact]
    public async Task SubmitSession_ReturnsQuestionCounts()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var request = new SubmitAssessmentSessionRequest
        {
            AssessmentId = _algebraAssessmentId,
            Answers = new Dictionary<Guid, QuestionAnswerDto>
            {
                [Guid.NewGuid()] = new QuestionAnswerDto { QuestionId = Guid.NewGuid(), SelectedOptions = new HashSet<string> { "A" } },
                [Guid.NewGuid()] = new QuestionAnswerDto { QuestionId = Guid.NewGuid(), SelectedOptions = new HashSet<string> { "B" } }
            },
            SubmittedAt = DateTimeOffset.UtcNow,
            TimeTakenSeconds = 1500
        };

        // Act
        var result = await client.PostAsJsonAsync(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session/submit", request);
        var submitResponse = await result.Content.ReadFromJsonAsync<SubmitAssessmentSessionResponse>();

        // Assert
        submitResponse!.QuestionsAnswered.Should().Be(2);
        submitResponse.TotalQuestions.Should().Be(3); // Algebra has 3 questions
    }

    [Fact]
    public async Task SubmitSession_ReturnsSuccessMessage()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var request = new SubmitAssessmentSessionRequest
        {
            AssessmentId = _algebraAssessmentId,
            Answers = new Dictionary<Guid, QuestionAnswerDto>(),
            SubmittedAt = DateTimeOffset.UtcNow,
            TimeTakenSeconds = 600
        };

        // Act
        var result = await client.PostAsJsonAsync(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session/submit", request);
        var submitResponse = await result.Content.ReadFromJsonAsync<SubmitAssessmentSessionResponse>();

        // Assert
        submitResponse!.Message.Should().NotBeNullOrEmpty();
        submitResponse.Message.Should().Contain("submitted successfully");
    }

    [Fact]
    public async Task SubmitSession_WithInvalidAssessmentId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var invalidId = Guid.NewGuid();
        var request = new SubmitAssessmentSessionRequest
        {
            AssessmentId = invalidId,
            Answers = new Dictionary<Guid, QuestionAnswerDto>(),
            SubmittedAt = DateTimeOffset.UtcNow,
            TimeTakenSeconds = 1000
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/v1.0/Assessment/{invalidId}/session/submit", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitSession_WithMismatchedAssessmentId_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var request = new SubmitAssessmentSessionRequest
        {
            AssessmentId = _chemistryAssessmentId, // Different from route
            Answers = new Dictionary<Guid, QuestionAnswerDto>(),
            SubmittedAt = DateTimeOffset.UtcNow,
            TimeTakenSeconds = 1200
        };

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/v1.0/Assessment/{_algebraAssessmentId}/session/submit", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitSession_WithAllQuestionsAnswered_ReturnsCorrectCounts()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var request = new SubmitAssessmentSessionRequest
        {
            AssessmentId = _biologyAssessmentId,
            Answers = new Dictionary<Guid, QuestionAnswerDto>
            {
                [Guid.NewGuid()] = new QuestionAnswerDto { QuestionId = Guid.NewGuid(), SelectedOptions = new HashSet<string> { "C" } },
                [Guid.NewGuid()] = new QuestionAnswerDto { QuestionId = Guid.NewGuid(), SelectedOptions = new HashSet<string> { "false" } },
                [Guid.NewGuid()] = new QuestionAnswerDto { QuestionId = Guid.NewGuid(), FreeResponse = "phospholipids" }
            },
            SubmittedAt = DateTimeOffset.UtcNow,
            TimeTakenSeconds = 2400
        };

        // Act
        var result = await client.PostAsJsonAsync(
            $"/api/v1.0/Assessment/{_biologyAssessmentId}/session/submit", request);
        var submitResponse = await result.Content.ReadFromJsonAsync<SubmitAssessmentSessionResponse>();

        // Assert
        submitResponse!.QuestionsAnswered.Should().Be(3);
        submitResponse.TotalQuestions.Should().Be(3); // Biology has 3 questions
    }

    #endregion

    #region GetResults Tests

    [Fact]
    public async Task GetResults_WithValidSessionId_ReturnsOkStatus()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var sessionId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1.0/Assessment/results/{sessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetResults_ReturnsAssessmentResultsDto()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var sessionId = Guid.NewGuid();

        // Act
        var results = await client.GetFromJsonAsync<AssessmentResultsDto>(
            $"/api/v1.0/Assessment/results/{sessionId}");

        // Assert
        results.Should().NotBeNull();
        results!.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public async Task GetResults_ContainsScoreInformation()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var sessionId = Guid.NewGuid();

        // Act
        var results = await client.GetFromJsonAsync<AssessmentResultsDto>(
            $"/api/v1.0/Assessment/results/{sessionId}");

        // Assert
        results!.ScorePercentage.Should().BeGreaterThan(0);
        results.PointsEarned.Should().BeGreaterThan(0);
        results.TotalPoints.Should().BeGreaterThan(0);
        results.CorrectAnswers.Should().BeGreaterThan(0);
        results.TotalQuestions.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetResults_ContainsTimingInformation()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var sessionId = Guid.NewGuid();

        // Act
        var results = await client.GetFromJsonAsync<AssessmentResultsDto>(
            $"/api/v1.0/Assessment/results/{sessionId}");

        // Assert
        results!.TimeTakenSeconds.Should().BeGreaterThan(0);
        results.EstimatedDurationMinutes.Should().BeGreaterThan(0);
        results.SubmittedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task GetResults_ContainsPerformanceLevel()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var sessionId = Guid.NewGuid();

        // Act
        var results = await client.GetFromJsonAsync<AssessmentResultsDto>(
            $"/api/v1.0/Assessment/results/{sessionId}");

        // Assert
        results!.PerformanceLevel.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetResults_ContainsSubjectBreakdown()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var sessionId = Guid.NewGuid();

        // Act
        var results = await client.GetFromJsonAsync<AssessmentResultsDto>(
            $"/api/v1.0/Assessment/results/{sessionId}");

        // Assert
        results!.SubjectBreakdown.Should().NotBeNullOrEmpty();
        results.SubjectBreakdown.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetResults_SubjectBreakdown_HasValidData()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var sessionId = Guid.NewGuid();

        // Act
        var results = await client.GetFromJsonAsync<AssessmentResultsDto>(
            $"/api/v1.0/Assessment/results/{sessionId}");

        // Assert
        results!.SubjectBreakdown.Should().AllSatisfy(subject =>
        {
            subject.Subject.Should().NotBeNullOrEmpty();
            subject.QuestionCount.Should().BeGreaterThan(0);
            subject.ScorePercentage.Should().BeGreaterThanOrEqualTo(0);
            subject.PerformanceLevel.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task GetResults_ContainsStrengths()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var sessionId = Guid.NewGuid();

        // Act
        var results = await client.GetFromJsonAsync<AssessmentResultsDto>(
            $"/api/v1.0/Assessment/results/{sessionId}");

        // Assert
        results!.Strengths.Should().NotBeNullOrEmpty();
        results.Strengths.Should().AllSatisfy(s => s.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task GetResults_ContainsAreasForImprovement()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var sessionId = Guid.NewGuid();

        // Act
        var results = await client.GetFromJsonAsync<AssessmentResultsDto>(
            $"/api/v1.0/Assessment/results/{sessionId}");

        // Assert
        results!.AreasForImprovement.Should().NotBeNullOrEmpty();
        results.AreasForImprovement.Should().AllSatisfy(a => a.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task GetResults_ContainsRecommendations()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var sessionId = Guid.NewGuid();

        // Act
        var results = await client.GetFromJsonAsync<AssessmentResultsDto>(
            $"/api/v1.0/Assessment/results/{sessionId}");

        // Assert
        results!.Recommendations.Should().NotBeNullOrEmpty();
        results.Recommendations.Should().AllSatisfy(r => r.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task GetResults_HasReviewAnswersFlag()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var sessionId = Guid.NewGuid();

        // Act
        var results = await client.GetFromJsonAsync<AssessmentResultsDto>(
            $"/api/v1.0/Assessment/results/{sessionId}");

        // Assert
        results!.CanReviewAnswers.Should().BeTrue();
    }

    [Fact]
    public async Task GetResults_DifferentSessionIds_ReturnSameStructure()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var sessionId1 = Guid.NewGuid();
        var sessionId2 = Guid.NewGuid();

        // Act
        var results1 = await client.GetFromJsonAsync<AssessmentResultsDto>(
            $"/api/v1.0/Assessment/results/{sessionId1}");
        var results2 = await client.GetFromJsonAsync<AssessmentResultsDto>(
            $"/api/v1.0/Assessment/results/{sessionId2}");

        // Assert - Both should have same structure, just different session IDs
        results1!.SessionId.Should().NotBe(results2!.SessionId);
        results1.AssessmentId.Should().Be(results2.AssessmentId); // Mock returns same assessment
        results1.SubjectBreakdown.Count.Should().Be(results2.SubjectBreakdown.Count);
    }

    [Fact]
    public async Task GetResults_ReturnsJsonContentType()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(_studentToken);
        var sessionId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1.0/Assessment/results/{sessionId}");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    #endregion
}
