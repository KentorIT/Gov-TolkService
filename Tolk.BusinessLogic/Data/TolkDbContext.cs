using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Tolk.BusinessLogic.Entities;

namespace Tolk.BusinessLogic.Data
{
    public class TolkDbContext : IdentityDbContext<AspNetUser, IdentityRole<int>, int>
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
            .HasComputedColumnSql("CAST(YEAR([CreatedAt]) AS NVARCHAR(MAX)) + '-' + CAST(([OrderId]+(100000)) AS NVARCHAR(MAX))");

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

            builder.Entity<InterpreterBroker>()
                .HasKey(ib => new { ib.BrokerId, ib.InterpreterId });

            builder.Entity<OrderRequirementRequestAnswer>()
                .HasKey(ra => new { ra.RequestId, ra.OrderRequirementId });

            builder.Entity<OrderInterpreterLocation>()
                .HasKey(oil => new { oil.OrderId, oil.InterpreterLocation });

            builder.Entity<Order>()
                .HasOne(o => o.CreatedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .HasOne(o => o.CreatedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Request>()
                .HasOne(o => o.CancelledByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Request>()
                .HasOne(o => o.CancelledByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Request>()
                .HasOne(o => o.CancelConfirmedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Request>()
                .HasOne(o => o.CancelConfirmedByImpersonator)
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
                .WithMany(nameof(Request.RequirementAnswers))
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Requisition>()
                .HasOne(r => r.CreatedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Requisition>()
                .HasOne(r => r.CreatedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Requisition>()
                .HasOne(r => r.ProcessedUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Requisition>()
                .HasOne(r => r.ProcessedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(r => r.CreatedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(r => r.CreatedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(r => r.AnsweringUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(r => r.AnsweredByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(r => r.TerminatingUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(r => r.TerminatedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(r => r.AnswerDisputingUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(r => r.AnswerDisputedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Attachment>()
                .HasOne(r => r.CreatedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Attachment>()
                .HasOne(r => r.CreatedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderContactPersonHistory>()
             .HasOne(r => r.ChangedByImpersonator)
             .WithMany()
             .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderContactPersonHistory>()
             .HasOne(r => r.ChangedByUser)
             .WithMany()
             .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OutboundWebHookCall>()
                .HasOne(c => c.RecipientUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TemporaryAttachmentGroup>()
            .HasKey(t => new { t.TemporaryAttachmentGroupKey, t.AttachmentId });

            builder.Entity<RequisitionAttachment>()
            .HasKey(ra => new { ra.RequisitionId, ra.AttachmentId });

            builder.Entity<RequisitionAttachment>()
                .HasOne(ra => ra.Requisition)
                .WithMany(g => g.Attachments)
                .HasForeignKey(map => map.RequisitionId);

            builder.Entity<RequisitionAttachment>()
                .HasOne(map => map.Attachment)
                .WithMany(a => a.Requisitions)
                .HasForeignKey(map => map.AttachmentId);

            builder.Entity<RequestAttachment>()
            .HasKey(ra => new { ra.RequestId, ra.AttachmentId });

            builder.Entity<RequestAttachment>()
                .HasOne(ra => ra.Request)
                .WithMany(g => g.Attachments)
                .HasForeignKey(map => map.RequestId);

            builder.Entity<RequestAttachment>()
                .HasOne(map => map.Attachment)
                .WithMany(a => a.Requests)
                .HasForeignKey(map => map.AttachmentId);

            builder.Entity<OrderAttachment>()
           .HasKey(oa => new { oa.OrderId, oa.AttachmentId });

            builder.Entity<OrderAttachment>()
                .HasOne(oa => oa.Order)
                .WithMany(g => g.Attachments)
                .HasForeignKey(map => map.OrderId);

            builder.Entity<OrderAttachment>()
                .HasOne(map => map.Attachment)
                .WithMany(a => a.Orders)
                .HasForeignKey(map => map.AttachmentId);
        }

        public DbSet<Region> Regions { get; set; }

        public DbSet<Language> Languages { get; set; }

        public DbSet<CustomerOrganisation> CustomerOrganisations { get; set; }

        public DbSet<OrderRequirement> OrderRequirements { get; set; }

        public DbSet<OrderCompetenceRequirement> OrderCompetenceRequirements { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderPriceRow> OrderPriceRows { get; set; }

        public DbSet<Request> Requests { get; set; }

        public DbSet<RequestPriceRow> RequestPriceRows { get; set; }

        public DbSet<Broker> Brokers { get; set; }

        public DbSet<Interpreter> Interpreters { get; set; }

        public DbSet<Ranking> Rankings { get; set; }

        public DbSet<Holiday> Holidays { get; set; }

        public DbSet<Requisition> Requisitions { get; set; }

        public DbSet<RequisitionPriceRow> RequisitionPriceRows { get; set; }

        public DbSet<PriceListRow> PriceListRows { get; set; }

        public DbSet<PriceCalculationCharge> PriceCalculationCharges { get; set; }

        public DbSet<OutboundWebHookCall> OutboundWebHookCalls { get; set; }

        public DbSet<OutboundEmail> OutboundEmails { get; set; }

        public DbSet<Complaint> Complaints { get; set; }

        public DbSet<InterpreterBroker> InterpreterBrokers { get; set; }

        public DbSet<Attachment> Attachments { get; set; }

        public DbSet<RequisitionAttachment> RequisitionAttachments { get; set; }

        public DbSet<RequestAttachment> RequestAttachments { get; set; }

        public DbSet<OrderAttachment> OrderAttachments { get; set; }

        public DbSet<TemporaryAttachmentGroup> TemporaryAttachmentGroups { get; set; }

        public static bool isUserStoreInitialized = false;

        public bool IsUserStoreInitialized
        {
            get
            {
                if (!isUserStoreInitialized)
                {
                    // If it is false, we want to check it for every single request.
                    isUserStoreInitialized = Users.Count() != 0 || Roles.Count() != 0;
                }
                return isUserStoreInitialized;
            }
        }
    }
}
