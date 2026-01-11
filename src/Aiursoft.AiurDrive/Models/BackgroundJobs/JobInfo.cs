namespace Aiursoft.AiurDrive.Models.BackgroundJobs;

/// <summary>
/// Represents the status of a background job.
/// </summary>
public enum JobStatus
{
    Pending,
    Processing,
    Success,
    Failed,
    Cancelled
}

/// <summary>
/// Represents a background job in the queue system.
/// </summary>
public class JobInfo
{
    public Guid JobId { get; init; } = Guid.NewGuid();

    public required string QueueName { get; init; }

    public required string JobName { get; init; }

    public JobStatus Status { get; set; } = JobStatus.Pending;

    public DateTime QueuedAt { get; init; } = DateTime.UtcNow;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public required Type ServiceType { get; init; }

    public required Func<object, Task> JobAction { get; init; }
}
