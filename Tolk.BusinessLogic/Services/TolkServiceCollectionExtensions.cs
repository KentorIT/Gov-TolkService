using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Services
{
    public static class TolkServiceCollectionExtensions
    {
        public static void AddTolkBusinessLogicServices(this IServiceCollection services)
        {
            services.AddTransient<RankingService>();
            services.AddTransient<OrderService>();
            services.AddTransient<DateCalculationService>();
            services.AddSingleton<TimeTravelClock, TimeTravelClock>();
        }
    }
}
