﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tolk.BusinessLogic.Data;

namespace Tolk.BusinessLogic.Data.Migrations
{
    [DbContext(typeof(TolkDbContext))]
    partial class TolkDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.0-rtm-30799")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Name")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasName("RoleNameIndex")
                        .HasFilter("[NormalizedName] IS NOT NULL");

                    b.ToTable("AspNetRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<int>("RoleId");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<int>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.Property<string>("LoginProvider");

                    b.Property<string>("ProviderKey");

                    b.Property<string>("ProviderDisplayName");

                    b.Property<int>("UserId");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
                {
                    b.Property<int>("UserId");

                    b.Property<int>("RoleId");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.Property<int>("UserId");

                    b.Property<string>("LoginProvider");

                    b.Property<string>("Name");

                    b.Property<string>("Value");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.AspNetUser", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("AccessFailedCount");

                    b.Property<int?>("BrokerId");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<int?>("CustomerOrganisationId");

                    b.Property<string>("Email")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed");

                    b.Property<int?>("InterpreterId");

                    b.Property<bool>("LockoutEnabled");

                    b.Property<DateTimeOffset?>("LockoutEnd");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256);

                    b.Property<string>("PasswordHash");

                    b.Property<string>("PhoneNumber");

                    b.Property<bool>("PhoneNumberConfirmed");

                    b.Property<string>("SecurityStamp");

                    b.Property<bool>("TwoFactorEnabled");

                    b.Property<string>("UserName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("BrokerId");

                    b.HasIndex("CustomerOrganisationId");

                    b.HasIndex("InterpreterId")
                        .IsUnique()
                        .HasFilter("[InterpreterId] IS NOT NULL");

                    b.HasIndex("NormalizedEmail")
                        .HasName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Broker", b =>
                {
                    b.Property<int>("BrokerId");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.HasKey("BrokerId");

                    b.ToTable("Brokers");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.BrokerRegion", b =>
                {
                    b.Property<int>("BrokerId");

                    b.Property<int>("RegionId");

                    b.HasKey("BrokerId", "RegionId");

                    b.HasIndex("RegionId");

                    b.ToTable("BrokerRegions");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.CustomerOrganisation", b =>
                {
                    b.Property<int>("CustomerOrganisationId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(20);

                    b.Property<int>("PriceListType");

                    b.HasKey("CustomerOrganisationId");

                    b.ToTable("CustomerOrganisations");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Holiday", b =>
                {
                    b.Property<DateTime>("Date")
                        .HasColumnType("date");

                    b.Property<int>("DateType");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.HasKey("Date");

                    b.ToTable("Holidays");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Interpreter", b =>
                {
                    b.Property<int>("InterpreterId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.HasKey("InterpreterId");

                    b.ToTable("Interpreters");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.InterpreterBroker", b =>
                {
                    b.Property<int>("BrokerId");

                    b.Property<int>("InterpreterId");

                    b.Property<bool>("AcceptedByInterpreter");

                    b.HasKey("BrokerId", "InterpreterId");

                    b.HasIndex("InterpreterId");

                    b.ToTable("InterpreterBrokers");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Language", b =>
                {
                    b.Property<int>("LanguageId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100);

                    b.HasKey("LanguageId");

                    b.ToTable("Languages");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Order", b =>
                {
                    b.Property<int>("OrderId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool>("AllowMoreThanTwoHoursTravelTime");

                    b.Property<int>("AssignentType");

                    b.Property<string>("City")
                        .HasMaxLength(100);

                    b.Property<int?>("ContactPersonId");

                    b.Property<DateTimeOffset>("CreatedAt");

                    b.Property<int>("CreatedBy");

                    b.Property<int>("CustomerOrganisationId");

                    b.Property<string>("CustomerReferenceNumber")
                        .HasMaxLength(100);

                    b.Property<string>("Description")
                        .HasMaxLength(1000);

                    b.Property<DateTimeOffset>("EndAt");

                    b.Property<int?>("ImpersonatingCreator");

                    b.Property<int>("LanguageId");

                    b.Property<int?>("OffSiteAssignmentType");

                    b.Property<string>("OffSiteContactInformation")
                        .HasMaxLength(255);

                    b.Property<int>("OrderNumber")
                        .ValueGeneratedOnAddOrUpdate()
                        .HasComputedColumnSql("[OrderId] + 10000000");

                    b.Property<int>("RegionId");

                    b.Property<int>("RequiredCompetenceLevel");

                    b.Property<DateTimeOffset>("StartAt");

                    b.Property<int>("Status");

                    b.Property<string>("Street")
                        .HasMaxLength(100);

                    b.Property<string>("UnitName")
                        .HasMaxLength(100);

                    b.Property<string>("ZipCode")
                        .HasMaxLength(100);

                    b.HasKey("OrderId");

                    b.HasIndex("ContactPersonId");

                    b.HasIndex("CreatedBy");

                    b.HasIndex("CustomerOrganisationId");

                    b.HasIndex("ImpersonatingCreator");

                    b.HasIndex("LanguageId");

                    b.HasIndex("RegionId");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.OrderInterpreterLocation", b =>
                {
                    b.Property<int>("OrderId");

                    b.Property<int>("InterpreterLocation");

                    b.Property<int>("Rank");

                    b.HasKey("OrderId", "InterpreterLocation");

                    b.ToTable("OrderInterpreterLocation");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.OrderRequirement", b =>
                {
                    b.Property<int>("OrderRequirementId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Description")
                        .HasMaxLength(1000);

                    b.Property<bool>("IsRequired");

                    b.Property<int>("OrderId");

                    b.Property<int>("RequirementType");

                    b.HasKey("OrderRequirementId");

                    b.HasIndex("OrderId");

                    b.ToTable("OrderRequirements");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.OrderRequirementRequestAnswer", b =>
                {
                    b.Property<int>("RequestId");

                    b.Property<int>("OrderRequirementId");

                    b.Property<string>("Answer")
                        .HasMaxLength(1000);

                    b.Property<bool>("CanSatisfyRequirement");

                    b.Property<int?>("RequestId1");

                    b.HasKey("RequestId", "OrderRequirementId");

                    b.HasIndex("OrderRequirementId");

                    b.HasIndex("RequestId1");

                    b.ToTable("OrderRequirementRequestAnswer");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.OutboundEmail", b =>
                {
                    b.Property<int>("OutboundEmailId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Body")
                        .IsRequired();

                    b.Property<DateTimeOffset>("CreatedAt");

                    b.Property<DateTimeOffset?>("DeliveredAt");

                    b.Property<string>("Recipient")
                        .IsRequired();

                    b.Property<string>("Subject")
                        .IsRequired();

                    b.HasKey("OutboundEmailId");

                    b.ToTable("OutboundEmails");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.PriceListRow", b =>
                {
                    b.Property<int>("PriceListRowId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("CompetenceLevel");

                    b.Property<DateTime>("EndDate");

                    b.Property<int>("MaxMinutes");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(10, 2)");

                    b.Property<int>("PriceListType");

                    b.Property<int>("PriceRowType");

                    b.Property<DateTime>("StartDate");

                    b.HasKey("PriceListRowId");

                    b.ToTable("PriceListRows");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Ranking", b =>
                {
                    b.Property<int>("RankingId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("BrokerFee")
                        .HasColumnType("decimal(5, 2)");

                    b.Property<int>("BrokerId");

                    b.Property<DateTime>("FirstValidDate")
                        .HasColumnType("date");

                    b.Property<DateTime>("LastValidDate")
                        .HasColumnType("date");

                    b.Property<int>("Rank");

                    b.Property<int>("RegionId");

                    b.HasKey("RankingId");

                    b.HasIndex("BrokerId", "RegionId")
                        .IsUnique();

                    b.ToTable("Rankings");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Region", b =>
                {
                    b.Property<int>("RegionId");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(20);

                    b.HasKey("RegionId");

                    b.ToTable("Regions");

                    b.HasData(
                        new { RegionId = 1, Name = "Stockholm" },
                        new { RegionId = 2, Name = "Uppsala" },
                        new { RegionId = 3, Name = "Södermanland" },
                        new { RegionId = 4, Name = "Östergötland" },
                        new { RegionId = 5, Name = "Jönköping" },
                        new { RegionId = 6, Name = "Kronoberg" },
                        new { RegionId = 7, Name = "Kalmar" },
                        new { RegionId = 80, Name = "Gotland" },
                        new { RegionId = 8, Name = "Blekinge " },
                        new { RegionId = 25, Name = "Skåne" },
                        new { RegionId = 11, Name = "Halland" },
                        new { RegionId = 13, Name = "Västra Götaland" },
                        new { RegionId = 15, Name = "Värmland" },
                        new { RegionId = 16, Name = "Örebro" },
                        new { RegionId = 17, Name = "Västmanland" },
                        new { RegionId = 18, Name = "Dalarna" },
                        new { RegionId = 19, Name = "Gävleborg" },
                        new { RegionId = 20, Name = "Västernorrland" },
                        new { RegionId = 21, Name = "Jämtland Härjedalen" },
                        new { RegionId = 22, Name = "Västerbotten" },
                        new { RegionId = 23, Name = "Norrbotten" }
                    );
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Request", b =>
                {
                    b.Property<int>("RequestId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTimeOffset?>("AnswerDate");

                    b.Property<DateTimeOffset?>("AnswerProcessedAt");

                    b.Property<int?>("AnswerProcessedBy");

                    b.Property<int?>("AnsweredBy");

                    b.Property<string>("BrokerMessage")
                        .HasMaxLength(1000);

                    b.Property<int?>("CompetenceLevel");

                    b.Property<string>("DenyMessage")
                        .HasMaxLength(1000);

                    b.Property<decimal?>("ExpectedTravelCosts")
                        .HasColumnType("decimal(10, 2)");

                    b.Property<DateTimeOffset>("ExpiresAt");

                    b.Property<int?>("ImpersonatingAnswerProcessedBy");

                    b.Property<int?>("ImpersonatingAnsweredBy");

                    b.Property<int?>("ImpersonatingReceivedBy");

                    b.Property<int?>("InterpreterId");

                    b.Property<int?>("InterpreterLocation");

                    b.Property<int>("OrderId");

                    b.Property<int>("RankingId");

                    b.Property<int?>("ReceivedBy");

                    b.Property<DateTimeOffset?>("RecievedAt");

                    b.Property<int>("Status");

                    b.HasKey("RequestId");

                    b.HasIndex("AnswerProcessedBy");

                    b.HasIndex("AnsweredBy");

                    b.HasIndex("ImpersonatingAnswerProcessedBy");

                    b.HasIndex("ImpersonatingAnsweredBy");

                    b.HasIndex("ImpersonatingReceivedBy");

                    b.HasIndex("InterpreterId");

                    b.HasIndex("OrderId");

                    b.HasIndex("RankingId");

                    b.HasIndex("ReceivedBy");

                    b.ToTable("Requests");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Requisition", b =>
                {
                    b.Property<int>("RequisitionId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTimeOffset>("CreatedAt");

                    b.Property<int>("CreatedBy");

                    b.Property<string>("DenyMessage")
                        .HasMaxLength(255);

                    b.Property<int?>("ImpersonatingCreatedBy");

                    b.Property<int?>("ImpersonatingProcessedBy");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasMaxLength(1000);

                    b.Property<DateTimeOffset?>("ProcessedAt");

                    b.Property<int?>("ProcessedBy");

                    b.Property<int?>("ReplacedByRequisitionId");

                    b.Property<int>("RequestId");

                    b.Property<DateTimeOffset>("SessionEndedAt");

                    b.Property<DateTimeOffset>("SessionStartedAt");

                    b.Property<int>("Status");

                    b.Property<DateTimeOffset?>("TimeWasteAfterEndedAt");

                    b.Property<DateTimeOffset?>("TimeWasteBeforeStartedAt");

                    b.Property<decimal>("TravelCosts")
                        .HasColumnType("decimal(10, 2)");

                    b.HasKey("RequisitionId");

                    b.HasIndex("CreatedBy");

                    b.HasIndex("ImpersonatingCreatedBy");

                    b.HasIndex("ImpersonatingProcessedBy");

                    b.HasIndex("ProcessedBy");

                    b.HasIndex("ReplacedByRequisitionId");

                    b.HasIndex("RequestId");

                    b.ToTable("Requisitions");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<int>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<int>")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<int>", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<int>", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<int>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole<int>")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser")
                        .WithMany("Roles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<int>", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.AspNetUser", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.Broker", "Broker")
                        .WithMany("Users")
                        .HasForeignKey("BrokerId");

                    b.HasOne("Tolk.BusinessLogic.Entities.CustomerOrganisation", "CustomerOrganisation")
                        .WithMany("Users")
                        .HasForeignKey("CustomerOrganisationId");

                    b.HasOne("Tolk.BusinessLogic.Entities.Interpreter", "Interpreter")
                        .WithOne("User")
                        .HasForeignKey("Tolk.BusinessLogic.Entities.AspNetUser", "InterpreterId");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.BrokerRegion", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.Broker", "Broker")
                        .WithMany("BrokerRegions")
                        .HasForeignKey("BrokerId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.Region", "Region")
                        .WithMany("BrokerRegions")
                        .HasForeignKey("RegionId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.InterpreterBroker", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.Broker", "Broker")
                        .WithMany()
                        .HasForeignKey("BrokerId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.Interpreter", "Interpreter")
                        .WithMany("Brokers")
                        .HasForeignKey("InterpreterId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Order", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "ContactPersonUser")
                        .WithMany()
                        .HasForeignKey("ContactPersonId");

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "CreatedByUser")
                        .WithMany()
                        .HasForeignKey("CreatedBy")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.CustomerOrganisation", "CustomerOrganisation")
                        .WithMany()
                        .HasForeignKey("CustomerOrganisationId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "CreatedByImpersonator")
                        .WithMany()
                        .HasForeignKey("ImpersonatingCreator")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.Language", "Language")
                        .WithMany()
                        .HasForeignKey("LanguageId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.Region", "Region")
                        .WithMany()
                        .HasForeignKey("RegionId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.OrderInterpreterLocation", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.Order", "Order")
                        .WithMany("InterpreterLocations")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.OrderRequirement", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.Order", "Order")
                        .WithMany("Requirements")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.OrderRequirementRequestAnswer", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.OrderRequirement", "OrderRequirement")
                        .WithMany("RequirementAnswers")
                        .HasForeignKey("OrderRequirementId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.Request", "Request")
                        .WithMany()
                        .HasForeignKey("RequestId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.Request")
                        .WithMany("RequirementAnswers")
                        .HasForeignKey("RequestId1");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Ranking", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.BrokerRegion", "BrokerRegion")
                        .WithOne("Ranking")
                        .HasForeignKey("Tolk.BusinessLogic.Entities.Ranking", "BrokerId", "RegionId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Request", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "ProcessingUser")
                        .WithMany()
                        .HasForeignKey("AnswerProcessedBy")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "AnsweringUser")
                        .WithMany()
                        .HasForeignKey("AnsweredBy")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "AnswerProcessedByImpersonator")
                        .WithMany()
                        .HasForeignKey("ImpersonatingAnswerProcessedBy")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "AnsweredByImpersonator")
                        .WithMany()
                        .HasForeignKey("ImpersonatingAnsweredBy")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "ReceivedByImpersonator")
                        .WithMany()
                        .HasForeignKey("ImpersonatingReceivedBy")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.Interpreter", "Interpreter")
                        .WithMany()
                        .HasForeignKey("InterpreterId");

                    b.HasOne("Tolk.BusinessLogic.Entities.Order", "Order")
                        .WithMany("Requests")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.Ranking", "Ranking")
                        .WithMany("Requests")
                        .HasForeignKey("RankingId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "ReceivedByUser")
                        .WithMany()
                        .HasForeignKey("ReceivedBy")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Requisition", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "CreatedByUser")
                        .WithMany()
                        .HasForeignKey("CreatedBy")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "CreatedByImpersonator")
                        .WithMany()
                        .HasForeignKey("ImpersonatingCreatedBy")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "ProcessedByImpersonator")
                        .WithMany()
                        .HasForeignKey("ImpersonatingProcessedBy")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "ProcessedUser")
                        .WithMany()
                        .HasForeignKey("ProcessedBy")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.Requisition", "ReplacedByRequisition")
                        .WithMany()
                        .HasForeignKey("ReplacedByRequisitionId");

                    b.HasOne("Tolk.BusinessLogic.Entities.Request", "Request")
                        .WithMany("Requisitions")
                        .HasForeignKey("RequestId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
