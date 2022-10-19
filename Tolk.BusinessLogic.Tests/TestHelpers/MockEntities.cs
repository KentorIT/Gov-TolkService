using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Tests.TestHelpers
{
    public static class MockEntities
    {
        public static CustomerOrganisation[] MockCustomers => new[]
            {
                new CustomerOrganisation{CustomerOrganisationId = 1, Name = "Myndighet A", OrganisationNumber = "734001-9810", UseOrderAgreementsFromDate = new DateTime(1990,1,1,13,0,0) },
                new CustomerOrganisation{CustomerOrganisationId = 2, Name = "Myndighet B", OrganisationNumber = "734001-9811" },
                new CustomerOrganisation{CustomerOrganisationId = 3, Name = "Myndighet C", OrganisationNumber = "734001-9812" },
                new CustomerOrganisation{CustomerOrganisationId = 4, Name = "Myndighet D", OrganisationNumber = "734001-9813" },
                new CustomerOrganisation{CustomerOrganisationId = 5, Name = "Myndighet E", OrganisationNumber = "734001-9814" },
                new CustomerOrganisation{CustomerOrganisationId = 6, Name = "Myndighet F", OrganisationNumber = "734001-9815" },
            };

        public static AspNetUser[] MockCustomerUsers(CustomerOrganisation[] mockCustomers) => new[]
            {
                new AspNetUser(1, "Arne@a.se", "Arne", "Arne", "Aronson", mockCustomers[0]),
                new AspNetUser(2, "Berit@b.se", "Berit", "Berit", "Bryntesson", mockCustomers[1]),
                new AspNetUser(3, "Ceasar@c.se", "Ceasar", "Ceasar", "Claesson", mockCustomers[2]),
                new AspNetUser(4, "doris@a.se", "Doris", "Doris", "Degerman", mockCustomers[0]),
                new AspNetUser(5, "emanuel@d.se", "Emanuel", "Emanuel", "Eriksson", mockCustomers[3]),
                new AspNetUser(6, "filippa@d.se", "Filippa", "Filippa", "Fröman", mockCustomers[3]),
            };

        public static Language[] MockLanguages => new[]
            {
                new Language { LanguageId = 1, Name = "English" },
                new Language { LanguageId = 2, Name = "German" },
                new Language { LanguageId = 3, Name = "French" },
                new Language { LanguageId = 4, Name = "Chinese" },
                new Language { LanguageId = 5, Name = "Danish" },
                new Language { LanguageId = 6, Name = "Spanish" },
                new Language { LanguageId = 7, Name = "Arabic" },
                new Language { LanguageId = 8, Name = "Italian" },
            };

        public static Ranking[] MockRankings => new[]
            {
                        new Ranking {
                RankingId = 1,
                Rank = 1,
                FirstValidDate = new DateTime(2018, 01, 01),
                LastValidDate = new DateTime(2099, 01, 01),
                BrokerFee = (decimal)0.1,
                BrokerId = 1,
                RegionId = 1,
                FrameworkAgreementId = 1,
                Quarantines = new List<Quarantine>()
            },
            new Ranking {
                RankingId = 2,
                Rank = 2,
                FirstValidDate = new DateTime(2018, 01, 01),
                LastValidDate = new DateTime(2099, 01, 01),
                BrokerFee = (decimal)0.2,
                BrokerId = 2,
                RegionId = 2,
                FrameworkAgreementId = 1,
                Quarantines = new List<Quarantine>()
            }
        };

        public static Ranking[] MockRankingsWithQuarantines => new[]
            {
                new Ranking {
                    RegionId = 1,
                    RankingId = 1,
                    BrokerId = 1,
                    Rank = 1,
                    Quarantines = new [] {
                        new Quarantine {
                            QuarantineId = 1,
                            ActiveFrom =  new DateTimeOffset(2018,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            ActiveTo =  new DateTimeOffset(2019,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            CustomerOrganisationId = 1
                        },
                         new Quarantine {
                            QuarantineId = 2,
                            ActiveFrom =  new DateTimeOffset(2010,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            ActiveTo =  new DateTimeOffset(2011,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            CustomerOrganisationId = 2,
                            Motivation = "Old for cust 2"
                        },
                         new Quarantine {
                            QuarantineId = 3,
                            ActiveFrom =  new DateTimeOffset(2010,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            ActiveTo =  new DateTimeOffset(2011,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            CustomerOrganisationId = 1,
                            Motivation = "Old for cust 1"
                        },
                        new Quarantine {
                            QuarantineId = 4,
                            ActiveFrom =  new DateTimeOffset(2018,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            ActiveTo =  new DateTimeOffset(2019,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            CustomerOrganisationId = 3,
                            Motivation = "First cust 3"
                        },
                    }.ToList()
                },
                new Ranking {
                    RegionId = 1,
                    RankingId = 2,
                    BrokerId = 2,
                    Rank = 2,
                    Quarantines = new [] {
                        new Quarantine {
                            QuarantineId = 5,
                            ActiveFrom =  new DateTimeOffset(2018,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            ActiveTo =  new DateTimeOffset(2019,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            CustomerOrganisationId = 2,
                            Motivation = "active Quarantine for cust 2, on second rank"
                        },
                        new Quarantine {
                            QuarantineId = 6,
                            ActiveFrom =  new DateTimeOffset(2018,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            ActiveTo =  new DateTimeOffset(2019,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            CustomerOrganisationId = 3,
                            Motivation = "Second cust 3"
                        }
                    }.ToList()
                },
                new Ranking { RegionId = 1, RankingId = 3, BrokerId = 3, Rank = 3, Quarantines = new List<Quarantine>() },
                new Ranking { RegionId = 1, RankingId = 4, BrokerId = 4, Rank = 4, Quarantines = new List<Quarantine>() },
                new Ranking { RegionId = 2, RankingId = 1, BrokerId = 1, Rank = 1, Quarantines = new List<Quarantine>() },
                new Ranking { RegionId = 2, RankingId = 2, BrokerId = 2, Rank = 2, Quarantines = new List<Quarantine>() },
                new Ranking { RegionId = 3, RankingId = 1, BrokerId = 1, Rank = 1, Quarantines = new [] {
                        new Quarantine {
                            QuarantineId = 7,
                            ActiveFrom =  new DateTimeOffset(2018,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            ActiveTo =  new DateTimeOffset(2019,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            CustomerOrganisationId = 2,
                            Motivation = "active Quarantine for cust 2, on first rank"
                        }
                    }.ToList()
                },
                new Ranking { RegionId = 3, RankingId = 2, BrokerId = 2, Rank = 2, Quarantines = new [] {
                        new Quarantine {
                            QuarantineId = 7,
                            ActiveFrom =  new DateTimeOffset(2018,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            ActiveTo =  new DateTimeOffset(2019,05,07, 0,0,0, new TimeSpan(02,00,00)),
                            CustomerOrganisationId = 2,
                            Motivation = "active Quarantine for cust 2, on second and last rank"
                        }
                    }.ToList()
                },
            };

        public static CustomerUnit[] MockUnits => new[]
            {
                new CustomerUnit { CustomerUnitId = 1, CustomerOrganisationId = 4, Email = string.Empty, Name = string.Empty },
                new CustomerUnit { CustomerUnitId = 2, CustomerOrganisationId = 4, Email = string.Empty, Name = string.Empty },
            };
        public static RequisitionPriceRow[] MockRequisitionPriceRows => new[]
            {
                new RequisitionPriceRow { RequisitionId = 1, StartAt = new DateTime(2020,1,1,12,0,0), EndAt = new DateTime(2020,1,1,13,0,0), Price = 1000, PriceRowType = PriceRowType.InterpreterCompensation, Quantity = 1 },
                new RequisitionPriceRow { RequisitionId = 1, Price = 10, PriceRowType = PriceRowType.AdministrativeCharge, Quantity = 1 },
                new RequisitionPriceRow { RequisitionId = 1, Price = (decimal)20.34, PriceRowType = PriceRowType.SocialInsuranceCharge, Quantity = 1 },
                new RequisitionPriceRow { RequisitionId = 1, Price = (decimal)0.25, PriceRowType = PriceRowType.RoundedPrice, Quantity = 1 },
                new RequisitionPriceRow { RequisitionId = 1, Price = 100, PriceRowType = PriceRowType.BrokerFee, Quantity = 1 },
                new RequisitionPriceRow { RequisitionId = 1, Price = 250, PriceRowType = PriceRowType.Outlay, Quantity = 1 },
                new RequisitionPriceRow { RequisitionId = 1, Price = 175, PriceRowType = PriceRowType.TravelCost, Quantity = 1  },
            };
        public static RequestPriceRow[] MockRequestPriceRows => new[]
            {
                new RequestPriceRow { RequestId = 1, StartAt = new DateTime(2020,1,1,12,0,0), EndAt = new DateTime(2020,1,1,13,0,0), Price = 1000, PriceRowType = PriceRowType.InterpreterCompensation, Quantity = 1 },
                new RequestPriceRow { RequestId = 1, Price = 10, PriceRowType = PriceRowType.AdministrativeCharge, Quantity = 1 },
                new RequestPriceRow { RequestId = 1, Price = (decimal)20.34, PriceRowType = PriceRowType.SocialInsuranceCharge, Quantity = 1 },
                new RequestPriceRow { RequestId = 1, Price = (decimal)0.25, PriceRowType = PriceRowType.RoundedPrice, Quantity = 1 },
                new RequestPriceRow { RequestId = 1, Price = 100, PriceRowType = PriceRowType.BrokerFee, Quantity = 1 },
            };

        public static RequestPriceRow[] MockRequestPriceRowsTwoBrokerFees => new[]
            {
                new RequestPriceRow { RequestId = 1, StartAt = new DateTime(2020,1,1,23,0,0), EndAt = new DateTime(2020,1,2,1,0,0), Price = 3000, PriceRowType = PriceRowType.InterpreterCompensation, Quantity = 1 },
                new RequestPriceRow { RequestId = 1, Price = 10, PriceRowType = PriceRowType.AdministrativeCharge, Quantity = 1 },
                new RequestPriceRow { RequestId = 1, Price = (decimal)20.34, PriceRowType = PriceRowType.SocialInsuranceCharge, Quantity = 1 },
                new RequestPriceRow { RequestId = 1, Price = (decimal)0.25, PriceRowType = PriceRowType.RoundedPrice, Quantity = 1 },
                new RequestPriceRow { RequestId = 1, Price = 100, PriceRowType = PriceRowType.BrokerFee, Quantity = 2 },
            };

        public static Order[] MockOrders(Language[] mockLanguages, Ranking[] mockRankings, AspNetUser[] mockCustomerUsers)
        {
            var orders = new[]
            {
                new Order(mockCustomerUsers[0], null, mockCustomerUsers[0].CustomerOrganisation, new DateTimeOffset(2018,05,07,13,00,00, new TimeSpan(02,00,00)))
                {
                    OrderId = 1,
                    CreatedBy = mockCustomerUsers[0].Id,
                    ContactPersonId = mockCustomerUsers[3].Id,
                    CustomerOrganisationId = mockCustomerUsers[0].CustomerOrganisation.CustomerOrganisationId,
                    CustomerReferenceNumber = "Number1",
                    OrderNumber = "2018-001337",
                    StartAt = new DateTimeOffset(2018,06,07,13,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,06,07,15,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Stockholm").Single(),
                    Language = mockLanguages.Where(l => l.Name == "English").Single(),
                    Status = OrderStatus.RequestRespondedNewInterpreter,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,05,26,14,56,00, new TimeSpan(02,00,00)), new DateTimeOffset(2018,04,26,14,56,00, new TimeSpan(02,00,00))),
                        new Request(mockRankings[1], new DateTimeOffset(2018,06,02,14,11,00, new TimeSpan(02,00,00)), new DateTimeOffset(2018,04,02,14,11,00, new TimeSpan(02,00,00))),
                    },
                },
                new Order(mockCustomerUsers[0], null, mockCustomerUsers[0].CustomerOrganisation, new DateTimeOffset(2018,05,07,13,00,00, new TimeSpan(02,00,00)))
                {
                    OrderId = 2,
                    CreatedBy = mockCustomerUsers[0].Id,
                    CustomerOrganisationId = mockCustomerUsers[0].CustomerOrganisation.CustomerOrganisationId,
                    CustomerReferenceNumber = "Number2",
                    OrderNumber = "2018-000066", // execute order 66...
                    StartAt = new DateTimeOffset(2018,07,07,08,30,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,07,07,17,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Stockholm").Single(),
                    Language = mockLanguages.Where(l => l.Name == "German").Single(),
                    Status = OrderStatus.Delivered,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,06,26,14,56,00, new TimeSpan(02,00,00)), new DateTimeOffset(2018,05,26,14,56,00, new TimeSpan(02,00,00))),
                    },
                },
                new Order(mockCustomerUsers[1], null, mockCustomerUsers[1].CustomerOrganisation, new DateTimeOffset(2018,05,07,13,00,00, new TimeSpan(02,00,00)))
                {
                    OrderId = 3,
                    CreatedBy = mockCustomerUsers[1].Id,
                    CustomerOrganisationId = mockCustomerUsers[1].CustomerOrganisation.CustomerOrganisationId,
                    CustomerReferenceNumber = "Number3",
                    OrderNumber = "2018-000042",
                    StartAt = new DateTimeOffset(2018,08,07,13,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,08,07,14,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Skåne").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    Status = OrderStatus.Requested,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,07,29,14,56,00, new TimeSpan(02,00,00)), new DateTimeOffset(2018,06,26,14,56,00, new TimeSpan(02,00,00))),
                    }
                },

                new Order(mockCustomerUsers[1], null, mockCustomerUsers[1].CustomerOrganisation, new DateTimeOffset(2018,05,07,13,00,00, new TimeSpan(02,00,00)))
                {
                    OrderId = 4,
                    CreatedBy = mockCustomerUsers[1].Id,
                    CustomerOrganisationId = mockCustomerUsers[1].CustomerOrganisation.CustomerOrganisationId,
                    CustomerReferenceNumber = "Number4",
                    OrderNumber = "2018-000654",
                    StartAt = new DateTimeOffset(2018,09,03,13,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,09,03,19,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Västra Götaland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "Chinese").Single(),
                    Status = OrderStatus.Delivered,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,09,01,14,56,00, new TimeSpan(02,00,00)), new DateTimeOffset(2018,06,26,14,56,00, new TimeSpan(02,00,00))),
                    }
                },
                new Order(mockCustomerUsers[1], null, mockCustomerUsers[1].CustomerOrganisation, new DateTimeOffset(2018,05,07,13,00,00, new TimeSpan(02,00,00)))
                {
                    OrderId = 5,
                    CreatedBy = mockCustomerUsers[1].Id,
                    CustomerOrganisationId = mockCustomerUsers[1].CustomerOrganisation.CustomerOrganisationId,
                    CustomerReferenceNumber = "Number5",
                    OrderNumber = "2018-000330",
                    StartAt = new DateTimeOffset(2018,09,18,09,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,09,18,13,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Västra Götaland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "German").Single(),
                    Status = OrderStatus.RequestResponded,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,09,15,14,56,00, new TimeSpan(02,00,00)), new DateTimeOffset(2018,06,26,14,56,00, new TimeSpan(02,00,00))),
                    }
                },
                new Order(mockCustomerUsers[1], null, mockCustomerUsers[1].CustomerOrganisation, new DateTimeOffset(2018,05,07,13,00,00, new TimeSpan(02,00,00)))
                {
                    OrderId = 6,
                    CreatedBy = mockCustomerUsers[1].Id,
                    CustomerReferenceNumber = "Number6",
                    CustomerOrganisationId = mockCustomerUsers[1].CustomerOrganisation.CustomerOrganisationId,
                    OrderNumber = "2018-000501",
                    StartAt = new DateTimeOffset(2018,10,09,10,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,10,09,15,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Gotland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    Status = OrderStatus.Delivered,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,09,15,14,56,00, new TimeSpan(02,00,00)), new DateTimeOffset(2018,06,26,14,56,00, new TimeSpan(02,00,00))),
                        new Request(mockRankings[1], new DateTimeOffset(2018,10,02,14,56,00, new TimeSpan(02,00,00)), new DateTimeOffset(2018,06,26,14,56,00, new TimeSpan(02,00,00))),
                    }
                },
                new Order(mockCustomerUsers[2], null, mockCustomerUsers[2].CustomerOrganisation, new DateTimeOffset(2018,05,07,13,00,00, new TimeSpan(02,00,00)))
                {
                    OrderId = 7,
                    CreatedBy = mockCustomerUsers[2].Id,
                    CustomerOrganisationId = mockCustomerUsers[2].CustomerOrganisation.CustomerOrganisationId,
                    CustomerReferenceNumber = "Number7",
                    OrderNumber = "2018-000006",
                    StartAt = new DateTimeOffset(2018,09,03,00,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,09,03,19,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Västra Götaland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "Chinese").Single(),
                    Status = OrderStatus.CancelledByCreator,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,08,25,14,56,00, new TimeSpan(02,00,00)), new DateTimeOffset(2018,06,26,14,56,00, new TimeSpan(02,00,00))),
                    }
                },
                new Order(mockCustomerUsers[2], null, mockCustomerUsers[2].CustomerOrganisation, new DateTimeOffset(2018,05,07,13,00,00, new TimeSpan(02,00,00)))
                {
                    OrderId = 8,
                    CreatedBy = mockCustomerUsers[2].Id,
                    CustomerOrganisationId = mockCustomerUsers[2].CustomerOrganisation.CustomerOrganisationId,
                    CustomerReferenceNumber = "Number8",
                    OrderNumber = "2018-000007",
                    StartAt = new DateTimeOffset(2018,08,15,00,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,08,15,19,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Uppsala").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    Status = OrderStatus.Delivered,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,08,01,14,56,00, new TimeSpan(02,00,00)), new DateTimeOffset(2018,06,26,14,56,00, new TimeSpan(02,00,00))),
                    }
                },
                new Order(mockCustomerUsers[2], null, mockCustomerUsers[2].CustomerOrganisation, new DateTimeOffset(2018,05,07,13,00,00, new TimeSpan(02,00,00)))
                {
                    OrderId = 9,
                    CreatedBy = mockCustomerUsers[2].Id,
                    CustomerOrganisationId = mockCustomerUsers[2].CustomerOrganisation.CustomerOrganisationId,
                    CustomerReferenceNumber = "EmptyOrder",
                    OrderNumber = "2018-000008",
                    Region = Region.Regions.Where(r => r.Name == "Uppsala").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    Status = OrderStatus.Requested,
                    Requests = new List<Request>(),
                    InterpreterLocations = new List<OrderInterpreterLocation>() { new OrderInterpreterLocation {InterpreterLocation = InterpreterLocation.OffSitePhone, OffSiteContactInformation = "0000" } }
                },
                new Order(mockCustomerUsers[3], null, mockCustomerUsers[3].CustomerOrganisation, new DateTimeOffset(2018,05,07,13,00,00, new TimeSpan(02,00,00)))
                {
                    OrderId = 10,
                    CreatedBy = mockCustomerUsers[3].Id,
                    CustomerOrganisationId = mockCustomerUsers[3].CustomerOrganisation.CustomerOrganisationId,
                    CustomerReferenceNumber = "EmptyOrder",
                    OrderNumber = "2018-000009",
                    Region = Region.Regions.Where(r => r.Name == "Uppsala").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    Status = OrderStatus.Requested,
                    Requests = new List<Request>(),
                    InterpreterLocations = new List<OrderInterpreterLocation>() { new OrderInterpreterLocation {InterpreterLocation = InterpreterLocation.OffSitePhone, OffSiteContactInformation = "0000" } }
                },

                new Order(mockCustomerUsers[4], null, mockCustomerUsers[4].CustomerOrganisation, new DateTimeOffset(2018,05,07,13,00,00, new TimeSpan(02,00,00)))
                {
                    OrderId = 11,
                    CreatedBy = mockCustomerUsers[4].Id,
                    CustomerOrganisationId = mockCustomerUsers[4].CustomerOrganisation.CustomerOrganisationId,
                    OrderNumber = "2018-000010",
                    Region = Region.Regions.Where(r => r.Name == "Uppsala").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    Status = OrderStatus.Requested,
                    Requests = new List<Request>(),
                    CustomerUnit = MockUnits[0],
                    InterpreterLocations = new List<OrderInterpreterLocation>() { new OrderInterpreterLocation {InterpreterLocation = InterpreterLocation.OffSitePhone, OffSiteContactInformation = "0000" } }
                },
                new Order(mockCustomerUsers[4], null, mockCustomerUsers[4].CustomerOrganisation, new DateTimeOffset(2018,05,07,13,00,00, new TimeSpan(02,00,00)))
                {
                    OrderId = 12,
                    CreatedBy = mockCustomerUsers[4].Id,
                    CustomerOrganisationId = mockCustomerUsers[4].CustomerOrganisation.CustomerOrganisationId,
                    OrderNumber = "2018-000011",
                    Region = Region.Regions.Where(r => r.Name == "Uppsala").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    CustomerUnit = MockUnits[1],
                    Status = OrderStatus.Requested,
                    Requests = new List<Request>(),
                    InterpreterLocations = new List<OrderInterpreterLocation>() { new OrderInterpreterLocation {InterpreterLocation = InterpreterLocation.OffSitePhone, OffSiteContactInformation = "0000" } }
                },
                new Order(mockCustomerUsers[4], null, mockCustomerUsers[4].CustomerOrganisation, new DateTimeOffset(2018,05,07,13,00,00, new TimeSpan(02,00,00)))
                {
                    OrderId = 13,
                    CreatedBy = mockCustomerUsers[4].Id,
                    CustomerOrganisationId = mockCustomerUsers[4].CustomerOrganisation.CustomerOrganisationId,
                    OrderNumber = "2018-000012",
                    Region = Region.Regions.Where(r => r.Name == "Uppsala").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    Status = OrderStatus.Requested,
                    Requests = new List<Request>(),
                    InterpreterLocations = new List<OrderInterpreterLocation>() { new OrderInterpreterLocation {InterpreterLocation = InterpreterLocation.OffSitePhone, OffSiteContactInformation = "0000" } }
                },
            };

            // Set required properties
            foreach (Order o in orders)
            {
                o.LanguageId = o.Language.LanguageId;
                o.RegionId = o.Region.RegionId;
                o.CustomerUnitId = o.CustomerUnit?.CustomerUnitId;
                foreach (Request r in o.Requests)
                {
                    r.Order = o;
                }
            }

            return orders;
        }
        public static OrderListRow[] MockOrderListRows(IEnumerable<Order> orders)
        {
            return orders.Select(o =>
                new OrderListRow
                {
                    CustomerOrganisationId = o.CustomerOrganisationId,
                    RegionId = o.RegionId,
                    CustomerReferenceNumber = o.CustomerReferenceNumber,
                    LanguageId = o.LanguageId,
                    StartAt = o.StartAt,
                    EndAt = o.EndAt,
                    EntityNumber = o.OrderNumber,
                    Status = o.Status,
                    BrokerId = o.Requests.LastOrDefault(r =>
                                        r.Status != RequestStatus.InterpreterReplaced &&
                                        r.Status != RequestStatus.DeniedByTimeLimit &&
                                        r.Status != RequestStatus.DeniedByCreator &&
                                        r.Status != RequestStatus.DeclinedByBroker &&
                                        r.Status != RequestStatus.LostDueToQuarantine)?.Ranking.BrokerId
                }).ToArray();
        }

        public static OrderGroup[] MockOrderGroups(Language[] mockLanguages, Ranking[] mockRankings, AspNetUser[] mockCustomerUsers)
        {
            var baseDate = new DateTimeOffset(2018, 06, 07, 13, 00, 00, new TimeSpan(02, 00, 00));
            return new[]
            {
                CreateOrderGroup(
                    "JUSTCREATED",
                    mockCustomerUsers[0],
                    1,
                    baseDate,
                    Region.Regions.Where(r => r.Name == "Stockholm").Single(),
                    mockLanguages.Where(l => l.Name == "English").Single(),
                    OrderStatus.Requested,
                    null,
                    CreateOrders( mockCustomerUsers[0], new List<int>(){ 1,2,3}, baseDate, Region.Regions.Where(r => r.Name == "Stockholm").Single(), mockLanguages.Where(l => l.Name == "English").Single(), OrderStatus.Requested, null ).ToList(),
                    mockRankings,
                    Enumerable.Empty<RequestStatus>().ToList()
                ),
                CreateOrderGroup(
                    "REQUESTSJUSTCREATED",
                    mockCustomerUsers[0],
                    1,
                    baseDate,
                    Region.Regions.Where(r => r.Name == "Stockholm").Single(),
                    mockLanguages.Where(l => l.Name == "English").Single(),
                    OrderStatus.Requested,
                    null,
                    CreateOrders( mockCustomerUsers[0], new List<int>(){ 1,2,3}, baseDate, Region.Regions.Where(r => r.Name == "Stockholm").Single(), mockLanguages.Where(l => l.Name == "English").Single(), OrderStatus.Requested, null ).ToList(),
                    mockRankings,
                    new List<RequestStatus>(){ RequestStatus.Created }
                ),
                CreateOrderGroup(
                    "REQUESTGROUPAWAITINGAPPROVAL",
                    mockCustomerUsers[0],
                    1,
                    baseDate,
                    Region.Regions.Where(r => r.Name == "Stockholm").Single(),
                    mockLanguages.Where(l => l.Name == "English").Single(),
                    OrderStatus.Requested,
                    null,
                    CreateOrders( mockCustomerUsers[0], new List<int>(){ 1,2,3}, baseDate, Region.Regions.Where(r => r.Name == "Stockholm").Single(), mockLanguages.Where(l => l.Name == "English").Single(), OrderStatus.Requested, null ).ToList(),
                    mockRankings,
                    new List<RequestStatus>(){ RequestStatus.Accepted },
                    AllowExceedingTravelCost.YesShouldBeApproved
                ),
                CreateOrderGroup(
                    "REQUESTGROUPALLOWEXCEEDINGJUSTCREATED",
                    mockCustomerUsers[0],
                    1,
                    baseDate,
                    Region.Regions.Where(r => r.Name == "Stockholm").Single(),
                    mockLanguages.Where(l => l.Name == "English").Single(),
                    OrderStatus.Requested,
                    null,
                    CreateOrders( mockCustomerUsers[0], new List<int>(){ 1,2,3}, baseDate, Region.Regions.Where(r => r.Name == "Stockholm").Single(), mockLanguages.Where(l => l.Name == "English").Single(), OrderStatus.Requested, null,
                    new List<OrderInterpreterLocation>() { new OrderInterpreterLocation { InterpreterLocation = InterpreterLocation.OnSite }, new OrderInterpreterLocation { InterpreterLocation = InterpreterLocation.OffSiteDesignatedLocation } }).ToList(),
                    mockRankings,
                    new List<RequestStatus>(){ RequestStatus.Created },
                    AllowExceedingTravelCost.YesShouldBeApproved,
                    new List<OrderGroupInterpreterLocation>() { new OrderGroupInterpreterLocation { InterpreterLocation = InterpreterLocation.OnSite }, new OrderGroupInterpreterLocation { InterpreterLocation = InterpreterLocation.OffSiteDesignatedLocation } }

                ),
                CreateOrderGroup(
                    "REQUESTGROUPDENIED",
                    mockCustomerUsers[0],
                    1,
                    baseDate,
                    Region.Regions.Where(r => r.Name == "Stockholm").Single(),
                    mockLanguages.Where(l => l.Name == "English").Single(),
                    OrderStatus.Requested,
                    null,
                    CreateOrders( mockCustomerUsers[0], new List<int>(){ 1,2,3}, baseDate, Region.Regions.Where(r => r.Name == "Stockholm").Single(), mockLanguages.Where(l => l.Name == "English").Single(), OrderStatus.Requested, null ).ToList(),
                    mockRankings,
                    new List<RequestStatus>(){ RequestStatus.DeniedByCreator }
                ),
                CreateOrderGroup(
                    "REQUESTGROUPNOANSWERFROMCUSTOMER",
                    mockCustomerUsers[0],
                    1,
                    baseDate,
                    Region.Regions.Where(r => r.Name == "Stockholm").Single(),
                    mockLanguages.Where(l => l.Name == "English").Single(),
                    OrderStatus.Requested,
                    null,
                    CreateOrders( mockCustomerUsers[0], new List<int>(){ 1,2,3}, baseDate, Region.Regions.Where(r => r.Name == "Stockholm").Single(), mockLanguages.Where(l => l.Name == "English").Single(), OrderStatus.Requested, null ).ToList(),
                    mockRankings,
                    new List<RequestStatus>(){ RequestStatus.ResponseNotAnsweredByCreator }
                ),
            };
        }

        public static Request[] GetRequestsFromOrders(Order[] mockOrders)
        {
            List<Request> mockRequests = new List<Request>();
            foreach (Order o in mockOrders)
            {
                mockRequests.AddRange(o.Requests);
            }
            return mockRequests.ToArray();
        }

        public static RequestListRow[] MockRequests(Order[] mockOrders)
        {
            return new[]
            {
                new RequestListRow
                {
                    CustomerOrganisationId = mockOrders[3].CustomerOrganisationId,
                    RegionId = mockOrders[3].RegionId,
                    CustomerReferenceNumber = mockOrders[3].CustomerReferenceNumber,
                    LanguageId = mockOrders[3].LanguageId,
                    StartAt = mockOrders[3].StartAt,
                    EndAt = mockOrders[3].EndAt,
                    ExpiresAt = mockOrders[3].StartAt.AddDays(-10),
                    EntityNumber = mockOrders[3].OrderNumber,
                    Status = RequestStatus.Accepted
                },
                new RequestListRow
                {
                    CustomerOrganisationId = mockOrders[0].CustomerOrganisationId,
                    RegionId = mockOrders[0].RegionId,
                    CustomerReferenceNumber = mockOrders[0].CustomerReferenceNumber,
                    LanguageId = mockOrders[0].LanguageId,
                    StartAt = mockOrders[0].StartAt,
                    EndAt = mockOrders[0].EndAt,
                    EntityNumber = mockOrders[0].OrderNumber,
                    ExpiresAt = mockOrders[0].StartAt.AddDays(-10),
                    Status = RequestStatus.InterpreterReplaced
                },
                new RequestListRow
                {
                    CustomerOrganisationId = mockOrders[1].CustomerOrganisationId,
                    RegionId = mockOrders[1].RegionId,
                    CustomerReferenceNumber = mockOrders[1].CustomerReferenceNumber,
                    LanguageId = mockOrders[1].LanguageId,
                    StartAt = mockOrders[1].StartAt,
                    EndAt = mockOrders[1].EndAt,
                    EntityNumber = mockOrders[1].OrderNumber,
                    ExpiresAt = mockOrders[1].StartAt.AddDays(-3d),
                    Status = RequestStatus.AcceptedNewInterpreterAppointed
                },
                new RequestListRow
                {
                    CustomerOrganisationId = mockOrders[2].CustomerOrganisationId,
                    RegionId = mockOrders[2].RegionId,
                    CustomerReferenceNumber = mockOrders[2].CustomerReferenceNumber,
                    LanguageId = mockOrders[2].LanguageId,
                    StartAt = mockOrders[2].StartAt,
                    EndAt = mockOrders[2].EndAt,
                    EntityNumber = mockOrders[2].OrderNumber,
                    ExpiresAt = mockOrders[2].StartAt.AddDays(-10),
                    Status = RequestStatus.ToBeProcessedByBroker
                },
                new RequestListRow
                {
                    CustomerOrganisationId = mockOrders[4].CustomerOrganisationId,
                    RegionId = mockOrders[4].RegionId,
                    CustomerReferenceNumber = mockOrders[4].CustomerReferenceNumber,
                    LanguageId = mockOrders[4].LanguageId,
                    StartAt = mockOrders[4].StartAt,
                    EndAt = mockOrders[4].EndAt,
                    EntityNumber = mockOrders[4].OrderNumber,
                    ExpiresAt = mockOrders[4].StartAt.AddDays(-10),
                    Status = RequestStatus.Approved
                },
                new RequestListRow
                {
                    CustomerOrganisationId = mockOrders[5].CustomerOrganisationId,
                    RegionId = mockOrders[5].RegionId,
                    CustomerReferenceNumber = mockOrders[5].CustomerReferenceNumber,
                    LanguageId = mockOrders[5].LanguageId,
                    StartAt = mockOrders[5].StartAt,
                    EndAt = mockOrders[5].EndAt,
                    EntityNumber = mockOrders[5].OrderNumber,
                    ExpiresAt = mockOrders[5].StartAt.AddDays(-10),
                    Status = RequestStatus.Accepted
                },
            };
        }

        public static Requisition[] MockRequisitions(Order[] orders)
        {
            var requisitions = new List<Requisition>
            {
                new Requisition
                {
                    RequisitionId = 1,
                    Status = RequisitionStatus.Commented,
                    Request = orders[0].Requests[1],
                    Message = string.Empty
                },
                new Requisition
                {
                    RequisitionId = 2,
                    Status = RequisitionStatus.Reviewed,
                    Request = orders[1].Requests[0],
                    Message = string.Empty
                },
                new Requisition
                {
                    RequisitionId = 3,
                    Status = RequisitionStatus.Reviewed,
                    Request = orders[3].Requests[0],
                    Message = string.Empty
                },
                new Requisition
                {
                    RequisitionId = 4,
                    Status = RequisitionStatus.Created,
                    Request = orders[5].Requests[1],
                    Message = string.Empty
                },
            };

            foreach (Requisition r in requisitions)
            {
                r.Request.Requisitions ??= new List<Requisition>();

                r.Request.Requisitions.Add(r);
            }

            return requisitions.ToArray();
        }

        public static Complaint[] MockComplaints(Order[] orders)
        {
            var complaints = new List<Complaint>
            {
                new Complaint
                {
                    ComplaintId = 1,
                    Status = ComplaintStatus.Created,
                    Request = orders[0].Requests[1],
                    ComplaintMessage = string.Empty
                },

                new Complaint
                {
                    ComplaintId = 2,
                    ComplaintMessage = string.Empty,
                    Status = ComplaintStatus.Created,
                    Request = orders[1].Requests[0]
                },

                new Complaint
                {
                    ComplaintId = 3,
                    ComplaintMessage = string.Empty,
                    Status = ComplaintStatus.Created,
                    Request = orders[3].Requests[0]
                },

                new Complaint
                {
                    ComplaintId = 4,
                    ComplaintMessage = string.Empty,
                    Status = ComplaintStatus.Created,
                    Request = orders[5].Requests[1]
                },

            };

            foreach (Complaint c in complaints)
            {
                c.Request.Complaints ??= new List<Complaint>();
                c.Request.Complaints.Add(c);
            }

            return complaints.ToArray();
        }

        public static Order[] LinkRequisitionsInOrdersRequests(Order[] orders, Requisition[] requisitions)
        {
            foreach (Order o in orders)
            {
                foreach (Request r in o.Requests)
                {
                    r.Requisitions ??= new List<Requisition>();
                    r.Requisitions = requisitions.Where(req => req.Request == r).ToList();
                }
            }

            return orders;
        }

        public static Holiday[] Holidays => new[] {
            new Holiday() { Name = "", Date = new DateTime(2018, 03, 29), DateType = DateType.DayBeforeBigHoliday},
            new Holiday() { Name = "", Date = new DateTime(2018, 03, 30), DateType = DateType.BigHolidayFullDay},
            new Holiday() { Name = "", Date = new DateTime(2018, 04, 01), DateType = DateType.BigHolidayFullDay},
            new Holiday() { Name = "", Date = new DateTime(2018, 04, 02), DateType = DateType.BigHolidayFullDay},
            new Holiday() { Name = "", Date = new DateTime(2018, 04, 03), DateType = DateType.DayAfterBigHoliday},
            new Holiday() { Name = "", Date = new DateTime(2018, 05, 01), DateType = DateType.Holiday},
            new Holiday() { Name = "", Date = new DateTime(2018, 05, 10), DateType = DateType.Holiday},
            new Holiday() { Name = "", Date = new DateTime(2018, 05, 18), DateType = DateType.DayBeforeBigHoliday},
            new Holiday() { Name = "", Date = new DateTime(2018, 05, 19), DateType = DateType.BigHolidayFullDay},
            new Holiday() { Name = "", Date = new DateTime(2018, 06, 06), DateType = DateType.Holiday},
            new Holiday() { Name = "", Date = new DateTime(2018, 12, 23), DateType = DateType.DayBeforeBigHoliday},
            new Holiday() { Name = "", Date = new DateTime(2018, 12, 24), DateType = DateType.BigHolidayFullDay},
            new Holiday() { Name = "", Date = new DateTime(2018, 12, 25), DateType = DateType.BigHolidayFullDay},
            new Holiday() { Name = "", Date = new DateTime(2018, 12, 26), DateType = DateType.BigHolidayFullDay},
            new Holiday() { Name = "", Date = new DateTime(2018, 12, 27), DateType = DateType.DayAfterBigHoliday},
            new Holiday() { Name = "", Date = new DateTime(2020, 06, 06), DateType = DateType.Holiday},
            new Holiday() { Name = "", Date = new DateTime(2019, 12, 23), DateType = DateType.DayBeforeBigHoliday},
            new Holiday() { Name = "", Date = new DateTime(2020, 12, 27), DateType = DateType.DayAfterBigHoliday},
            new Holiday() { Name = "", Date = new DateTime(2022, 06, 06), DateType = DateType.DayAfterBigHoliday},
            new Holiday() { Name = "", Date = new DateTime(2022, 06, 06), DateType = DateType.Holiday},
            new Holiday() { Name = "", Date = new DateTime(2025, 06, 06), DateType = DateType.DayBeforeBigHoliday},
            new Holiday() { Name = "", Date = new DateTime(2025, 06, 06), DateType = DateType.Holiday}
        };

        public static PriceCalculationCharge[] PriceCalculationCharges => new[] {
            new PriceCalculationCharge() { PriceCalculationChargeId = 1, ChargePercentage =  (decimal)31.42, ChargeTypeId = ChargeType.SocialInsuranceCharge, StartDate = new DateTime(2018,01,01), EndDate =  new DateTime(2099,01,01)},
            new PriceCalculationCharge() { PriceCalculationChargeId = 2, ChargePercentage =  (decimal)0.7, ChargeTypeId = ChargeType.AdministrativeCharge, StartDate = new DateTime(2018,01,01), EndDate =  new DateTime(2099,01,01)},
        };
        public static BrokerFeeByServiceTypePriceListRow[] BrokerFeeByServiceTypePriceListRows
        {
            get
            {
                var list = new[] {
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 1,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.OtherInterpreter,
                        InterpreterLocation = InterpreterLocation.OnSite,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 111
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 2,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.EducatedInterpreter,
                        InterpreterLocation = InterpreterLocation.OnSite,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 211
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 3,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.AuthorizedInterpreter,
                        InterpreterLocation = InterpreterLocation.OnSite,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 311
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 4,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.SpecializedInterpreter,
                        InterpreterLocation = InterpreterLocation.OnSite,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 411
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 5,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.OtherInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteDesignatedLocation,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 141
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 6,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.EducatedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteDesignatedLocation,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 241
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 7,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.AuthorizedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteDesignatedLocation,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 341
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 8,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.SpecializedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteDesignatedLocation,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 441
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 9,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.OtherInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSitePhone,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 121
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 10,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.EducatedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSitePhone,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 221
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 11,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.AuthorizedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSitePhone,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 321
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 12,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.SpecializedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSitePhone,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 421
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 13,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.OtherInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteVideo,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 131
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 14,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.EducatedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteVideo,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 231
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 15,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.AuthorizedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteVideo,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 331
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 16,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.SpecializedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteVideo,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 1 ),
                        Price = 431
                    },
                                        new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 17,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.OtherInterpreter,
                        InterpreterLocation = InterpreterLocation.OnSite,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 112
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 18,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.EducatedInterpreter,
                        InterpreterLocation = InterpreterLocation.OnSite,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 212
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 19,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.AuthorizedInterpreter,
                        InterpreterLocation = InterpreterLocation.OnSite,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 312
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 20,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.SpecializedInterpreter,
                        InterpreterLocation = InterpreterLocation.OnSite,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 412
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 21,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.OtherInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteDesignatedLocation,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 142
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 22,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.EducatedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteDesignatedLocation,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 242
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 23,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.AuthorizedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteDesignatedLocation,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 342
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 24,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.SpecializedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteDesignatedLocation,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 442
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 25,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.OtherInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSitePhone,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 122
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 26,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.EducatedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSitePhone,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 222
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 27,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.AuthorizedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSitePhone,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 322
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 28,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.SpecializedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSitePhone,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 422
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 29,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.OtherInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteVideo,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 132
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 30,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.EducatedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteVideo,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 232
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 31,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.AuthorizedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteVideo,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 332
                    },
                    new BrokerFeeByServiceTypePriceListRow {
                        BrokerFeeByServiceTypePriceListRowId = 32,
                        FirstValidDate = new DateTime(2018, 01, 01),
                        LastValidDate = new DateTime(2099, 12, 31),
                        CompetenceLevel = CompetenceLevel.SpecializedInterpreter,
                        InterpreterLocation = InterpreterLocation.OffSiteVideo,
                        RegionGroup = RegionGroup.RegionGroups.Single(g => g.RegionGroupId == 2 ),
                        Price = 432
                    },
                };

                return list;
            }
        }
        public static FrameworkAgreement[] FrameworkAgreements => new[] {
            new FrameworkAgreement { FrameworkAgreementId = 1, AgreementNumber= "1234", Description = "", FirstValidDate = new DateTime(2016, 01, 01), LastValidDate = new DateTime(2099, 06, 01), BrokerFeeCalculationType = BrokerFeeCalculationType.ByRegionAndBroker, FrameworkAgreementResponseRuleset = FrameworkAgreementResponseRuleset.VersionOne },
        };

        public static Ranking[] RankingsWithContractEnded => new[] {
            new Ranking { RankingId = 1, Rank = 1, FirstValidDate = new DateTime(2018, 01, 01), LastValidDate = new DateTime(2018, 06, 01), BrokerFee = (decimal)0.1, BrokerId = 1, RegionId = 1 },
            new Ranking { RankingId = 2, Rank = 2, FirstValidDate = new DateTime(2018, 01, 01), LastValidDate = new DateTime(2018, 06, 01), BrokerFee = (decimal)0.1, BrokerId = 2, RegionId = 1 },
            new Ranking { RankingId = 3, Rank = 1, FirstValidDate = new DateTime(2018, 06, 02), LastValidDate = new DateTime(2099, 01, 01), BrokerFee = (decimal)0.1, BrokerId = 2, RegionId = 1 }
        };

        public static PriceListRow[] PriceListRows => new[] {
            new PriceListRow() { PriceListRowId = 1001, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 352, MaxMinutes = 60, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1002, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 409, MaxMinutes = 60, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1003, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 480, MaxMinutes = 60, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1004, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 606, MaxMinutes = 60, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1005, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 479, MaxMinutes = 90, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1006, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 567, MaxMinutes = 90, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1007, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 665, MaxMinutes = 90, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1008, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 845, MaxMinutes = 90, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1009, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 606, MaxMinutes = 120, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1010, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 725, MaxMinutes = 120, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1011, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 850, MaxMinutes = 120, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1012, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1084, MaxMinutes = 120, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1013, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 733, MaxMinutes = 150, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1014, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 833, MaxMinutes = 150, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1015, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1035, MaxMinutes = 150, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1016, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1323, MaxMinutes = 150, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1017, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 860, MaxMinutes = 180, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1018, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1041, MaxMinutes = 180, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1019, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1220, MaxMinutes = 180, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1020, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1562, MaxMinutes = 180, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1021, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 987, MaxMinutes = 210, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1022, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1199, MaxMinutes = 210, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1023, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1405, MaxMinutes = 210, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1024, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1801, MaxMinutes = 210, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1025, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1114, MaxMinutes = 240, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1026, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1357, MaxMinutes = 240, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1027, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1590, MaxMinutes = 240, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1028, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 2040, MaxMinutes = 240, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1029, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1241, MaxMinutes = 270, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1030, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1515, MaxMinutes = 270, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1031, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1775, MaxMinutes = 270, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1032, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 2279, MaxMinutes = 270, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1033, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1368, MaxMinutes = 300, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1034, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1673, MaxMinutes = 300, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1035, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1960, MaxMinutes = 300, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1036, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 2518, MaxMinutes = 300, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1037, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1495, MaxMinutes = 330, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1038, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1831, MaxMinutes = 330, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1039, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 2145, MaxMinutes = 330, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1040, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 2757, MaxMinutes = 330, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1041, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 293, MaxMinutes = 60, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1042, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 340, MaxMinutes = 60, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1043, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 399, MaxMinutes = 60, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1044, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 511, MaxMinutes = 60, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1045, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 420, MaxMinutes = 90, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1046, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 492, MaxMinutes = 90, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1047, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 577, MaxMinutes = 90, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1048, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 738, MaxMinutes = 90, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1049, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 547, MaxMinutes = 120, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1050, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 644, MaxMinutes = 120, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1051, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 755, MaxMinutes = 120, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1052, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 965, MaxMinutes = 120, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1053, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 674, MaxMinutes = 150, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1054, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 796, MaxMinutes = 150, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1055, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 933, MaxMinutes = 150, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1056, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1192, MaxMinutes = 150, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1057, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 801, MaxMinutes = 180, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1058, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 948, MaxMinutes = 180, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1059, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1111, MaxMinutes = 180, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1060, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1419, MaxMinutes = 180, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1061, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 928, MaxMinutes = 210, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1062, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1100, MaxMinutes = 210, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1063, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1289, MaxMinutes = 210, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1064, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1646, MaxMinutes = 210, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1065, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1055, MaxMinutes = 240, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1066, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1252, MaxMinutes = 240, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1067, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1467, MaxMinutes = 240, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1068, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1873, MaxMinutes = 240, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1069, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1182, MaxMinutes = 270, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1070, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1404, MaxMinutes = 270, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1071, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1645, MaxMinutes = 270, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1072, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 2100, MaxMinutes = 270, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1073, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1309, MaxMinutes = 300, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1074, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1556, MaxMinutes = 300, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1075, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1823, MaxMinutes = 300, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1076, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 2327, MaxMinutes = 300, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1077, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1436, MaxMinutes = 330, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1078, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 1708, MaxMinutes = 330, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1079, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 2001, MaxMinutes = 330, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1080, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 2554, MaxMinutes = 330, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BasePrice },
            new PriceListRow() { PriceListRowId = 1081, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 127, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.PriceOverMaxTime },
            new PriceListRow() { PriceListRowId = 1082, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 158, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.PriceOverMaxTime },
            new PriceListRow() { PriceListRowId = 1083, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 185, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.PriceOverMaxTime },
            new PriceListRow() { PriceListRowId = 1084, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 239, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.PriceOverMaxTime },
            new PriceListRow() { PriceListRowId = 1085, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 127, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.PriceOverMaxTime },
            new PriceListRow() { PriceListRowId = 1086, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 152, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.PriceOverMaxTime },
            new PriceListRow() { PriceListRowId = 1087, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 178, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.PriceOverMaxTime },
            new PriceListRow() { PriceListRowId = 1088, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 227, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.PriceOverMaxTime },
            new PriceListRow() { PriceListRowId = 1089, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 77, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.InconvenientWorkingHours },
            new PriceListRow() { PriceListRowId = 1090, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 101, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.InconvenientWorkingHours },
            new PriceListRow() { PriceListRowId = 1091, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 119, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.InconvenientWorkingHours },
            new PriceListRow() { PriceListRowId = 1092, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 136, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.InconvenientWorkingHours },
            new PriceListRow() { PriceListRowId = 1093, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 77, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.InconvenientWorkingHours },
            new PriceListRow() { PriceListRowId = 1094, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 101, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.InconvenientWorkingHours },
            new PriceListRow() { PriceListRowId = 1095, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 119, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.InconvenientWorkingHours },
            new PriceListRow() { PriceListRowId = 1096, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 136, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.InconvenientWorkingHours },
            new PriceListRow() { PriceListRowId = 1097, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 127, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.WeekendIWH },
            new PriceListRow() { PriceListRowId = 1098, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 158, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.WeekendIWH },
            new PriceListRow() { PriceListRowId = 1099, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 185, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.WeekendIWH },
            new PriceListRow() { PriceListRowId = 1100, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 239, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.WeekendIWH },
            new PriceListRow() { PriceListRowId = 1101, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 127, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.WeekendIWH },
            new PriceListRow() { PriceListRowId = 1102, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 158, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.WeekendIWH },
            new PriceListRow() { PriceListRowId = 1103, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 185, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.WeekendIWH },
            new PriceListRow() { PriceListRowId = 1104, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 239, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.WeekendIWH },
            new PriceListRow() { PriceListRowId = 1105, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 154, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BigHolidayWeekendIWH },
            new PriceListRow() { PriceListRowId = 1106, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 202, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BigHolidayWeekendIWH },
            new PriceListRow() { PriceListRowId = 1107, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 238, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BigHolidayWeekendIWH },
            new PriceListRow() { PriceListRowId = 1108, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 272, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.BigHolidayWeekendIWH },
            new PriceListRow() { PriceListRowId = 1109, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 154, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BigHolidayWeekendIWH },
            new PriceListRow() { PriceListRowId = 1110, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 202, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BigHolidayWeekendIWH },
            new PriceListRow() { PriceListRowId = 1111, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 238, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BigHolidayWeekendIWH },
            new PriceListRow() { PriceListRowId = 1112, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 272, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.BigHolidayWeekendIWH },
            new PriceListRow() { PriceListRowId = 1113, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 191, MaxMinutes = 60, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.LostTime },
            new PriceListRow() { PriceListRowId = 1114, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 224, MaxMinutes = 60, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.LostTime },
            new PriceListRow() { PriceListRowId = 1115, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 264, MaxMinutes = 60, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.LostTime },
            new PriceListRow() { PriceListRowId = 1116, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 350, MaxMinutes = 60, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.LostTime },
            new PriceListRow() { PriceListRowId = 1117, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 191, MaxMinutes = 60, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.LostTime },
            new PriceListRow() { PriceListRowId = 1118, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 224, MaxMinutes = 60, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.LostTime },
            new PriceListRow() { PriceListRowId = 1119, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 264, MaxMinutes = 60, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.LostTime },
            new PriceListRow() { PriceListRowId = 1120, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 350, MaxMinutes = 60, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.LostTime },
            new PriceListRow() { PriceListRowId = 1121, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 77, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.LostTimeIWH },
            new PriceListRow() { PriceListRowId = 1122, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 101, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.LostTimeIWH },
            new PriceListRow() { PriceListRowId = 1123, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 119, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.LostTimeIWH },
            new PriceListRow() { PriceListRowId = 1124, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 136, MaxMinutes = 30, PriceListType = PriceListType.Court, PriceListRowType = PriceListRowType.LostTimeIWH },
            new PriceListRow() { PriceListRowId = 1125, CompetenceLevel = CompetenceLevel.OtherInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 77, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.LostTimeIWH },
            new PriceListRow() { PriceListRowId = 1126, CompetenceLevel = CompetenceLevel.EducatedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 101, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.LostTimeIWH },
            new PriceListRow() { PriceListRowId = 1127, CompetenceLevel = CompetenceLevel.AuthorizedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 119, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.LostTimeIWH },
            new PriceListRow() { PriceListRowId = 1128, CompetenceLevel = CompetenceLevel.SpecializedInterpreter, StartDate = new DateTime(2018,01,01), EndDate = new DateTime(2099,01,01), Price = 136, MaxMinutes = 30, PriceListType = PriceListType.Other, PriceListRowType = PriceListRowType.LostTimeIWH }
        };

        private static IEnumerable<Order> CreateOrders(AspNetUser createdBy, List<int> ids, DateTimeOffset createdAt, Region region, Language language, OrderStatus status, CustomerUnit unit, List<OrderInterpreterLocation> locations = null)
        {
            foreach (var id in ids)
            {
                yield return new Order(createdBy, null, createdBy.CustomerOrganisation, createdAt)
                {
                    OrderId = id,
                    CreatedBy = createdBy.Id,
                    CustomerOrganisationId = createdBy.CustomerOrganisation.CustomerOrganisationId,
                    OrderNumber = $"2018-1{id.ToString().PadLeft(5 - id.ToString().Length, '0')}",
                    StartAt = createdAt.AddDays(14),
                    EndAt = createdAt.AddDays(14).AddHours(2),
                    Region = region,
                    Language = language,
                    Status = status,
                    CustomerUnit = unit,
                    CustomerUnitId = unit?.CustomerUnitId,
                    InterpreterLocations = locations
                };
            }
        }

        private static OrderGroup CreateOrderGroup(string groupName, AspNetUser createdBy, int id, DateTimeOffset createdAt, Region region, Language language, OrderStatus status, CustomerUnit unit, List<Order> orders, Ranking[] mockRankings, List<RequestStatus> requestStatuses, AllowExceedingTravelCost allowExceedingTravelCost = AllowExceedingTravelCost.No, List<OrderGroupInterpreterLocation> locations = null)
        {
            int mocRank = 0;
            var requestGroups = new List<RequestGroup>();
            var orderGroup = new OrderGroup(createdBy, null, createdBy.CustomerOrganisation, createdAt, orders);
            foreach (var requestStatus in requestStatuses)
            {
                var requests = new List<Request>();
                foreach (Order order in orders)
                {
                    var request = new Request(mockRankings[mocRank], createdAt.AddDays(mocRank + 1), createdAt.AddDays(mocRank)) { Status = requestStatus, Order = order };
                    request.RequestStatusConfirmations = new List<RequestStatusConfirmation>();
                    requests.Add(request);
                    order.Requests.Add(request);
                    order.OrderGroupId = id;
                    order.Group = orderGroup;
                }
                var requestGroup = new RequestGroup(mockRankings[mocRank], createdAt.AddDays(mocRank + 1), createdAt.AddDays(mocRank), requests)
                {
                    Status = requestStatus,
                    OrderGroup = orderGroup,
                    StatusConfirmations = new List<RequestGroupStatusConfirmation>(),
                    Views = new List<RequestGroupView>()
                };
                requestGroups.Add(requestGroup);
                mocRank++;
            }
            orderGroup.OrderGroupId = id;
            orderGroup.CreatedBy = createdBy.Id;
            orderGroup.CustomerOrganisationId = createdBy.CustomerOrganisation.CustomerOrganisationId;
            orderGroup.OrderGroupNumber = groupName;
            orderGroup.Region = region;
            orderGroup.RegionId = region.RegionId;
            orderGroup.Language = language;
            orderGroup.LanguageId = language.LanguageId;
            orderGroup.Status = status;
            orderGroup.RequestGroups = requestGroups;
            orderGroup.CustomerUnit = unit;
            orderGroup.CustomerUnitId = unit?.CustomerUnitId;
            orderGroup.AllowExceedingTravelCost = allowExceedingTravelCost;
            orderGroup.InterpreterLocations = locations;
            return orderGroup;
        }
    }
}
