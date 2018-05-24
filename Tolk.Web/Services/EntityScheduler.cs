using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Services
{
    public class EntityScheduler
    {
        private IServiceProvider _services;
        private ILogger _logger;

        public EntityScheduler(IServiceProvider services, ILoggerFactory loggerFactory)
        {
            _services = services;
            _logger = loggerFactory.CreateLogger<EntityScheduler>();

            _logger.LogDebug("Created EntityScheduler instance");
        }

        public void Init()
        {
            using (var serviceScope = _services.CreateScope())
            {
                var ctx = serviceScope.ServiceProvider.GetRequiredService<TolkDbContext>();
            }

            _logger.LogInformation("EntityScheduler initialized");
        }
    }
}
