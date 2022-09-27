using System.Runtime.Versioning;
using System.ServiceProcess;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace SlowStartService
{
    [SupportedOSPlatform("windows")]
    public class EarlyWindowsService : ServiceBase
    {
        public TaskCompletionSource<object> DelayStart { get; }
        public ManualResetEventSlim DelayStop { get; }
        public BaseWindowsServiceLifetime? BaseWindowsServiceLifetime { get; set; }
        public HostOptions? HostOptions { get; internal set; }

        public EarlyWindowsService()
        {
            CanShutdown = true;
            DelayStart = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            DelayStop = new ManualResetEventSlim();
        }

        public void Start()
        {
            if (!WindowsServiceHelpers.IsWindowsService())
                return;

            Thread thread = new Thread(Run);
            thread.IsBackground = true;
            thread.Start(); // Otherwise this would block and prevent IHost.StartAsync from finishing.
        }

        private void Run()
        {
            try
            {
                Run(this); // This blocks until the service is stopped.
                DelayStart.TrySetException(new InvalidOperationException("Stopped without starting"));
            }
            catch (Exception ex)
            {
                DelayStart.TrySetException(ex);
            }
        }


        // Called by base.Run when the service is ready to start.
        protected override void OnStart(string[] args)
        {
            DelayStart.TrySetResult(null);
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            if (BaseWindowsServiceLifetime != null)
            {
                BaseWindowsServiceLifetime.NotifyRestartService = false;
                BaseWindowsServiceLifetime.Stopping = true;
                BaseWindowsServiceLifetime.ApplicationLifetime.StopApplication();
            }
            // Wait for the host to shutdown before marking service as stopped.
            if (HostOptions != null) DelayStop.Wait(HostOptions.ShutdownTimeout);
            if (ExitCode != 0)
            {
                // Notifies Windows SCM about we're "crashing" and need recovery actions
                System.Environment.Exit(-1);
            }
            else
            {
                base.OnStop();
            }
        }

        protected override void OnShutdown()
        {
            if (BaseWindowsServiceLifetime != null)
            {
                BaseWindowsServiceLifetime.NotifyRestartService = false;
                BaseWindowsServiceLifetime.Stopping = true;
                BaseWindowsServiceLifetime.ApplicationLifetime.StopApplication();
            }
            // Wait for the host to shutdown before marking service as stopped.
            if (HostOptions != null) DelayStop.Wait(HostOptions.ShutdownTimeout);
            base.OnShutdown();
        }
    }
}
