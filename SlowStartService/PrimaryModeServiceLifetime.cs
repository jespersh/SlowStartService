using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Options;

namespace SlowStartService
{
    public class PrimaryModeServiceLifetime : BaseWindowsServiceLifetime
    {
        private readonly ILogger<PrimaryModeServiceLifetime> _logger;

        public PrimaryModeServiceLifetime(EarlyWindowsService earlyWindowsService,
            IHostEnvironment environment,
            IHostApplicationLifetime applicationLifetime,
            ILoggerFactory loggerFactory,
            IOptions<HostOptions> optionsAccessor,
            ILogger<PrimaryModeServiceLifetime> logger)
            : base(earlyWindowsService, environment, applicationLifetime, loggerFactory, optionsAccessor)
        {
            _logger = logger;
        }



        public override Task StopAsync(CancellationToken cancellationToken)
        {
            if (WindowsServiceHelpers.IsWindowsService())
                return base.StopAsync(cancellationToken);
            return Task.CompletedTask;
        }

        public override async Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            if (WindowsServiceHelpers.IsWindowsService())
                await base.WaitForStartAsync(cancellationToken);

            _logger.LogInformation($"Starting as {nameof(PrimaryModeServiceLifetime)}");

            // add code here
        }
    }
}
