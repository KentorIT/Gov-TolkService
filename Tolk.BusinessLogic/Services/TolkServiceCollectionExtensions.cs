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
            services.AddSingleton<ISwedishClock, TimeTravelClock>();
            services.AddTransient<UserService>();
            services.AddTransient<EmailService>();
            services.AddTransient<WebHookService>();
            services.AddTransient<INotificationService, NotificationService>();
            services.AddTransient<VerificationService>();
            services.AddTransient<InterpreterService>();
            services.AddTransient<ITellusConnection, TellusConnectionService>();
            services.AddScoped<PriceCalculationService>();
            services.AddScoped<RequestService>();
            services.AddScoped<RequisitionService>();
            services.AddScoped<ComplaintService>();
            services.AddScoped<HashService>();
        }
    }
}
