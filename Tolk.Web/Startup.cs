using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tolk.Web.Services;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using System.Linq;
using Microsoft.AspNetCore.Authentication.Cookies;
using Tolk.Web.Resources;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Tolk.Web.Authorization;
using Tolk.BusinessLogic.Services;
using Microsoft.Extensions.Internal;
using Tolk.Web.Helpers;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Tolk.BusinessLogic.Helpers;
using System;

namespace Tolk.Web
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
            services.Configure<TolkOptions>(Configuration);
            services.PostConfigure<TolkOptions>(opt => opt.Validate());

            services.AddDbContext<TolkDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DBConnection")));

            // Add overrides to identity related services first, then the TryAdd calls
            // within AddIdentity won't do anything.
            services.AddScoped<IUserClaimsPrincipalFactory<AspNetUser>, TolkClaimsPrincipalFactory>();
            services.AddScoped<ISecurityStampValidator, TolkSecurityStampValidator>();

            services.AddScoped<IdentityErrorDescriber, SwedishIdentityErrorDescriber>();

            services.Configure<CookieAuthenticationOptions>(opt =>
            {
                // Log in sessions last for 90 days.
                opt.ExpireTimeSpan = TimeSpan.FromDays(90);
                // If less than half of ExpireTimeSpan remains, lengthen session to ExpireTimeSpan again.
                opt.SlidingExpiration = true;
            });

            services.AddIdentity<AspNetUser, IdentityRole<int>>(opt =>
            {
                opt.SignIn.RequireConfirmedEmail = true;
            })
                .AddEntityFrameworkStores<TolkDbContext>()
                .AddDefaultTokenProviders();

            services.AddMemoryCache();
            services.AddScoped<SelectListService>();
            services.AddScoped<PriceCalculationService>();

            services.RegisterTolkAuthorizationPolicies();

            services.AddMvc(opt =>
            {
                opt.ModelBinderProviders.Insert(0, new DateTimeOffsetModelBinderProvider());
                opt.ModelBinderProviders.Insert(1, new TimeSpanModelBinderProvider());
                opt.ModelMetadataDetailsProviders.Add(new ClientRequiredAttribute.ValidationMetadataProvider());
            });

            services.Configure<RequestLocalizationOptions>(opt =>
            {
                var supportedCultures = new[] { new CultureInfo("sv-SE") };
                opt.DefaultRequestCulture = new RequestCulture("sv-SE", "sv-SE");
                opt.SupportedCultures = supportedCultures;
                opt.SupportedUICultures = supportedCultures;
            });

            services.AddSingleton<IValidationAttributeAdapterProvider, SwedishValidationAttributeAdapterProvider>();

            services.AddSingleton<EntityScheduler>();

            services.AddTolkBusinessLogicServices();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            TolkDbContext dbContext)
        {
            if (env.IsDevelopment() && false)
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            dbContext.Database.Migrate();

            app.UseStaticFiles();

            app.UseAuthentication();

            var swedishCulture = new CultureInfo("sv-SE");
            var cultureArray = new[] { swedishCulture };

            app.UseRequestLocalization(new RequestLocalizationOptions()
            {
                DefaultRequestCulture = new RequestCulture(swedishCulture),
                SupportedCultures = cultureArray,
                SupportedUICultures = cultureArray
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
