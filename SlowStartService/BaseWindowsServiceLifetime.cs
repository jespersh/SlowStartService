using Microsoft.Extensions.Options;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;

namespace SlowStartService
{
    [SupportedOSPlatform("windows")]
    public class BaseWindowsServiceLifetime : IHostLifetime, IDisposable
    {
        public IHostApplicationLifetime ApplicationLifetime { get; }
        private IHostEnvironment Environment { get; }
        private ILogger Logger { get; }
        private readonly EarlyWindowsService _earlyWindowsService;
        private readonly HostOptions _hostOptions;
        public bool NotifyRestartService { get; set; }
        public bool Stopping { get; set; }
        private bool _disposedValue;

        public BaseWindowsServiceLifetime(EarlyWindowsService earlyWindowsService, IHostEnvironment environment,
            IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions<HostOptions> optionsAccessor)
            : this(earlyWindowsService, environment, applicationLifetime, loggerFactory, optionsAccessor, Options.Create(new WindowsServiceLifetimeOptions()))
        {
        }

        public BaseWindowsServiceLifetime(EarlyWindowsService earlyWindowsService, IHostEnvironment environment, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, IOptions<HostOptions> optionsAccessor, IOptions<WindowsServiceLifetimeOptions> windowsServiceOptionsAccessor)
        {
            _earlyWindowsService = earlyWindowsService ?? throw new ArgumentNullException(nameof(earlyWindowsService));
            _earlyWindowsService.BaseWindowsServiceLifetime = this;
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            ApplicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
            Logger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }
            if (windowsServiceOptionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(windowsServiceOptionsAccessor));
            }
            _hostOptions = optionsAccessor.Value;
            _hostOptions.ShutdownTimeout = TimeSpan.FromSeconds(30);
            _earlyWindowsService.HostOptions = _hostOptions;
            NotifyRestartService = true;
        }

        public virtual async Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => _earlyWindowsService.DelayStart.TrySetCanceled());
            ApplicationLifetime.ApplicationStarted.Register(() =>
            {
                Logger.LogInformation("Application started. Hosting environment: {envName}; Content root path: {contentRoot}",
                    Environment.EnvironmentName, Environment.ContentRootPath);
            });
            ApplicationLifetime.ApplicationStopping.Register(() =>
            {
                Stopping = true;
                if (NotifyRestartService)
                {
                    System.Environment.ExitCode = -1;
                    _earlyWindowsService.ExitCode = -1;
                }
                Logger.LogInformation("Application is shutting down...");
            });
            ApplicationLifetime.ApplicationStopped.Register(() =>
            {
                _earlyWindowsService.DelayStop.Set();
            });

            await _earlyWindowsService.DelayStart.Task; // Wait for Service to indicate it has started
        }

        public virtual Task StopAsync(CancellationToken cancellationToken)
        {
            // Avoid deadlock where host waits for StopAsync before firing ApplicationStopped,
            // and Stop waits for ApplicationStopped.
            Task.Run(_earlyWindowsService.Stop, CancellationToken.None);
            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _earlyWindowsService.DelayStop.Set();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
