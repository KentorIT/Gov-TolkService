using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Api.Helpers;
using Tolk.Web.Api.Services;
using Tolk.Web.Api.Authorization;
using Tolk.Web.Api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Tolk.Api.Payloads.Responses;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using Microsoft.Extensions.Hosting;

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

            services.AddDistributedMemoryCache();
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
            services.AddScoped<VerificationService>();
            services.AddScoped<InterpreterService>();
            services.AddScoped<EmailService>();
            services.AddScoped<ApiOrderService>();
            services.AddScoped<OrderService>();
            services.AddScoped<CacheService>();

            services.AddAuthentication(options =>
            {
                // the scheme name has to match the value we're going to use in AuthenticationBuilder.AddScheme(...)
                options.DefaultAuthenticateScheme = CustomAuthHandler.SchemeName;
                options.DefaultChallengeScheme = CustomAuthHandler.SchemeName;
            })
            .AddCustomAuth(o => { });
            services.AddDbContext<TolkDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DBConnection")));
            services.AddAuthorization(opt =>
            {
                opt.AddPolicy(Policies.Broker, builder => builder.RequireClaim(TolkClaimTypes.BrokerId));
                opt.AddPolicy(Policies.Customer, builder => builder.RequireClaim(TolkClaimTypes.CustomerOrganisationId));
            });

            services.AddRazorPages();
            services.AddControllers().AddNewtonsoftJson();
            services.AddOpenApiDocument(document =>
            {
                document.OperationProcessors.Add(new AddRequiredHeaderParameter("X-Kammarkollegiet-InterpreterService-UserName"));
                document.OperationProcessors.Add(new AddRequiredHeaderParameter("X-Kammarkollegiet-InterpreterService-ApiKey"));

                document.PostProcess = postProcess =>
                {
                    postProcess.Info.Description = "Detta är en beskrivning av Tolkavropstjänstens API för förmedlingar.";
                    postProcess.Info.Title = "Tolkavropstjänstens API för förmedlingar.";
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            if (Configuration.GetSection("EnableFileLogging").Get<bool>())
            {
                loggerFactory.AddLog4Net(Configuration.GetSection("Log4NetCore").Get<Microsoft.Extensions.Logging.Log4NetProviderOptions>());
            }
            app.UseStaticFiles();

            app.UseOpenApi();
            app.UseSwaggerUi3();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}");
            });
        }
    }
}
