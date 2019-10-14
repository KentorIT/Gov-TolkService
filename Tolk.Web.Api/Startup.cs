using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Api.Helpers;
using Tolk.Web.Api.Services;

namespace Tolk.Web.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<TolkApiOptions>(Configuration);

            services.AddScoped<DateCalculationService>();
            services.AddScoped<RankingService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<PriceCalculationService>();
            services.AddScoped<OrderService>();
            services.AddScoped<RequestService>();
            services.AddScoped<RequisitionService>();
            services.AddScoped<ComplaintService>();
            services.AddScoped<ApiUserService>();
            services.AddSingleton<ISwedishClock, TimeService>();
            services.AddScoped<TimeService>();
            services.AddScoped<ITolkBaseOptions, Services.TolkBaseOptionsService>();
            services.AddScoped<HashService>();
            services.AddScoped<VerificationService>();
            services.AddScoped<InterpreterService>();
            services.AddScoped<EmailService>();
            services.AddScoped<ApiOrderService>();

            services.AddDbContext<TolkDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DBConnection")));
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Ping}/{id?}");
            });
        }
    }
}
