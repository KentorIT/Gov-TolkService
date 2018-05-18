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
            services.AddDbContext<TolkDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DBConnection")));

            // Add overrides to identity related services first, then the TryAdd calls
            // within AddIdentity won't do anything.
            services.AddScoped<IUserClaimsPrincipalFactory<AspNetUser>, TolkClaimsPrincipalFactory>();
            services.AddScoped<ISecurityStampValidator, TolkSecurityStampValidator>();
            
            services.AddIdentity<AspNetUser, IdentityRole<int>>()
                .AddEntityFrameworkStores<TolkDbContext>()
                .AddDefaultTokenProviders();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddMemoryCache();
            services.AddScoped<SelectListService>();

            services.RegisterTolkAuthorization();

            services.AddMvc(opt =>
            {
                opt.ModelBinderProviders.Insert(0, new DateTimeOffsetModelBinderProvider());
            });

            services.Configure<RequestLocalizationOptions>(opt =>
            {
                var supportedCultures = new[] { new CultureInfo("sv-SE") };
                opt.DefaultRequestCulture = new RequestCulture("sv-SE", "sv-SE");
                opt.SupportedCultures = supportedCultures;
                opt.SupportedUICultures = supportedCultures;
            });

            services.AddSingleton<IValidationAttributeAdapterProvider, SwedishValidationAttributeAdapterProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

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
