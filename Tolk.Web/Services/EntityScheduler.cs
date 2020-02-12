using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Services
{
    public class EntityScheduler
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<EntityScheduler> _logger;
        private readonly ISwedishClock _clock;
        private readonly TolkOptions _options;

        private readonly int timeToRun;

        private DateTimeOffset nextDailyRunTime;

        private bool nextRunIsNotifications = true;

        private const int timeDelayContinousJobs = 5000;
        private const int allotedTimeAllTasks = 120000;

        public EntityScheduler(IServiceProvider services, ILogger<EntityScheduler> logger, ISwedishClock clock, IOptions<TolkOptions> options)
        {
            _services = services;
            _logger = logger;
            _clock = clock;
            _options = options?.Value;

            timeToRun = _options.HourToRunDailyJobs;
            if (_clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            DateTimeOffset now = _clock.SwedenNow;
            now -= now.TimeOfDay;

            nextDailyRunTime = now - now.TimeOfDay;
            nextDailyRunTime = nextDailyRunTime.AddHours(timeToRun);

            if (_clock.SwedenNow.Hour > timeToRun)
            {
                // Next remind is tomorrow
                nextDailyRunTime = nextDailyRunTime.AddDays(1);
            }

            _logger.LogInformation("Created EntityScheduler instance");
        }

        public void Init()
        {
            Task.Run(() => Run(true));

            _logger.LogInformation("EntityScheduler initialized");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not stop, any errors must be swollowed")]
        private async void Run(bool isInit = false)
        {
            _logger.LogTrace("EntityScheduler waking up.");

            if ((nextDailyRunTime - _clock.SwedenNow).TotalHours > 25 || nextDailyRunTime.Hour != timeToRun)
            {
                _logger.LogWarning("nextDailyRunTime set to invalid time, was {0}", nextDailyRunTime);
                DateTimeOffset now = _clock.SwedenNow;
                now -= now.TimeOfDay;
                nextDailyRunTime = now - now.TimeOfDay;
                nextDailyRunTime = nextDailyRunTime.AddHours(timeToRun);

                if (_clock.SwedenNow.Hour > timeToRun)
                {
                    // Next remind is tomorrow
                    nextDailyRunTime = nextDailyRunTime.AddDays(1);
                }
            }

            if (isInit)
            {
                using (TolkDbContext context = _options.GetContext())
                {
                    await context.OutboundEmails.Where(e => e.IsHandling == true)
                        .Select(c => c).ForEachAsync(c => c.IsHandling = false);
                    await context.OutboundWebHookCalls.Where(e => e.IsHandling == true)
                        .Select(c => c).ForEachAsync(c => c.IsHandling = false);
                    await context.SaveChangesAsync();
                }
            }

            try
            {
                if (nextRunIsNotifications)
                {
                    //Separate these, to get a better parallellism for the notifications
                    // They fail to run together with the other Continous jobs , due to recurring deadlocks around the email table...
                    List<Task> tasksToRunNotifications = new List<Task>
                    {
                        Task.Factory.StartNew(() => _services.GetRequiredService<EmailService>().SendEmails(), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current),
                        Task.Factory.StartNew(() => _services.GetRequiredService<WebHookService>().CallWebHooks(), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Current)
                    };
                    await Task.Factory.ContinueWhenAny(tasksToRunNotifications.ToArray(), r => { });
            }
                else
                {
                    //would like to have a timer here, to make it possible to get tighter runs if the last run ran for longer than 10 seconds or somethng...
                    using (var serviceScope = _services.CreateScope())
                    {
                        Task[] tasksToRun;

                        if (_clock.SwedenNow > nextDailyRunTime)
                        {
                            nextDailyRunTime = nextDailyRunTime.AddDays(1);
                            nextDailyRunTime -= nextDailyRunTime.TimeOfDay;
                            nextDailyRunTime = nextDailyRunTime.AddHours(timeToRun);
                            _logger.LogTrace("Running DailyRunTime, next run on {0}", nextDailyRunTime);

                            tasksToRun = new Task[]
                            {
                            RunDailyJobs(serviceScope.ServiceProvider),
                            };
                        }
                        else
                        {
                            tasksToRun = new Task[]
                            {
                                RunContinousJobs(serviceScope.ServiceProvider),
                            };
                        }
                        if (!Task.WaitAll(tasksToRun, allotedTimeAllTasks))
                        {
                            throw new InvalidOperationException($"All tasks instances didn't complete execution within the allotted time: {allotedTimeAllTasks / 1000} seconds");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Entity Scheduler failed ({message}).", ex.Message);
                _ = _services.GetRequiredService<EmailService>().SendErrorEmail(nameof(EntityScheduler), nameof(Run), ex);
            }
            finally
            {
                nextRunIsNotifications = !nextRunIsNotifications;
                if (_services.GetRequiredService<ITolkBaseOptions>().RunEntityScheduler)
                {
                    await Task.Delay(timeDelayContinousJobs).ContinueWith(t => Run(), TaskScheduler.Default);
                }
            }

            _logger.LogTrace($"EntityScheduler done, scheduled to wake up in {timeDelayContinousJobs / 1000} seconds again");
        }

        private async Task RunDailyJobs(IServiceProvider provider)
        {
            _logger.LogInformation($"Starting {nameof(RunDailyJobs)}");
            await provider.GetRequiredService<OrderService>().CleanTempAttachments();
            await provider.GetRequiredService<RequestService>().SendEmailReminders();
            await provider.GetRequiredService<VerificationService>().HandleTellusVerifications(true);
            await provider.GetRequiredService<OrderService>().HandleExpiredComplaints();
            _logger.LogInformation($"Completed {nameof(RunDailyJobs)}");
        }

        private async Task RunContinousJobs(IServiceProvider provider)
        {
            _logger.LogInformation($"Starting {nameof(RunContinousJobs)}");
            await provider.GetRequiredService<OrderService>().HandleAllScheduledTasks();
            await provider.GetRequiredService<RequestService>().DeleteRequestViews();
            await provider.GetRequiredService<RequestService>().DeleteRequestGroupViews();
            await provider.GetRequiredService<ErrorNotificationService>().CheckForFailuresToReport();
            _logger.LogInformation($"Completed {nameof(RunContinousJobs)}");
        }
    }
}
