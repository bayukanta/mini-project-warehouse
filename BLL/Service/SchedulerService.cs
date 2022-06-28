using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BLL.Jobs;

namespace BLL.Service
{
    public class SchedulerService : ISchedulerService, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly IScheduler _scheduler;
        private readonly CancellationTokenSource _cancellationTokenSource;
        public SchedulerService(QuartzJobFactory quartzJobFactory,
            IConfiguration configuration,
            ILogger<SchedulerService> logger,
            IHostApplicationLifetime lifetime)
        {
            _configuration = configuration;
            _logger = logger;
            _cancellationTokenSource = new CancellationTokenSource();
            var containerName = _configuration.GetValue<string>("ContainerName");
            _logger.LogInformation($"Container {containerName} scheduler service starting...");
            var schedulerFactory = new StdSchedulerFactory();
            _scheduler = schedulerFactory
                .GetScheduler()
                .GetAwaiter()
                .GetResult();
            _scheduler.JobFactory = quartzJobFactory;

            Action callback = HandleShutdownScheduler();
            lifetime.ApplicationStopping.Register(callback);
        }
        private Action HandleShutdownScheduler()
        {
            return () =>
            {
                _logger.LogInformation("Terminating scheduler.");

                _cancellationTokenSource.Cancel();
                Stop();

                var jobKeys = _scheduler
                    .GetJobKeys(GroupMatcher<JobKey>.AnyGroup())
                    .GetAwaiter()
                    .GetResult();

                foreach (var jobKey in jobKeys)
                {
                    _scheduler.Interrupt(jobKey);
                }

                _logger.LogInformation("Waiting for scheduler's jobs to be completed.");


                _scheduler
                    .Shutdown(true)
                    .GetAwaiter()
                    .GetResult();

                _logger.LogInformation("Terminated scheduler.");
            };
        }

        public void Initialize()
        {
            var logTime = _configuration.GetValue<string>("Schedule:LogTimeJob");
            AddJob<DeliverToPaymentJob>(logTime);

        }


        public async void Start()
        {
            if (_scheduler.InStandbyMode)
            {
                await _scheduler.Start();
            }
        }

        public async void Stop()
        {
            _logger.LogDebug($"Stopping scheduler.");

            if (_scheduler.IsStarted)
            {
                await _scheduler.Standby();
            }
        }

        private void AddJob<T>(string cronExpression) where T : IJob
        {
            var jobName = typeof(T).Name;
            var groupName = jobName + "Group";
            var triggerName = jobName + "Trigger";

            var jobDetail = JobBuilder.Create<T>()
                .WithIdentity(jobName, groupName)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity(triggerName, groupName)
                .StartNow()
                .WithCronSchedule(cronExpression)
                .Build();

            _scheduler.ScheduleJob(jobDetail, trigger);
        }
        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual async void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    await _scheduler.Shutdown();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
