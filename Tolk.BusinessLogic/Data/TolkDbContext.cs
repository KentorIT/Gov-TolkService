using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
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
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            builder.Entity<Region>()
                .HasData(Region.Regions);

            builder.Entity<Order>()
            .Property(p => p.OrderNumber)
            .HasComputedColumnSql("CAST(YEAR([CreatedAt]) AS NVARCHAR(MAX)) + '-' + CAST(([OrderId]+(100000)) AS NVARCHAR(MAX))");

            builder.Entity<OrderGroup>()
            .Property(p => p.OrderGroupNumber)
            .HasComputedColumnSql("'G-' + CAST(YEAR([CreatedAt]) AS NVARCHAR(100)) + '-' + CAST(([OrderGroupId]+(100000)) AS NVARCHAR(100))");

            builder.Entity<UserAuditLogEntry>()
                .HasOne(uale => uale.User)
                .WithMany(u => u.AuditLogEntries)
                .HasForeignKey(uale => uale.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserAuditLogEntry>()
                .HasOne(o => o.UpdatedByImpersonatorUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<IdentityUserRole<int>>()
                .HasOne<AspNetUser>()
                .WithMany(u => u.Roles)
                .HasForeignKey(iur => iur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<IdentityUserClaim<int>>()
                .HasOne<AspNetUser>()
                .WithMany(u => u.Claims)
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

            builder.Entity<UserNotificationSetting>()
                .HasKey(uns => new { uns.UserId, uns.NotificationChannel, uns.NotificationType });

            builder.Entity<OrderRequirementRequestAnswer>()
                .HasKey(ra => new { ra.RequestId, ra.OrderRequirementId });

            builder.Entity<OrderInterpreterLocation>()
                .HasKey(oil => new { oil.OrderId, oil.InterpreterLocation });

            builder.Entity<OrderGroupInterpreterLocation>()
                .HasKey(oil => new { oil.OrderGroupId, oil.InterpreterLocation });

            builder.Entity<OrderGroupCompetenceRequirement>()
                .HasKey(ogir => new { ogir.OrderGroupId, ogir.CompetenceLevel });

            builder.Entity<Holiday>()
                .HasKey(h => new { h.Date, h.DateType });

            builder.Entity<UserDefaultSetting>()
                .HasKey(uds => new { uds.UserId, uds.DefaultSettingType });

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

            builder.Entity<OutboundWebHookCall>()
                .HasOne(c => c.RecipientUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OutboundWebHookCall>()
                .HasOne(c => c.ResentUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OutboundWebHookCall>()
                .HasOne(c => c.ResentImpersonatorUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OutboundEmail>()
                .HasOne(c => c.ResentByUser)
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

            builder.Entity<RequestGroupAttachment>()
                .HasKey(ra => new { ra.RequestGroupId, ra.AttachmentId });

            builder.Entity<RequestGroupAttachment>()
                .HasOne(ra => ra.RequestGroup)
                .WithMany(g => g.Attachments)
                .HasForeignKey(map => map.RequestGroupId);

            builder.Entity<RequestGroupAttachment>()
                .HasOne(map => map.Attachment)
                .WithMany(a => a.RequestGroups)
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

            builder.Entity<OrderGroupAttachment>()
                .HasKey(oa => new { oa.OrderGroupId, oa.AttachmentId });

            builder.Entity<OrderGroupAttachment>()
                .HasOne(oa => oa.OrderGroup)
                .WithMany(g => g.Attachments)
                .HasForeignKey(map => map.OrderGroupId);

            builder.Entity<OrderGroupAttachment>()
                .HasOne(map => map.Attachment)
                .WithMany(a => a.OrderGroups)
                .HasForeignKey(map => map.AttachmentId);

            builder.Entity<RequestStatusConfirmation>()
                .HasOne(r => r.ConfirmedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RequestStatusConfirmation>()
                .HasOne(r => r.ImpersonatingConfirmedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RequestGroupStatusConfirmation>()
                .HasOne(r => r.ConfirmedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RequestGroupStatusConfirmation>()
                .HasOne(r => r.ImpersonatingConfirmedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RequisitionStatusConfirmation>()
                .HasOne(r => r.ConfirmedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RequisitionStatusConfirmation>()
                .HasOne(r => r.ImpersonatingConfirmedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderGroupStatusConfirmation>()
                .HasOne(r => r.ConfirmedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderGroupStatusConfirmation>()
                .HasOne(r => r.ImpersonatingConfirmedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RequestUpdateLatestAnswerTime>()
                .HasKey(ru => new { ru.RequestId });

            builder.Entity<RequestUpdateLatestAnswerTime>()
               .HasOne(ru => ru.UpdatedByUser)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RequestUpdateLatestAnswerTime>()
               .HasOne(ru => ru.UpdatedByImpersonator)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RequestGroupUpdateLatestAnswerTime>()
                .HasKey(ru => new { ru.RequestGroupId });

            builder.Entity<RequestGroupUpdateLatestAnswerTime>()
               .HasOne(ru => ru.UpdatedByUser)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RequestGroupUpdateLatestAnswerTime>()
               .HasOne(ru => ru.UpdatedByImpersonator)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderStatusConfirmation>()
                .HasOne(o => o.ConfirmedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderStatusConfirmation>()
                .HasOne(o => o.ImpersonatingConfirmedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SystemMessage>()
                .HasOne(s => s.CreatedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SystemMessage>()
                .HasOne(s => s.CreatedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SystemMessage>()
                .HasOne(s => s.LastUpdatedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SystemMessage>()
                .HasOne(s => s.LastUpdatedImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RequestView>()
                .HasOne(r => r.ViewedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RequestView>()
                .HasOne(r => r.ViewedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RequestGroupView>()
                .HasOne(r => r.ViewedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RequestGroupView>()
                .HasOne(r => r.ViewedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InterpreterBroker>()
                .HasOne(o => o.InactivatedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InterpreterBroker>()
                .HasOne(o => o.InactivatedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CustomerUnit>()
                .HasOne(c => c.CreatedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CustomerUnit>()
                .HasOne(c => c.CreatedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CustomerUnit>()
                .HasOne(c => c.InactivatedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CustomerUnit>()
                .HasOne(c => c.InactivatedByImpersonator)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CustomerUnitUser>()
                .HasKey(cu => new { cu.CustomerUnitId, cu.UserId });

            builder.Entity<CustomerUnitUser>()
                .HasOne(cu => cu.User)
                .WithMany(g => g.CustomerUnits)
                .HasForeignKey(map => map.UserId);

            builder.Entity<CustomerUnitUser>()
                .HasOne(cu => cu.CustomerUnit)
                .WithMany(g => g.CustomerUnitUsers)
                .HasForeignKey(map => map.CustomerUnitId);

            builder.Entity<FaqDisplayUserRole>()
                .HasKey(h => new { h.FaqId, h.DisplayUserRole });

            builder.Entity<Faq>()
                .HasOne(f => f.CreatedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Faq>()
                .HasOne(f => f.LastUpdatedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderChangeConfirmation>()
               .HasOne(occ => occ.OrderChangeLogEntry)
               .WithOne(oc => oc.OrderChangeConfirmation)
               .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TemporaryChangedEmailEntry>()
               .HasOne(t => t.User)
               .WithOne(u => u.TemporaryChangedEmailEntry)
               .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TemporaryChangedEmailEntry>()
                .HasOne(t => t.UpdatedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TemporaryChangedEmailEntry>()
                .HasOne(t => t.ImpersonatingUpdatedByUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);
        }

        public DbSet<Region> Regions { get; set; }

        public DbSet<Language> Languages { get; set; }

        public DbSet<CustomerOrganisation> CustomerOrganisations { get; set; }

        public DbSet<OrderRequirement> OrderRequirements { get; set; }
        public DbSet<OrderRequirementRequestAnswer> OrderRequirementRequestAnswer { get; set; }

        public DbSet<OrderGroupRequirement> OrderGroupRequirements { get; set; }

        public DbSet<OrderGroupCompetenceRequirement> OrderGroupCompetenceRequirements { get; set; }

        public DbSet<OrderGroupInterpreterLocation> OrderGroupInterpreterLocations { get; set; }

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

        public DbSet<RequestGroupAttachment> RequestGroupAttachments { get; set; }

        public DbSet<OrderAttachment> OrderAttachments { get; set; }

        public DbSet<OrderGroupAttachment> OrderGroupAttachments { get; set; }

        public DbSet<RequestStatusConfirmation> RequestStatusConfirmation { get; set; }

        public DbSet<RequestGroupStatusConfirmation> RequestGroupStatusConfirmations { get; set; }

        public DbSet<RequisitionStatusConfirmation> RequisitionStatusConfirmations { get; set; }

        public DbSet<OrderStatusConfirmation> OrderStatusConfirmation { get; set; }

        public DbSet<OrderGroupStatusConfirmation> OrderGroupStatusConfirmations { get; set; }

        public DbSet<TemporaryAttachmentGroup> TemporaryAttachmentGroups { get; set; }

        public DbSet<UserNotificationSetting> UserNotificationSettings { get; set; }

        public DbSet<UserAuditLogEntry> UserAuditLogEntries { get; set; }

        public DbSet<AspNetUserHistoryEntry> AspNetUserHistoryEntries { get; set; }

        public DbSet<AspNetUserRoleHistoryEntry> AspNetUserRoleHistoryEntries { get; set; }

        public DbSet<AspNetUserClaimHistoryEntry> AspNetUserClaimHistoryEntries { get; set; }

        public DbSet<UserNotificationSettingHistoryEntry> UserNotificationSettingHistoryEntries { get; set; }

        public DbSet<CustomerUnitUserHistoryEntry> CustomerUnitUserHistoryEntries { get; set; }

        public DbSet<TemporaryChangedEmailEntry> TemporaryChangedEmailStoreEntries { get; set; }

        public DbSet<MealBreak> MealBreaks { get; set; }

        public DbSet<SystemMessage> SystemMessages { get; set; }

        public DbSet<UserLoginLogEntry> UserLoginLogEntries { get; set; }

        public DbSet<RequestView> RequestViews { get; set; }

        public DbSet<RequestGroupView> RequestGroupViews { get; set; }

        public DbSet<FailedWebHookCall> FailedWebHookCalls { get; set; }

        public DbSet<CustomerUnit> CustomerUnits { get; set; }

        public DbSet<CustomerUnitUser> CustomerUnitUsers { get; set; }

        public DbSet<Faq> Faq { get; set; }

        public DbSet<OrderGroup> OrderGroups { get; set; }

        public DbSet<FaqDisplayUserRole> FaqDisplayUserRole { get; set; }

        public DbSet<RequestGroup> RequestGroups { get; set; }

        public DbSet<Quarantine> Quarantines { get; set; }

        public DbSet<QuarantineHistoryEntry> QuarantineHistoryEntries { get; set; }

        public DbSet<UserDefaultSetting> UserDefaultSettings { get; set; }

        public DbSet<UserDefaultSettingHistoryEntry> UserDefaultSettingHistoryEntries { get; set; }

        public DbSet<UserDefaultSettingOrderRequirement> UserDefaultSettingOrderRequirements { get; set; }

        public DbSet<UserDefaultSettingsOrderRequirementHistoryEntry> UserDefaultSettingsOrderRequirementHistoryEntries { get; set; }

        public DbSet<OrderHistoryEntry> OrderHistoryEntries { get; set; }

        public DbSet<OrderChangeLogEntry> OrderChangeLogEntries { get; set; }

        public DbSet<OrderAttachmentHistoryEntry> OrderAttachmentHistoryEntries { get; set; }

        public DbSet<OrderChangeConfirmation> OrderChangeConfirmations { get; set; }

        public DbQuery<OrderListRow> OrderListRows { get; set; }

        public DbQuery<RequestListRow> RequestListRows { get; set; }

        private static bool isUserStoreInitialized = false;

        public bool IsUserStoreInitialized
        {
            get
            {
                if (!isUserStoreInitialized)
                {
                    // If it is false, we want to check it for every single request.
                    isUserStoreInitialized = Users.Any();
                }
                return isUserStoreInitialized;
            }
        }
    }
}
