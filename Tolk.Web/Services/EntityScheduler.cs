using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Services
{
    public class EntityScheduler
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<EntityScheduler> _logger;
        private readonly ISwedishClock _clock;

        private DateTimeOffset nextDailyRunTime;

        private const int timeToRun = 5;
        private const int timeDelayContinousJobs = 15000;
        private const int allotedTimeAllTasks = 120000;

        public EntityScheduler(IServiceProvider services, ILogger<EntityScheduler> logger, ISwedishClock clock)
        {
            _services = services;
            _logger = logger;
            _clock = clock;

            DateTimeOffset now = _clock.SwedenNow;
            now -= now.TimeOfDay;

            nextDailyRunTime = now - now.TimeOfDay;
            nextDailyRunTime = nextDailyRunTime.AddHours(timeToRun);

            if (_clock.SwedenNow.Hour > timeToRun)

            {
                // Next remind is tomorrow
                nextDailyRunTime = nextDailyRunTime.AddDays(1);
            }

            _logger.LogDebug("Created EntityScheduler instance");
        }

        public void Init()
        {
            Task.Run(() => Run());

            _logger.LogInformation("EntityScheduler initialized");
        }

        private void Run()
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

            try
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
                        throw new InvalidOperationException($"All tasks instances didn't complete execution within the allotted time: {allotedTimeAllTasks/1000} seconds");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Entity Scheduler failed ({message}).", ex.Message);
                using (var serviceScope = _services.CreateScope())
                {
                    _ = serviceScope.ServiceProvider.GetRequiredService<EmailService>().SendErrorEmail(nameof(EntityScheduler), nameof(Run), ex);
                }
            }
            finally
            {
                Task.Delay(timeDelayContinousJobs).ContinueWith(t => Run());
            }

            _logger.LogTrace($"EntityScheduler done, scheduled to wake up in {timeDelayContinousJobs/1000} seconds again");
        }

        private async Task RunDailyJobs(IServiceProvider provider)
        {

            _logger.LogInformation($"Starting {nameof(RunDailyJobs)}");
            await provider.GetRequiredService<OrderService>().CleanTempAttachments();
            await provider.GetRequiredService<RequestService>().SendEmailReminders();
            await provider.GetRequiredService<VerificationService>().HandleTellusVerifications(true);
            _logger.LogInformation($"Completed {nameof(RunDailyJobs)}");

        }

        private async Task RunContinousJobs(IServiceProvider provider)
        {
            _logger.LogInformation($"Starting {nameof(RunContinousJobs)}");
            await provider.GetRequiredService<OrderService>().HandleAllScheduledTasks();
            await provider.GetRequiredService<RequestService>().DeleteRequestViews();
            await provider.GetRequiredService<EmailService>().SendEmails();
            await provider.GetRequiredService<WebHookService>().CallWebHooks();
            _logger.LogInformation($"Completed {nameof(RunContinousJobs)}");
        }
    }
}
