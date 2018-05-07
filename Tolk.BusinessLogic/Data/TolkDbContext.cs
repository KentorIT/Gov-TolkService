using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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
            .HasComputedColumnSql("[OrderId] + 1000000");

        }

        public DbSet<Region> Regions { get; set; }

        public DbSet<Language> Languages { get; set; }

        public DbSet<CustomerOrganisation> CustomerOrganisations { get; set; }

        public DbSet<OrderRequirement> OrderRequirements { get; set; }

        public DbSet<Order> Orders { get; set; }
    }
}
