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
            services.AddSingleton<ISwedishClock, TimeTravelClock>();
            services.AddScoped<RankingService>();
            services.AddScoped<OrderService>();
            services.AddScoped<DateCalculationService>();
            services.AddScoped<InterpreterService>();
            services.AddScoped<ITolkBaseOptions, TolkBaseOptionsService>();
            services.AddScoped<UserService>();
            services.AddScoped<EmailService>();
            services.AddScoped<WebHookService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<VerificationService>();
            services.AddScoped<PriceCalculationService>();
            services.AddScoped<RequestService>();
            services.AddScoped<RequisitionService>();
            services.AddScoped<ComplaintService>();
            services.AddScoped<HashService>();
            services.AddScoped<StatisticsService>();
        }
    }
}
