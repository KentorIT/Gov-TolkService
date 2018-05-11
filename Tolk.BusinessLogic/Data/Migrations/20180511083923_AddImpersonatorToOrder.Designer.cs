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
    [Migration("20180511083923_AddImpersonatorToOrder")]
    partial class AddImpersonatorToOrder
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.0-preview2-30571")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
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

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<string>("RoleId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider");

                    b.Property<string>("ProviderKey");

                    b.Property<string>("ProviderDisplayName");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<string>("RoleId");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<string>("LoginProvider");

                    b.Property<string>("Name");

                    b.Property<string>("Value");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.AspNetUser", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AccessFailedCount");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Email")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed");

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
                    b.Property<int>("BrokerRegionId")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("BrokerId");

                    b.Property<int>("RegionId");

                    b.HasKey("BrokerRegionId");

                    b.HasIndex("BrokerId");

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

                    b.HasKey("CustomerOrganisationId");

                    b.ToTable("CustomerOrganisations");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.InterpreterBrokerRegion", b =>
                {
                    b.Property<int>("BrokerRegionId");

                    b.Property<string>("InterpreterId");

                    b.HasKey("BrokerRegionId", "InterpreterId");

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

                    b.Property<string>("CreatedBy");

                    b.Property<DateTime>("CreatedDate");

                    b.Property<int>("CustomerOrganisationId");

                    b.Property<string>("CustomerReferenceNumber")
                        .HasMaxLength(100);

                    b.Property<string>("Description")
                        .HasMaxLength(1000);

                    b.Property<DateTimeOffset>("EndDateTime");

                    b.Property<string>("ImpersonatingCreator");

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

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Ranking", b =>
                {
                    b.Property<int>("RankingId")
                        .ValueGeneratedOnAdd();

                    b.Property<decimal>("BrokerFee")
                        .HasColumnType("decimal(5, 2)");

                    b.Property<int>("BrokerRegionId");

                    b.Property<DateTimeOffset>("EndDate");

                    b.Property<int>("Rank");

                    b.Property<DateTimeOffset>("StartDate");

                    b.HasKey("RankingId");

                    b.HasIndex("BrokerRegionId")
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

                    b.Property<string>("Description")
                        .HasMaxLength(100);

                    b.Property<int>("OrderId");

                    b.Property<int>("RankingId");

                    b.Property<int?>("RankingId1");

                    b.HasKey("RequestId");

                    b.HasIndex("OrderId");

                    b.HasIndex("RankingId");

                    b.HasIndex("RankingId1");

                    b.ToTable("Requests");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.UserBroker", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<int>("BrokerId");

                    b.HasKey("UserId");

                    b.HasIndex("BrokerId");

                    b.ToTable("UserBroker");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.UserCustomerOrganisation", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<int>("CustomerOrganisationId");

                    b.HasKey("UserId");

                    b.HasIndex("CustomerOrganisationId");

                    b.ToTable("UserCustomerOrganisation");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole")
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser")
                        .WithMany("Roles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.BrokerRegion", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.Broker", "Broker")
                        .WithMany("BrokerRegions")
                        .HasForeignKey("BrokerId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.Region", "Region")
                        .WithMany()
                        .HasForeignKey("RegionId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.InterpreterBrokerRegion", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.BrokerRegion", "BrokerRegion")
                        .WithMany()
                        .HasForeignKey("BrokerRegionId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "Interpreter")
                        .WithMany()
                        .HasForeignKey("InterpreterId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Order", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "CreatedByUser")
                        .WithMany()
                        .HasForeignKey("CreatedBy");

                    b.HasOne("Tolk.BusinessLogic.Entities.CustomerOrganisation", "CustomerOrganisation")
                        .WithMany()
                        .HasForeignKey("CustomerOrganisationId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "CreatedByImpersonator")
                        .WithMany()
                        .HasForeignKey("ImpersonatingCreator");

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
                        .HasForeignKey("Tolk.BusinessLogic.Entities.Ranking", "BrokerRegionId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.Request", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.Order", "Order")
                        .WithMany("Requests")
                        .HasForeignKey("OrderId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.Ranking")
                        .WithMany("Requests")
                        .HasForeignKey("RankingId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tolk.BusinessLogic.Entities.Ranking", "Ranking")
                        .WithMany()
                        .HasForeignKey("RankingId1");
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.UserBroker", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.Broker", "Broker")
                        .WithMany()
                        .HasForeignKey("BrokerId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "User")
                        .WithOne("Broker")
                        .HasForeignKey("Tolk.BusinessLogic.Entities.UserBroker", "UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Tolk.BusinessLogic.Entities.UserCustomerOrganisation", b =>
                {
                    b.HasOne("Tolk.BusinessLogic.Entities.CustomerOrganisation", "CustomerOrganisation")
                        .WithMany()
                        .HasForeignKey("CustomerOrganisationId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Tolk.BusinessLogic.Entities.AspNetUser", "User")
                        .WithOne("CustomerOrganisation")
                        .HasForeignKey("Tolk.BusinessLogic.Entities.UserCustomerOrganisation", "UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
