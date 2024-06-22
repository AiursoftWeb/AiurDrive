using Aiursoft.CSTools.Tools;
using Aiursoft.Scanner.Abstractions;

namespace Aiursoft.AiurDrive.Services
{
    public class TimedCleaner : IHostedService, IDisposable, ISingletonDependency
    {
        private readonly ILogger _logger;
        private Timer? _timer;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _env;

        public TimedCleaner(
            ILogger<TimedCleaner> logger,
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _env = env;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_env.IsDevelopment() || !EntryExtends.IsProgramEntry())
            {
                _logger.LogInformation("Skip cleaner in development environment");
                return Task.CompletedTask;
            }
            _logger.LogInformation("Timed Background Service is starting");
            _timer = new Timer(DoWork, null, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(10));
            return Task.CompletedTask;
        }

        private async void DoWork(object? state)
        {
            try
            {
                _logger.LogInformation("Cleaner task started!");
                using var scope = _scopeFactory.CreateScope();
                await Task.Delay(0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is stopping");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}