using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Services
{
    public class EntityScheduler
    {
        private IServiceProvider _services;
        private ILogger<EntityScheduler> _logger;
        private ISwedishClock _clock;
        private DateTimeOffset nextDailyRunTime;

        private const int TimeToRun = 5;

        public EntityScheduler(IServiceProvider services, ILogger<EntityScheduler> logger, ISwedishClock clock)
        {
            _services = services;
            _logger = logger;
            _clock = clock;

            DateTimeOffset now = _clock.SwedenNow;
            now -= now.TimeOfDay;

            nextDailyRunTime = now - now.TimeOfDay;
            nextDailyRunTime = nextDailyRunTime.AddHours(TimeToRun);

            if (_clock.SwedenNow.Hour > TimeToRun)

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

            if ((nextDailyRunTime - _clock.SwedenNow).TotalHours > 25 || nextDailyRunTime.Hour != TimeToRun)
            {
                _logger.LogWarning("nextDailyRunTime set to invalid time, was {0}", nextDailyRunTime);
                DateTimeOffset now = _clock.SwedenNow;
                now -= now.TimeOfDay;
                nextDailyRunTime = now - now.TimeOfDay;
                nextDailyRunTime = nextDailyRunTime.AddHours(TimeToRun);

                if (_clock.SwedenNow.Hour > TimeToRun)
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
                        nextDailyRunTime = nextDailyRunTime - nextDailyRunTime.TimeOfDay;
                        nextDailyRunTime = nextDailyRunTime.AddHours(TimeToRun);
                        _logger.LogTrace("Running DailyRunTime, next run on {0}", nextDailyRunTime);

                        tasksToRun = new Task[]
                        {
                            serviceScope.ServiceProvider.GetRequiredService<OrderService>().CleanTempAttachments(),
                            serviceScope.ServiceProvider.GetRequiredService<RequestService>().SendEmailReminders(),
                            serviceScope.ServiceProvider.GetRequiredService<VerificationService>().ValidateTellusLanguageList(true)
                        };
                    }
                    else
                    {
                        tasksToRun = new Task[]
                        {
                            serviceScope.ServiceProvider.GetRequiredService<OrderService>().HandleStartedOrders(),
                            serviceScope.ServiceProvider.GetRequiredService<OrderService>().HandleExpiredRequests(),
                            serviceScope.ServiceProvider.GetRequiredService<OrderService>().HandleExpiredComplaints(),
                            serviceScope.ServiceProvider.GetRequiredService<OrderService>().HandleExpiredNonAnsweredRespondedRequests(),
                            serviceScope.ServiceProvider.GetRequiredService<RequestService>().DeleteRequestViews(),
                            serviceScope.ServiceProvider.GetRequiredService<EmailService>().SendEmails(),
                            serviceScope.ServiceProvider.GetRequiredService<WebHookService>().CallWebHooks()
                        };
                    }

                    Task.WaitAll(tasksToRun);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Entity Scheduler failed ({message}).", ex.Message);
            }
            finally
            {
                Task.Delay(15000).ContinueWith(t => Run());
            }

            _logger.LogTrace("EntityScheduler done, scheduled to wake up in 15 seconds again");
        }
    }
}
