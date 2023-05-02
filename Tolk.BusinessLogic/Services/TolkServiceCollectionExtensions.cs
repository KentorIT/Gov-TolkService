using Microsoft.Extensions.DependencyInjection;

namespace Tolk.BusinessLogic.Services
{
    public static class TolkServiceCollectionExtensions
    {
        public static void AddTolkBusinessLogicServices(this IServiceCollection services)
        {
            services.AddSingleton<ISwedishClock, TimeTravelClock>();

            services.AddTransient<ITolkBaseOptions, TolkBaseOptionsService>();
            services.AddTransient<EmailService>();
            services.AddTransient<WebHookService>();
            services.AddTransient<PeppolService>();

            services.AddScoped<ErrorNotificationService>();
            services.AddScoped<RankingService>();
            services.AddScoped<OrderService>();
            services.AddScoped<DateCalculationService>();
            services.AddScoped<InterpreterService>();
            services.AddScoped<UserService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<VerificationService>();
            services.AddScoped<PriceCalculationService>();
            services.AddScoped<RequestService>();
            services.AddScoped<RequisitionService>();
            services.AddScoped<ComplaintService>();
            services.AddScoped<StatisticsService>();
            services.AddScoped<CacheService>();            
            services.AddScoped<ContractService>();            
            services.AddScoped<StandardBusinessDocumentService>();            
        }
    }
}
