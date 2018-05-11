using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Tolk.BusinessLogic.Entities;

namespace Tolk.BusinessLogic.Data
{
    public class TolkDbContext : IdentityDbContext<AspNetUser>
    {
        public TolkDbContext(DbContextOptions<TolkDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            builder.Entity<Region>()
                .HasData(Region.Regions);

            builder.Entity<Order>()
            .Property(p => p.OrderNumber)
            .HasComputedColumnSql("[OrderId] + 10000000");

            builder.Entity<IdentityUserRole<string>>()
                .HasOne<AspNetUser>()
                .WithMany(u => u.Roles)
                .HasForeignKey(iur => iur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Request>()
                .HasOne<Ranking>()
                .WithMany(r => r.Requests)
                .HasForeignKey(r => r.RankingId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public DbSet<Region> Regions { get; set; }

        public DbSet<Language> Languages { get; set; }

        public DbSet<CustomerOrganisation> CustomerOrganisations { get; set; }

        public DbSet<OrderRequirement> OrderRequirements { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<Request> Requests { get; set; }

        public DbSet<Broker> Brokers { get; set; }

        public DbSet<BrokerRegion> BrokerRegions { get; set; }

        public DbSet<Ranking> Rankings { get; set; }

        public static bool isUserStoreInitialized = false;

        public bool IsUserStoreInitialized
        {
            get
            {
                if(!isUserStoreInitialized)
                {
                    // If it is false, we want to check it for every single request.
                    isUserStoreInitialized = Users.Count() != 0 || Roles.Count() != 0;
                }
                return isUserStoreInitialized;
            }
        }
    }
}
