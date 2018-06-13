﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Services
{
    public class EntityScheduler
    {
        private IServiceProvider _services;
        private ILogger<EntityScheduler> _logger;

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
                using (var serviceScope = _services.CreateScope())
                {
                    var orderTask = serviceScope.ServiceProvider.GetRequiredService<OrderService>().HandleExpiredRequests();
                    var emailTask = serviceScope.ServiceProvider.GetRequiredService<EmailService>().SendEmails();

                    Task.WaitAll(orderTask, emailTask);
                }
            }
            catch(Exception ex)
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
