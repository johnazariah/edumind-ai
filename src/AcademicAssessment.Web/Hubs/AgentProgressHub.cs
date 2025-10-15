using Microsoft.AspNetCore.SignalR;

namespace AcademicAssessment.Web.Hubs;

/// <summary>
/// SignalR hub for broadcasting real-time agent progress updates.
/// Clients (dashboards, student apps) can connect to receive live updates
/// about assessment generation, evaluation, and orchestration progress.
/// </summary>
public class AgentProgressHub : Hub
{
    private readonly ILogger<AgentProgressHub> _logger;

    public AgentProgressHub(ILogger<AgentProgressHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Join a student-specific group to receive updates for that student's assessments.
    /// </summary>
    /// <param name="studentId">Student identifier</param>
    public async Task JoinStudentGroup(string studentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"student-{studentId}");
        _logger.LogInformation("Client {ConnectionId} joined student group {StudentId}",
            Context.ConnectionId, studentId);
    }

    /// <summary>
    /// Leave a student group.
    /// </summary>
    /// <param name="studentId">Student identifier</param>
    public async Task LeaveStudentGroup(string studentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"student-{studentId}");
        _logger.LogInformation("Client {ConnectionId} left student group {StudentId}",
            Context.ConnectionId, studentId);
    }

    /// <summary>
    /// Join a teacher group to receive updates for all students in their classes.
    /// </summary>
    /// <param name="teacherId">Teacher identifier</param>
    public async Task JoinTeacherGroup(string teacherId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"teacher-{teacherId}");
        _logger.LogInformation("Client {ConnectionId} joined teacher group {TeacherId}",
            Context.ConnectionId, teacherId);
    }

    /// <summary>
    /// Join a school-wide group for administrators.
    /// </summary>
    /// <param name="schoolId">School identifier</param>
    public async Task JoinSchoolGroup(string schoolId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"school-{schoolId}");
        _logger.LogInformation("Client {ConnectionId} joined school group {SchoolId}",
            Context.ConnectionId, schoolId);
    }

    /// <summary>
    /// Broadcast progress update to all connected clients.
    /// Called by agents via hub connection.
    /// </summary>
    /// <param name="progressData">Progress data object</param>
    public async Task AgentProgress(object progressData)
    {
        await Clients.All.SendAsync("AgentProgress", progressData);
        _logger.LogDebug("Broadcasted agent progress to all clients");
    }

    /// <summary>
    /// Broadcast progress to a specific student's group.
    /// </summary>
    /// <param name="studentId">Student identifier</param>
    /// <param name="progressData">Progress data object</param>
    public async Task StudentProgress(string studentId, object progressData)
    {
        await Clients.Group($"student-{studentId}").SendAsync("StudentProgress", progressData);
        _logger.LogDebug("Broadcasted student progress to student-{StudentId} group", studentId);
    }

    /// <summary>
    /// Broadcast notification about assessment completion.
    /// </summary>
    /// <param name="studentId">Student identifier</param>
    /// <param name="assessmentData">Assessment data</param>
    public async Task AssessmentReady(string studentId, object assessmentData)
    {
        await Clients.Group($"student-{studentId}").SendAsync("AssessmentReady", assessmentData);
        _logger.LogInformation("Notified student-{StudentId} that assessment is ready", studentId);
    }

    /// <summary>
    /// Broadcast notification about evaluation completion.
    /// </summary>
    /// <param name="studentId">Student identifier</param>
    /// <param name="evaluationData">Evaluation results</param>
    public async Task EvaluationComplete(string studentId, object evaluationData)
    {
        await Clients.Group($"student-{studentId}").SendAsync("EvaluationComplete", evaluationData);
        _logger.LogInformation("Notified student-{StudentId} that evaluation is complete", studentId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId} from {UserAgent}",
            Context.ConnectionId,
            Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString() ?? "Unknown");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
