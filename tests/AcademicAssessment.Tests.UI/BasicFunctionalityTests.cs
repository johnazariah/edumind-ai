using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace AcademicAssessment.Tests.UI;

public class BasicFunctionalityTests
{
    [Fact]
    public async Task WebApi_ShouldReturnHealthy()
    {
        using var client = new HttpClient();

        var response = await client.GetAsync("http://localhost:5103/health");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }

    [Fact]
    public async Task WebApi_ShouldReturnAssessments()
    {
        using var client = new HttpClient();

        var response = await client.GetAsync("http://localhost:5103/api/v1/assessment");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
        content.Should().Contain("title");
    }

    [Fact]
    public async Task WebApi_ShouldReturnSpecificAssessment()
    {
        using var client = new HttpClient();

        var response = await client.GetAsync("http://localhost:5103/api/v1/assessment/6c8d46f0-9cf6-4ba6-8361-5a6dc7bb9e38");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Introduction to Algebra");
    }

    [Fact]
    public async Task StudentApp_HomeShouldLoad()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        var response = await client.GetAsync("http://localhost:5049/");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("EduMind.AI");
        content.Should().NotContain("Hello, world!");
    }

    [Fact]
    public async Task StudentApp_AssessmentsShouldLoad()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        var response = await client.GetAsync("http://localhost:5049/assessments");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        // Should NOT contain error messages
        content.Should().NotContain("unhandled error");
        content.Should().NotContain("alert-danger");

        // Should contain assessment-related content
        content.Should().Contain("Assessments");
    }

    [Fact]
    public async Task StudentApp_AssessmentDetailShouldNotError()
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(15);

        var response = await client.GetAsync("http://localhost:5049/assessment/6c8d46f0-9cf6-4ba6-8361-5a6dc7bb9e38");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        // The critical test - this should NOT contain error messages
        content.Should().NotContain("We couldn&#x27;t load this assessment");
        content.Should().NotContain("unhandled error has occurred");
        content.Should().NotContain("alert-danger");

        // And should contain assessment content
        content.Should().Contain("Introduction to Algebra");
    }
}