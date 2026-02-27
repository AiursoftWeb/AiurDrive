using Aiursoft.AiurDrive.Models.BackgroundJobs;

namespace Aiursoft.AiurDrive.Services.BackgroundJobs;

/// <summary>
/// Background service that processes jobs from the CanonQueue.
/// </summary>
public class QueueWorkerService(
    BackgroundJobQueue backgroundJobQueue,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<QueueWorkerService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Queue Worker Service is starting");

        using var processTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        using var cleanupTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        var processTask = ProcessJobsLoop(stoppingToken, processTimer);
        var cleanupTask = CleanupJobsLoop(stoppingToken, cleanupTimer);

        await Task.WhenAll(processTask, cleanupTask);
        
        logger.LogInformation("Queue Worker Service is stopping");
    }

    private async Task ProcessJobsLoop(CancellationToken stoppingToken, PeriodicTimer timer)
    {
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var queues = backgroundJobQueue.GetQueuesWithPendingJobs().ToList();

                foreach (var queueName in queues)
                {
                    // Try to get next job for this queue (will return null if queue is already processing)
                    var job = backgroundJobQueue.TryDequeueNextJob(queueName);
                    if (job != null)
                    {
                        // Process job asynchronously without blocking the timer
                        _ = Task.Run(async () => await ProcessJobAsync(job), stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while processing jobs loop");
            }
        }
    }

    private async Task CleanupJobsLoop(CancellationToken stoppingToken, PeriodicTimer timer)
    {
        // Initial delay to avoid running immediately at startup if not desired, 
        // but here we wait for the first tick which is 5 minutes.
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                logger.LogInformation("Cleaning up old jobs");
                backgroundJobQueue.CleanupOldJobs(TimeSpan.FromHours(1));
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cleaning up old jobs");
            }
        }
    }

    private async Task ProcessJobAsync(JobInfo job)
    {
        try
        {
            logger.LogInformation("Processing job {JobId} ({JobName}) from queue {QueueName}",
                job.JobId, job.JobName, job.QueueName);

            // Create a scope for dependency injection
            using var scope = serviceScopeFactory.CreateScope();

            // Resolve the service
            var service = scope.ServiceProvider.GetRequiredService(job.ServiceType);

            // Execute the job
            await job.JobAction(service);

            // Mark as success
            backgroundJobQueue.CompleteJob(job.JobId, true);

            logger.LogInformation("Job {JobId} ({JobName}) completed successfully",
                job.JobId, job.JobName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {JobId} ({JobName}) failed with error: {Error}",
                job.JobId, job.JobName, ex.Message);

            // Mark as failed with error message
            backgroundJobQueue.CompleteJob(job.JobId, false, ex.ToString());
        }
    }
}
