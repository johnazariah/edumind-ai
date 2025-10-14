using AcademicAssessment.Core.Common;
using AcademicAssessment.Core.Enums;
using AcademicAssessment.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AcademicAssessment.Web.Controllers;

/// <summary>
/// API endpoints for student analytics and performance tracking
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/students/{studentId:guid}/analytics")]
[Produces("application/json")]
public class StudentAnalyticsController : ControllerBase
{
    private readonly IStudentAnalyticsService _analyticsService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<StudentAnalyticsController> _logger;

    /// <summary>
    /// Initializes a new instance of the StudentAnalyticsController
    /// </summary>
    public StudentAnalyticsController(
        IStudentAnalyticsService analyticsService,
        ITenantContext tenantContext,
        ILogger<StudentAnalyticsController> logger)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets overall performance summary for a student across all subjects
    /// </summary>
    /// <param name="studentId">The unique identifier of the student</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Student performance summary including average score, mastery level, and current streak</returns>
    /// <response code="200">Returns the student performance summary</response>
    /// <response code="403">Forbidden - User doesn't have access to this student's data</response>
    /// <response code="404">Not found - Student not found or no assessment data available</response>
    [HttpGet("performance-summary")]
    [ProducesResponseType(typeof(StudentPerformanceSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPerformanceSummary(
        [FromRoute] Guid studentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "User {UserId} requesting performance summary for student {StudentId}",
            _tenantContext.UserId,
            studentId);

        // Authorization: Check if user has access to this student's data
        if (!await CanAccessStudentDataAsync(studentId))
        {
            _logger.LogWarning(
                "User {UserId} with role {Role} attempted unauthorized access to student {StudentId}",
                _tenantContext.UserId,
                _tenantContext.Role,
                studentId);
            return Forbid();
        }

        var result = await _analyticsService.GetStudentPerformanceSummaryAsync(
            studentId,
            cancellationToken);

        if (result is Result<StudentPerformanceSummary>.Success success)
        {
            return Ok(success.Value);
        }

        var failure = (Result<StudentPerformanceSummary>.Failure)result;
        _logger.LogError(
            "Failed to get performance summary for student {StudentId}: {Error}",
            studentId,
            failure.Error);
        return NotFound(new { error = failure.Error.Message });
    }

    /// <summary>
    /// Gets subject-specific performance analytics for a student
    /// </summary>
    /// <param name="studentId">The unique identifier of the student</param>
    /// <param name="subject">Optional subject filter (if null, returns aggregated data across all subjects)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed subject performance including scores, accuracy, time metrics, and topic analysis</returns>
    /// <response code="200">Returns the subject performance data</response>
    /// <response code="400">Bad request - Invalid subject parameter</response>
    /// <response code="403">Forbidden - User doesn't have access to this student's data</response>
    /// <response code="404">Not found - No assessment data available for the specified subject</response>
    [HttpGet("subject-performance")]
    [ProducesResponseType(typeof(SubjectPerformance), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubjectPerformance(
        [FromRoute] Guid studentId,
        [FromQuery] Subject? subject = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "User {UserId} requesting subject performance for student {StudentId}, subject: {Subject}",
            _tenantContext.UserId,
            studentId,
            subject?.ToString() ?? "All");

        if (!await CanAccessStudentDataAsync(studentId))
        {
            return Forbid();
        }

        // If subject is not provided, default to Mathematics for backward compatibility
        // In the future, we might want to aggregate all subjects or return an error
        var subjectToQuery = subject ?? Subject.Mathematics;

        var result = await _analyticsService.GetSubjectPerformanceAsync(
            studentId,
            subjectToQuery,
            cancellationToken);

        return result.Match<SubjectPerformance, IActionResult>(
            onSuccess: performance => Ok(performance),
            onFailure: error =>
            {
                _logger.LogError(
                    "Failed to get subject performance for student {StudentId}, subject {Subject}: {Error}",
                    studentId,
                    subjectToQuery,
                    error);
                return NotFound(new { error = error.Message });
            });
    }

    /// <summary>
    /// Gets learning objective mastery data for a student
    /// </summary>
    /// <param name="studentId">The unique identifier of the student</param>
    /// <param name="subject">Optional subject filter (if null, returns data for all subjects)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of learning objectives with mastery levels and statuses</returns>
    /// <response code="200">Returns the learning objective mastery data</response>
    /// <response code="403">Forbidden - User doesn't have access to this student's data</response>
    /// <response code="404">Not found - No data available for the specified criteria</response>
    [HttpGet("learning-objectives")]
    [ProducesResponseType(typeof(IReadOnlyList<LearningObjectiveMastery>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLearningObjectiveMastery(
        [FromRoute] Guid studentId,
        [FromQuery] Subject? subject = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "User {UserId} requesting learning objective mastery for student {StudentId}, subject: {Subject}",
            _tenantContext.UserId,
            studentId,
            subject?.ToString() ?? "All");

        if (!await CanAccessStudentDataAsync(studentId))
        {
            return Forbid();
        }

        var result = await _analyticsService.GetLearningObjectiveMasteryAsync(
            studentId,
            subject,
            cancellationToken);

        return result.Match<IReadOnlyList<LearningObjectiveMastery>, IActionResult>(
            onSuccess: mastery => Ok(mastery),
            onFailure: error =>
            {
                _logger.LogError(
                    "Failed to get learning objective mastery for student {StudentId}, subject {Subject}: {Error}",
                    studentId,
                    subject?.ToString() ?? "All",
                    error);
                return NotFound(new { error = error.Message });
            });
    }

    /// <summary>
    /// Gets IRT-based ability estimates for a student across all subjects
    /// </summary>
    /// <param name="studentId">The unique identifier of the student</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of subjects and their corresponding ability estimates (-3 to +3 scale)</returns>
    /// <response code="200">Returns the ability estimates</response>
    /// <response code="403">Forbidden - User doesn't have access to this student's data</response>
    /// <response code="404">Not found - Insufficient data to calculate ability estimates</response>
    [HttpGet("ability-estimates")]
    [ProducesResponseType(typeof(IReadOnlyDictionary<Subject, double>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAbilityEstimates(
        [FromRoute] Guid studentId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "User {UserId} requesting ability estimates for student {StudentId}",
            _tenantContext.UserId,
            studentId);

        if (!await CanAccessStudentDataAsync(studentId))
        {
            return Forbid();
        }

        var result = await _analyticsService.GetAbilityEstimatesAsync(
            studentId,
            cancellationToken);

        return result.Match<IReadOnlyDictionary<Subject, double>, IActionResult>(
            onSuccess: estimates => Ok(estimates),
            onFailure: error =>
            {
                _logger.LogError(
                    "Failed to get ability estimates for student {StudentId}: {Error}",
                    studentId,
                    error);
                return NotFound(new { error = error.Message });
            });
    }

    /// <summary>
    /// Gets priority-ordered list of areas where the student needs improvement
    /// </summary>
    /// <param name="studentId">The unique identifier of the student</param>
    /// <param name="topN">Number of top improvement areas to return (default: 5, max: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of improvement areas with priority levels and recommended actions</returns>
    /// <response code="200">Returns the improvement areas</response>
    /// <response code="400">Bad request - Invalid topN parameter</response>
    /// <response code="403">Forbidden - User doesn't have access to this student's data</response>
    /// <response code="404">Not found - No data available to identify improvement areas</response>
    [HttpGet("improvement-areas")]
    [ProducesResponseType(typeof(IReadOnlyList<ImprovementArea>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImprovementAreas(
        [FromRoute] Guid studentId,
        [FromQuery] int topN = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "User {UserId} requesting top {TopN} improvement areas for student {StudentId}",
            _tenantContext.UserId,
            topN,
            studentId);

        // Validate topN parameter
        if (topN < 1 || topN > 20)
        {
            return BadRequest(new { error = "topN must be between 1 and 20" });
        }

        if (!await CanAccessStudentDataAsync(studentId))
        {
            return Forbid();
        }

        var result = await _analyticsService.GetImprovementAreasAsync(
            studentId,
            topN,
            cancellationToken);

        return result.Match<IReadOnlyList<ImprovementArea>, IActionResult>(
            onSuccess: areas => Ok(areas),
            onFailure: error =>
            {
                _logger.LogError(
                    "Failed to get improvement areas for student {StudentId}: {Error}",
                    studentId,
                    error);
                return NotFound(new { error = error.Message });
            });
    }

    /// <summary>
    /// Gets time-series progress data for a student
    /// </summary>
    /// <param name="studentId">The unique identifier of the student</param>
    /// <param name="startDate">Optional start date for the timeline (ISO 8601 format)</param>
    /// <param name="endDate">Optional end date for the timeline (ISO 8601 format)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Progress timeline with data points and growth rate calculations</returns>
    /// <response code="200">Returns the progress timeline</response>
    /// <response code="400">Bad request - Invalid date parameters</response>
    /// <response code="403">Forbidden - User doesn't have access to this student's data</response>
    /// <response code="404">Not found - No assessment data available for the specified date range</response>
    [HttpGet("progress-timeline")]
    [ProducesResponseType(typeof(ProgressTimeline), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProgressTimeline(
        [FromRoute] Guid studentId,
        [FromQuery] DateTimeOffset? startDate = null,
        [FromQuery] DateTimeOffset? endDate = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "User {UserId} requesting progress timeline for student {StudentId}, date range: {StartDate} to {EndDate}",
            _tenantContext.UserId,
            studentId,
            startDate?.ToString("yyyy-MM-dd") ?? "beginning",
            endDate?.ToString("yyyy-MM-dd") ?? "now");

        // Validate date range
        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
        {
            return BadRequest(new { error = "startDate cannot be after endDate" });
        }

        if (!await CanAccessStudentDataAsync(studentId))
        {
            return Forbid();
        }

        var result = await _analyticsService.GetProgressTimelineAsync(
            studentId,
            startDate,
            endDate,
            cancellationToken);

        return result.Match<ProgressTimeline, IActionResult>(
            onSuccess: timeline => Ok(timeline),
            onFailure: error =>
            {
                _logger.LogError(
                    "Failed to get progress timeline for student {StudentId}: {Error}",
                    studentId,
                    error);
                return NotFound(new { error = error.Message });
            });
    }

    /// <summary>
    /// Gets privacy-preserving peer comparison data for a student
    /// </summary>
    /// <param name="studentId">The unique identifier of the student</param>
    /// <param name="gradeLevel">Grade level for peer comparison (required)</param>
    /// <param name="subject">Optional subject filter for comparison</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Peer comparison with k-anonymity protection (minimum 5 peers required)</returns>
    /// <response code="200">Returns the peer comparison data</response>
    /// <response code="400">Bad request - Missing or invalid grade level</response>
    /// <response code="403">Forbidden - User doesn't have access to this student's data</response>
    /// <response code="404">Not found - Insufficient peer data available (k-anonymity threshold not met)</response>
    [HttpGet("peer-comparison")]
    [ProducesResponseType(typeof(PeerComparison), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPeerComparison(
        [FromRoute] Guid studentId,
        [FromQuery] GradeLevel? gradeLevel = null,
        [FromQuery] Subject? subject = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "User {UserId} requesting peer comparison for student {StudentId}, grade {GradeLevel}, subject: {Subject}",
            _tenantContext.UserId,
            studentId,
            gradeLevel?.ToString() ?? "All",
            subject?.ToString() ?? "All");

        if (!await CanAccessStudentDataAsync(studentId))
        {
            return Forbid();
        }

        var result = await _analyticsService.GetPeerComparisonAsync(
            studentId,
            gradeLevel,
            subject,
            cancellationToken);

        return result.Match<PeerComparison, IActionResult>(
            onSuccess: comparison => Ok(comparison),
            onFailure: error =>
            {
                _logger.LogError(
                    "Failed to get peer comparison for student {StudentId}, grade {GradeLevel}: {Error}",
                    studentId,
                    gradeLevel,
                    error);
                return NotFound(new { error = error.Message });
            });
    }

    /// <summary>
    /// Determines if the current user has access to the specified student's data
    /// </summary>
    /// <remarks>
    /// Authorization rules:
    /// - Students can access their own data
    /// - Teachers can access data for students in their classes
    /// - School admins can access all students in their school
    /// - Course admins and system admins have broader access
    /// </remarks>
    private async Task<bool> CanAccessStudentDataAsync(Guid studentId)
    {
        // TODO: Implement proper authorization logic when authentication is added
        // For now, allow all access in development
        // 
        // Production implementation should:
        // 1. Check if user is the student (UserId == studentId)
        // 2. Check if user is a teacher with student in their classes
        // 3. Check if user is school admin with student in their school
        // 4. Check if user is course admin or system admin

        return await Task.FromResult(true); // Temporary - allow all access for development
    }
}
