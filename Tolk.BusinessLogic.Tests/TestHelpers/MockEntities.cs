using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Tests.TestHelpers
{
    public static class MockEntities
    {
        public static Language[] Languages
        {
            get
            {
                return
                    new[]
                    {
                        new Language { LanguageId = 0, Name = "English" },
                        new Language { LanguageId = 1, Name = "German" },
                        new Language { LanguageId = 2, Name = "French" },
                        new Language { LanguageId = 3, Name = "Chinese" },
                    };
            }
        }

        public static Ranking[] MockRankings()
        {
            return new[]
            {
                new Ranking { RankingId = 0, BrokerId = 0, Rank = 0 },
                new Ranking { RankingId = 1, BrokerId = 1, Rank = 1 },
            };
        }

        public static Order[] Orders(Language[] mockLanguages, Ranking[] mockRankings)
        {
            var orders = new[]
            {
                new Order {
                    OrderId = 0,
                    OrderNumber = "2018-001337",
                    StartAt = new DateTimeOffset(2018,06,07,13,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,06,07,15,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Stockholm").Single(),
                    Language = mockLanguages.Where(l => l.Name == "English").Single(),
                    CustomerOrganisationId = 2,
                    Status = OrderStatus.RequestRespondedNewInterpreter,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,05,26,14,56,00, new TimeSpan(02,00,00)), DateTimeOffset.Now),
                        new Request(mockRankings[1], new DateTimeOffset(2018,06,02,14,11,00, new TimeSpan(02,00,00)), DateTimeOffset.Now),
                    },
                },
                new Order {
                    OrderId = 1,
                    OrderNumber = "2018-000066", // execute order 66...
                    StartAt = new DateTimeOffset(2018,07,07,08,30,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,07,07,17,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Stockholm").Single(),
                    Language = mockLanguages.Where(l => l.Name == "German").Single(),
                    CustomerOrganisationId = 2,
                    Status = OrderStatus.DeliveryAccepted,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,06,26,14,56,00, new TimeSpan(02,00,00)), DateTimeOffset.Now),
                    },
                },
                new Order {
                    OrderId = 2,
                    OrderNumber = "2018-000042",
                    StartAt = new DateTimeOffset(2018,08,07,13,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,08,07,14,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Skåne").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    CustomerOrganisationId = 1,
                    Status = OrderStatus.Requested,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,07,29,14,56,00, new TimeSpan(02,00,00)), DateTimeOffset.Now),
                    }
                },

                new Order {
                    OrderId = 3,
                    OrderNumber = "2018-000654",
                    StartAt = new DateTimeOffset(2018,09,03,13,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,09,03,19,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Västra Götaland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "Chinese").Single(),
                    CustomerOrganisationId = 1,
                    Status = OrderStatus.Delivered,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,09,01,14,56,00, new TimeSpan(02,00,00)), DateTimeOffset.Now),
                    }
                },
                new Order {
                    OrderId = 4,
                    OrderNumber = "2018-000330",
                    StartAt = new DateTimeOffset(2018,09,18,09,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,09,18,13,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Västra Götaland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "German").Single(),
                    CustomerOrganisationId = 1,
                    Status = OrderStatus.RequestResponded,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,09,15,14,56,00, new TimeSpan(02,00,00)), DateTimeOffset.Now),
                    }
                },
                new Order {
                    OrderId = 5,
                    OrderNumber = "2018-000501",
                    StartAt = new DateTimeOffset(2018,10,09,10,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,10,09,15,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Gotland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    CustomerOrganisationId = 1,
                    Status = OrderStatus.Delivered,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,09,15,14,56,00, new TimeSpan(02,00,00)), DateTimeOffset.Now),
                        new Request(mockRankings[1], new DateTimeOffset(2018,10,02,14,56,00, new TimeSpan(02,00,00)), DateTimeOffset.Now),
                    }
                },
                new Order {
                    OrderId = 6,
                    OrderNumber = "2018-000006",
                    StartAt = new DateTimeOffset(2018,09,03,00,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,09,03,19,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Västra Götaland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "Chinese").Single(),
                    CustomerOrganisationId = 6,
                    Status = OrderStatus.CancelledByCreator,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,08,25,14,56,00, new TimeSpan(02,00,00)), DateTimeOffset.Now),
                    }
                },
                new Order
                {
                    OrderId = 7,
                    OrderNumber = "2018-000007",
                    StartAt = new DateTimeOffset(2018,08,15,00,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,08,15,19,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Uppsala").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    CustomerOrganisationId = 6,
                    Status = OrderStatus.Delivered,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,08,01,14,56,00, new TimeSpan(02,00,00)), DateTimeOffset.Now),
                    }
                }
            };

            // Set required properties
            foreach (Order o in orders)
            {
                o.LanguageId = o.Language.LanguageId;
                o.RegionId = o.Region.RegionId;
                foreach (Request r in o.Requests)
                {
                    r.Order = o;
                }
            }

            return orders;
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

        public static Request[] Requests(Order[] mockOrders)
        {
            return new[]
            {
                new Request
                {
                    Order = mockOrders[3],
                    ExpiresAt = mockOrders[3].StartAt.AddDays(-10),
                    Status = RequestStatus.Accepted
                },
                new Request
                {
                    Order = mockOrders[0],
                    ExpiresAt = mockOrders[0].StartAt.AddDays(-10),
                    Status = RequestStatus.InterpreterReplaced
                },
                new Request
                {
                    Order = mockOrders[1],
                    ExpiresAt = mockOrders[1].StartAt.AddDays(-3d),
                    Status = RequestStatus.AcceptedNewInterpreterAppointed
                },
                new Request
                {
                    Order = mockOrders[2],
                    ExpiresAt = mockOrders[2].StartAt.AddDays(-10),
                    Status = RequestStatus.ToBeProcessedByBroker
                },
                new Request
                {
                    Order = mockOrders[4],
                    ExpiresAt = mockOrders[4].StartAt.AddDays(-10),
                    Status = RequestStatus.Approved
                },
                new Request
                {
                    Order = mockOrders[5],
                    ExpiresAt = mockOrders[5].StartAt.AddDays(-10),
                    Status = RequestStatus.Accepted
                },
            };
        }

        public static Requisition[] Requisitions(Order[] orders)
        {
            var requisitions = new List<Requisition>
            {
                new Requisition
                {
                    Status = RequisitionStatus.DeniedByCustomer,
                    Request = orders[0].Requests[1]
                },
                new Requisition
                {
                    Status = RequisitionStatus.Approved,
                    Request = orders[1].Requests[0]
                },
                new Requisition
                {
                    Status = RequisitionStatus.Approved,
                    Request = orders[3].Requests[0]
                },
                new Requisition
                {
                    Status = RequisitionStatus.Created,
                    Request = orders[5].Requests[1]
                },
            };

            foreach (Requisition r in requisitions)
            {
                if (r.Request.Requisitions == null)
                {
                    r.Request.Requisitions = new List<Requisition>();
                }
                r.Request.Requisitions.Add(r);
            }

            return requisitions.ToArray();
        }

        public static Order[] LinkRequisitionsInOrdersRequests(Order[] orders, Requisition[] requisitions)
        {
            foreach (Order o in orders)
            {
                foreach (Request r in o.Requests)
                {
                    if (r.Requisitions == null)
                    {
                        r.Requisitions = new List<Requisition>();
                    }
                    r.Requisitions = requisitions.Where(req => req.Request == r).ToList();
                }
            }

            return orders;
        }

        public static Holiday[] Holidays
        {
            get
            {
                return
                    new[]
                    {
                        new Holiday() { Date = new DateTime(2018,03,29), DateType=DateType.DayBeforeBigHoliday},
                        new Holiday() { Date = new DateTime(2018,03,30), DateType=DateType.BigHolidayFullDay},
                        new Holiday() { Date = new DateTime(2018,04,01), DateType=DateType.BigHolidayFullDay},
                        new Holiday() { Date = new DateTime(2018,04,02), DateType=DateType.BigHolidayFullDay},
                        new Holiday() { Date = new DateTime(2018,04,03), DateType=DateType.DayAfterBigHoliday},
                        new Holiday() { Date = new DateTime(2018,05,01), DateType=DateType.Holiday},
                        new Holiday() { Date = new DateTime(2018,05,10), DateType=DateType.Holiday},
                        new Holiday() { Date = new DateTime(2018,05,18), DateType=DateType.DayBeforeBigHoliday},
                        new Holiday() { Date = new DateTime(2018,05,19), DateType=DateType.BigHolidayFullDay},
                        new Holiday() { Date = new DateTime(2018,12,24), DateType=DateType.BigHolidayFullDay},
                        new Holiday() { Date = new DateTime(2018,12,25), DateType=DateType.BigHolidayFullDay},
                        new Holiday() { Date = new DateTime(2018,12,26), DateType=DateType.BigHolidayFullDay},
                        new Holiday() { Date = new DateTime(2018,12,27), DateType=DateType.DayAfterBigHoliday}
                    };
            }
        }

        public static PriceCalculationCharge[] PriceCalculationCharges
        {
            get
            {
                return
                    new[]
                    {
                        new PriceCalculationCharge() { PriceCalculationChargeId = 1, ChargePercentage =  (decimal)31.42, ChargeTypeId = ChargeType.SocialInsuranceCharge, StartDate = new DateTime(2018,01,01), EndDate =  new DateTime(2099,01,01)},
                        new PriceCalculationCharge() { PriceCalculationChargeId = 2, ChargePercentage =  (decimal)0.7, ChargeTypeId = ChargeType.AdministrativeCharge, StartDate = new DateTime(2018,01,01), EndDate =  new DateTime(2099,01,01)},
                    };
            }
        }

        public static Ranking[] Rankings
        {
            get
            {
                return
                    new[]
                    {
                        new Ranking { RankingId = 1, Rank = 1, FirstValidDate = new DateTime(2018,01,01), LastValidDate = new DateTime(2099,01,01), BrokerFee = (decimal)0.1, BrokerId = 1, RegionId = 1}
                    };
            }
        }

        public static PriceListRow[] PriceListRows
        {
            get
            {
                return
                    new[]
                    {
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
            }
        }

    }
}
