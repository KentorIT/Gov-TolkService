using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;
using Tolk.Web.Services;

namespace Tolk.Web
{
    public class Startup
    {
        private const string EmailConfirmationTokenProviderName = "ConfirmEmail";
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

            services.AddScoped<MockTellusApiService>();

            services.ConfigureApplicationCookie(opt =>
            {
                //This does not work, for some reason...
                //opt.LoginPath = new PathString("/Home/IndexNotLoggedIn");
                // Log in sessions last two hours, sliding.
                opt.ExpireTimeSpan = TimeSpan.FromHours(2);
                // If less than half of ExpireTimeSpan remains, lengthen session to ExpireTimeSpan again.
                opt.SlidingExpiration = true;
            });
            services.Configure<IdentityOptions>(options =>
            {
                options.Tokens.EmailConfirmationTokenProvider = EmailConfirmationTokenProviderName;
                options.Tokens.ChangeEmailTokenProvider = EmailConfirmationTokenProviderName;
            });

            services.Configure<ConfirmEmailDataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromDays(7);
            });
            services.AddIdentity<AspNetUser, IdentityRole<int>>(opt =>
            {
                opt.SignIn.RequireConfirmedEmail = true;
            })
                .AddEntityFrameworkStores<TolkDbContext>()
                .AddDefaultTokenProviders()
                .AddTokenProvider<ConfirmEmailDataProtectorTokenProvider<AspNetUser>>(EmailConfirmationTokenProviderName);

            services.AddMemoryCache();
            services.AddScoped<SelectListService>();

            services.AddTransient<HelpLinkService>();

            services.RegisterTolkAuthorizationPolicies();
            services.AddMvc(opt =>
            {
                opt.ModelBinderProviders.Insert(0, new DateTimeOffsetModelBinderProvider());
                opt.ModelBinderProviders.Insert(1, new TimeSpanModelBinderProvider());
                opt.ModelBinderProviders.Insert(2, new RadioButtonGroupModelBinderProvider());
                opt.ModelBinderProviders.Insert(3, new CheckboxGroupModelBinderProvider());
                opt.ModelMetadataDetailsProviders.Add(new ClientRequiredAttribute.ValidationMetadataProvider());
            });

            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<OrderModel, ReplaceOrderModel>();
            });

            // This does not, for some reason, work. I had to initialize the map when I use it instead...
            //services.AddAutoMapper(cfg =>
            //{
            //    cfg.CreateMap<OrderModel, ReplaceOrderModel>();
            //});

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
            TolkDbContext dbContext,
            RoleManager<IdentityRole<int>> roleManager
            )
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
            var autoMigrate = Configuration.GetSection("Database")["AutoMigrateOnStartup"];

            if (autoMigrate != null && bool.Parse(autoMigrate))
            {
                dbContext.Database.Migrate();
            }

            if (!roleManager.RoleExistsAsync(Roles.SystemAdministrator).Result)
            {
                IdentityResult roleResult = roleManager.CreateAsync(new IdentityRole<int>(Roles.SystemAdministrator)).Result;
            }
            if (!roleManager.RoleExistsAsync(Roles.Impersonator).Result)
            {
                IdentityResult roleResult = roleManager.CreateAsync(new IdentityRole<int>(Roles.Impersonator)).Result;
            }
            if (!roleManager.RoleExistsAsync(Roles.CentralAdministrator).Result)
            {
                IdentityResult roleResult = roleManager.CreateAsync(new IdentityRole<int>(Roles.CentralAdministrator)).Result;
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
            app.UseRewriter(new RewriteOptions
            {
                Rules =
                {
                    new RedirectHostRule
                    {
                        InternalHost = Configuration["InternalHost"],
                        OfficialSiteUrl = Configuration["PublicOrigin"],
                    }
                }
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
