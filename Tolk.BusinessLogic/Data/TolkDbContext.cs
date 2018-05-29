using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Tolk.BusinessLogic.Entities;

namespace Tolk.BusinessLogic.Data
{
    public class TolkDbContext : IdentityDbContext<AspNetUser,IdentityRole<int>, int>
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

            builder.Entity<IdentityUserRole<int>>()
                .HasOne<AspNetUser>()
                .WithMany(u => u.Roles)
                .HasForeignKey(iur => iur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AspNetUser>()
                .HasOne(u => u.Interpreter)
                .WithOne(i => i.User);

            builder.Entity<Request>()
                .HasOne(r => r.Ranking)
                .WithMany(r => r.Requests)
                .HasForeignKey(r => r.RankingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BrokerRegion>()
                .HasKey(br => new { br.BrokerId, br.RegionId });

            builder.Entity<InterpreterBrokerRegion>()
                .HasKey(ibr => new { ibr.BrokerId, ibr.RegionId, ibr.InterpreterId });

            builder.Entity<OrderRequirementRequestAnswer>()
                .HasKey(ra => new { ra.RequestId, ra.OrderRequirementId });

            builder.Entity<Order>()
                .HasOne(o => o.CreatedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .HasOne(o => o.CreatedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Request>()
                .HasOne(r => r.ReceivedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Request>()
                .HasOne(r => r.AnsweringUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Request>()
                .HasOne(r => r.ProcessingUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Request>()
                .HasOne(r => r.ReceivedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Request>()
                .HasOne(r => r.AnsweredByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Request>()
                .HasOne(r => r.AnswerProcessedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderRequirementRequestAnswer>()
                .HasOne(r => r.Request)
                .WithMany()
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

        public DbSet<Interpreter> Interpreters { get; set; }

        public DbSet<Ranking> Rankings { get; set; }

        public DbSet<Holiday> Holidays { get; set; }

        public DbSet<PriceListRow> PriceListRows { get; set; }

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
