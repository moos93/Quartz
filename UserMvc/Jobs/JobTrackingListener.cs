using Quartz;

namespace UserMvc.Jobs
{
    public class JobTrackingListener : IJobListener
    {
        private readonly ILogger<JobTrackingListener> _logger;
        public JobTrackingListener(ILogger<JobTrackingListener> logger)
        {
            _logger = logger;
        }
        public string Name => "JobTrackingListener";


        public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Job {context.JobDetail.Key.Name} is starting...");
            return Task.CompletedTask;
        }
        public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Job {context.JobDetail.Key.Name} was vetoed.");
            return Task.CompletedTask;
        }

        public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken cancellationToken = default)
        {

            if (jobException != null)
            {
                _logger.LogError(jobException, $"Job {context.JobDetail.Key.Name} failed with error: {jobException.Message}");
            }
            else
            {
                _logger.LogInformation($"Job {context.JobDetail.Key.Name} completed successfully.");
            }
            return Task.CompletedTask;
        }
    }
}
