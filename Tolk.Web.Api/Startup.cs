﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Api.Helpers;
using Tolk.Web.Api.Services;
using Tolk.Web.Api.Authorization;
using Tolk.Web.Api.Exceptions;

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
            services.AddApplicationInsightsTelemetry();

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
            services.AddAuthentication(options =>
            {
                // the scheme name has to match the value we're going to use in AuthenticationBuilder.AddScheme(...)
                options.DefaultAuthenticateScheme = "Custom Scheme";
                options.DefaultChallengeScheme = "Custom Scheme";
            })
            .AddCustomAuth(o => { });
            services.AddDbContext<TolkDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DBConnection")));
            services.AddAuthorization(opt =>
            {
                opt.AddPolicy(Policies.Broker, builder => builder.RequireClaim(TolkClaimTypes.BrokerId));
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
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
        public static void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseAuthentication();
            app.UseStaticFiles();
            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}");
            });
        }
    }
}
