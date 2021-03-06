﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations;
using Tolk.BusinessLogic.Data;

namespace Tolk.BusinessLogic.Data.Migrations
{
    [DbContext(typeof(TolkDbContext))]
    [Migration("20180518123903_ConsolidatedSprint1")]
    partial class ConsolidatedSprint1
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.0-preview2-30571")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole<int>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

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
                        .ValueGeneratedOnAdd();

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
                        .ValueGeneratedOnAdd();

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
                        .ValueGeneratedOnAdd();

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
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(20);

                    b.Property<int>("PriceListType");

                    b.HasKey("CustomerOrganisationId");

                    b.ToTable("CustomerOrganisations");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Interpreter", b =>
                {
                    b.Property<int>("InterpreterId")
                        .ValueGeneratedOnAdd();

                    b.HasKey("InterpreterId");

                    b.ToTable("Interpreters");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.InterpreterBrokerRegion", b =>
                {
                    b.Property<int>("BrokerId");

                    b.Property<int>("RegionId");

                    b.Property<int>("InterpreterId");

                    b.HasKey("BrokerId", "RegionId", "InterpreterId");

                    b.HasIndex("InterpreterId");

                    b.ToTable("InterpreterBrokerRegion");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Language", b =>
                {
                    b.Property<int>("LanguageId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(20);

                    b.HasKey("LanguageId");

                    b.ToTable("Languages");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Order", b =>
                {
                    b.Property<int>("OrderId")
                        .ValueGeneratedOnAdd();

                    b.Property<bool>("AllowMoreThanTwoHoursTravelTime");

                    b.Property<int>("AssignentType");

                    b.Property<string>("City")
                        .HasMaxLength(100);

                    b.Property<int>("CreatedBy");

                    b.Property<DateTime>("CreatedDate");

                    b.Property<int>("CustomerOrganisationId");

                    b.Property<string>("CustomerReferenceNumber")
                        .HasMaxLength(100);

                    b.Property<string>("Description")
                        .HasMaxLength(1000);

                    b.Property<DateTimeOffset>("EndDateTime");

                    b.Property<int>("ImpersonatingCreator");

                    b.Property<int>("LanguageId");

                    b.Property<int>("OrderNumber")
                        .ValueGeneratedOnAddOrUpdate()
                        .HasComputedColumnSql("[OrderId] + 10000000");

                    b.Property<string>("OtherAddressInformation")
                        .HasMaxLength(255);

                    b.Property<string>("OtherContactEmail")
                        .HasMaxLength(255);

                    b.Property<string>("OtherContactPerson")
                        .HasMaxLength(255);

                    b.Property<string>("OtherContactPhone")
                        .HasMaxLength(50);

                    b.Property<int>("RegionId");

                    b.Property<int?>("RequestedCompetenceLevel");

                    b.Property<int?>("RequestedInterpreterLocation");

                    b.Property<int>("RequiredCompetenceLevel");

                    b.Property<int>("RequiredInterpreterLocation");

                    b.Property<DateTimeOffset>("StartDateTime");

                    b.Property<int>("Status");

                    b.Property<string>("Street")
                        .HasMaxLength(100);

                    b.Property<string>("UnitName")
                        .HasMaxLength(100);

                    b.Property<string>("ZipCode")
                        .HasMaxLength(100);

                    b.HasKey("OrderId");

                    b.HasIndex("CreatedBy");

                    b.HasIndex("CustomerOrganisationId");

                    b.HasIndex("ImpersonatingCreator");

                    b.HasIndex("LanguageId");

                    b.HasIndex("RegionId");

                    b.ToTable("Orders");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.OrderRequirement", b =>
                {
                    b.Property<int>("OrderRequirementId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Description")
                        .HasMaxLength(100);

                    b.Property<int>("OrderId");

                    b.Property<int>("RequirementType");

                    b.HasKey("OrderRequirementId");

                    b.HasIndex("OrderId");

                    b.ToTable("OrderRequirements");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.PriceListRow", b =>
                {
                    b.Property<int>("PriceListRowId")
                        .ValueGeneratedOnAdd();

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
                        .ValueGeneratedOnAdd();

                    b.Property<decimal>("BrokerFee")
                        .HasColumnType("decimal(5, 2)");

                    b.Property<int>("BrokerId");

                    b.Property<DateTimeOffset>("EndDate");

                    b.Property<int>("Rank");

                    b.Property<int>("RegionId");

                    b.Property<DateTimeOffset>("StartDate");

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
                        .ValueGeneratedOnAdd();

                    b.Property<string>("BrokerMessage")
                        .HasMaxLength(1000);

                    b.Property<decimal?>("ExpectedTravelCosts");

                    b.Property<int>("ImpersonatingModifier");

                    b.Property<int?>("InterpreterId");

                    b.Property<int>("ModifiedBy");

                    b.Property<DateTimeOffset?>("ModifiedDate");

                    b.Property<int>("OrderId");

                    b.Property<int>("RankingId");

                    b.Property<int>("Status");

                    b.HasKey("RequestId");

                    b.HasIndex("ImpersonatingModifier");

                    b.HasIndex("InterpreterId");

                    b.HasIndex("ModifiedBy");

                    b.HasIndex("OrderId");

                    b.HasIndex("RankingId");

                    b.ToTable("Requests");
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

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.InterpreterBrokerRegion", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.Interpreter", "Interpreter")
                        .WithMany("BrokerRegions")
                        .HasForeignKey("InterpreterId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.BrokerRegion", "BrokerRegion")
                        .WithMany()
                        .HasForeignKey("BrokerId", "RegionId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Order", b =>
                {
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

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.OrderRequirement", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.Order", "Order")
                        .WithMany("Requirements")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade);
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
                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "ModifiedByImpersonator")
                        .WithMany()
                        .HasForeignKey("ImpersonatingModifier")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.Interpreter", "Interpreter")
                        .WithMany()
                        .HasForeignKey("InterpreterId");

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "ModifyUser")
                        .WithMany()
                        .HasForeignKey("ModifiedBy")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.Order", "Order")
                        .WithMany("Requests")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.Ranking", "Ranking")
                        .WithMany("Requests")
                        .HasForeignKey("RankingId")
                        .OnDelete(DeleteBehavior.Restrict);
                });
#pragma warning restore 612, 618
        }
    }
}
