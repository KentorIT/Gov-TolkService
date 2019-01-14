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
        private DateTime? nextClean = null;

        public EntityScheduler(IServiceProvider services, ILogger<EntityScheduler> logger)
        {
            _services = services;
            _logger = logger;

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

                    if (nextClean == null || DateTime.Now > nextClean)
                    {
                        nextClean = DateTime.Now.AddDays(1).Date;
                        tasksToRun = new Task[]
                        {
                            serviceScope.ServiceProvider.GetRequiredService<OrderService>().CleanTempAttachments()
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
