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
        private DateTimeOffset? nextClean = null;
        private DateTimeOffset nextRemind;

        public EntityScheduler(IServiceProvider services, ILogger<EntityScheduler> logger, ISwedishClock clock)
        {
            _services = services;
            _logger = logger;
            _clock = clock;

            if (_clock.SwedenNow.Hour < 5)
            {
                // Next Remind is today
                nextRemind = _clock.SwedenNow.Date.AddHours(5);
            }
            else
            {
                // Next remind is tomorrow
                nextRemind = _clock.SwedenNow.AddDays(1).Date.AddHours(5);
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

            try
            {
                //would like to have a timer here, to make it possible to get tighter runs if the last ru ran for longer than 10 seconds or somethng...
                using (var serviceScope = _services.CreateScope())
                {
                    Task[] tasksToRun;

                    if (nextClean == null || _clock.SwedenNow > nextClean)
                    {
                        nextClean = _clock.SwedenNow.AddDays(1).Date;
                        tasksToRun = new Task[]
                        {
                            serviceScope.ServiceProvider.GetRequiredService<OrderService>().CleanTempAttachments()
                        };
                    }
                    else if (_clock.SwedenNow > nextRemind)
                    {
                        nextRemind = _clock.SwedenNow.AddDays(1).Date.AddHours(5);
                        tasksToRun = new Task[]
                        {
                            serviceScope.ServiceProvider.GetRequiredService<RequestService>().SendEmailReminders()
                        };
                    }
                    else
                    {
                        tasksToRun = new Task[]
                        {
                            serviceScope.ServiceProvider.GetRequiredService<OrderService>().HandleExpiredRequests(),
                            serviceScope.ServiceProvider.GetRequiredService<OrderService>().HandleExpiredComplaints(),
                            serviceScope.ServiceProvider.GetRequiredService<OrderService>().HandleExpiredReplacedInterpreterRequests(),
                            serviceScope.ServiceProvider.GetRequiredService<OrderService>().HandleExpiredNonAnsweredRespondedRequests(),
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
