using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;

namespace Tolk.Web.Tests.Helpers
{
    // Shared mock-entities
    public static class MockEntities
    {
        internal static Language[] MockLanguages()
        {
            return new[]
            {
                new Language { LanguageId = 0, Name = "English" },
                new Language { LanguageId = 1, Name = "German" },
                new Language { LanguageId = 2, Name = "French" },
                new Language { LanguageId = 3, Name = "Chinese" },
            };
        }

        internal static Ranking[] MockRankings()
        {
            return new[]
            {
                new Ranking { RankingId = 0, BrokerId = 0, Rank = 1 },
                new Ranking { RankingId = 1, BrokerId = 1, Rank = 2 },
            };
        }

        internal static Order[] MockOrders(Language[] mockLanguages, Ranking[] mockRankings)
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
                    Status = BusinessLogic.Enums.OrderStatus.RequestRespondedNewInterpreter,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,05,26,14,56,00, new TimeSpan(02,00,00))),
                        new Request(mockRankings[1], new DateTimeOffset(2018,06,02,14,11,00, new TimeSpan(02,00,00))),
                    },
                },
                new Order {
                    OrderId = 1,
                    OrderNumber = "2018-000066", // execute order 66...
                    StartAt = new DateTimeOffset(2018,07,07,08,30,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,07,07,17,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Stockholm").Single(),
                    Language = mockLanguages.Where(l => l.Name == "German").Single(),
                    Status = BusinessLogic.Enums.OrderStatus.DeliveryAccepted,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,06,26,14,56,00, new TimeSpan(02,00,00))),
                    },
                },
                new Order {
                    OrderId = 2,
                    OrderNumber = "2018-000042",
                    StartAt = new DateTimeOffset(2018,08,07,13,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,08,07,13,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Skåne").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    Status = BusinessLogic.Enums.OrderStatus.Requested,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,07,29,14,56,00, new TimeSpan(02,00,00))),
                    }
                },

                new Order {
                    OrderId = 3,
                    OrderNumber = "2018-000654",
                    StartAt = new DateTimeOffset(2018,09,03,13,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,09,03,19,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Västra Götaland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "Chinese").Single(),
                    Status = BusinessLogic.Enums.OrderStatus.Delivered,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,09,01,14,56,00, new TimeSpan(02,00,00))),
                    }
                },
                new Order {
                    OrderId = 4,
                    OrderNumber = "2018-000330",
                    StartAt = new DateTimeOffset(2018,09,18,09,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,09,18,13,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Västra Götaland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "German").Single(),
                    Status = BusinessLogic.Enums.OrderStatus.RequestResponded,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,09,15,14,56,00, new TimeSpan(02,00,00))),
                    }
                },
                new Order {
                    OrderId = 5,
                    OrderNumber = "2018-000501",
                    StartAt = new DateTimeOffset(2018,10,09,10,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,10,09,15,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Gotland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    Status = BusinessLogic.Enums.OrderStatus.Delivered,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,09,15,14,56,00, new TimeSpan(02,00,00))),
                        new Request(mockRankings[1], new DateTimeOffset(2018,10,02,14,56,00, new TimeSpan(02,00,00))),
                    }
                },
                new Order {
                    OrderId = 6,
                    OrderNumber = "2018-000006",
                    StartAt = new DateTimeOffset(2018,09,03,00,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,09,03,19,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Västra Götaland").Single(),
                    Language = mockLanguages.Where(l => l.Name == "Chinese").Single(),
                    Status = BusinessLogic.Enums.OrderStatus.CancelledByCreatorConfirmed,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,08,25,14,56,00, new TimeSpan(02,00,00))),
                    }
                },
            };

            // Set required properties
            foreach (Order o in orders)
            {
                o.LanguageId = o.Language.LanguageId;
                foreach (Request r in o.Requests)
                {
                    r.Order = o;
                }
            }

            return orders;
        }

        internal static RequestListItemModel[] MockRequestListItems(Order[] mockOrders)
        {
            return new[]
            {
                new RequestListItemModel
                {
                    OrderNumber = mockOrders[3].OrderNumber,
                    RegionId = mockOrders[3].Region.RegionId,
                    RegionName = mockOrders[3].Region.Name,
                    CustomerId = 1,
                    LanguageId = mockOrders[3].Language.LanguageId,
                    Start = mockOrders[3].StartAt,
                    End = mockOrders[3].EndAt,
                    ExpiresAt = mockOrders[3].StartAt.AddDays(-10),
                    Status = BusinessLogic.Enums.RequestStatus.Accepted
                },
                new RequestListItemModel
                {
                    OrderNumber = mockOrders[0].OrderNumber,
                    RegionId = mockOrders[0].Region.RegionId,
                    RegionName = mockOrders[0].Region.Name,
                    CustomerId = 2,
                    LanguageId = mockOrders[0].Language.LanguageId,
                    Start = mockOrders[0].StartAt,
                    End = mockOrders[0].EndAt,
                    ExpiresAt = mockOrders[0].StartAt.AddDays(-10),
                    Status = BusinessLogic.Enums.RequestStatus.InterpreterReplaced
                },
                new RequestListItemModel
                {
                    OrderNumber = mockOrders[1].OrderNumber,
                    RegionId = mockOrders[1].Region.RegionId,
                    RegionName = mockOrders[1].Region.Name,
                    CustomerId = 2,
                    LanguageId = mockOrders[1].Language.LanguageId,
                    Start = mockOrders[1].StartAt,
                    End = mockOrders[1].EndAt,
                    ExpiresAt = mockOrders[1].StartAt.AddDays(-3d),
                    Status = BusinessLogic.Enums.RequestStatus.AcceptedNewInterpreterAppointed
                },
                new RequestListItemModel
                {
                    OrderNumber = mockOrders[2].OrderNumber,
                    RegionId = mockOrders[2].Region.RegionId,
                    RegionName = mockOrders[2].Region.Name,
                    CustomerId = 1,
                    LanguageId = mockOrders[2].Language.LanguageId,
                    Start = mockOrders[2].StartAt,
                    End = mockOrders[2].EndAt,
                    ExpiresAt = mockOrders[2].StartAt.AddDays(-10),
                    Status = BusinessLogic.Enums.RequestStatus.ToBeProcessedByBroker
                },
                new RequestListItemModel
                {
                    OrderNumber = mockOrders[4].OrderNumber,
                    RegionId = mockOrders[4].Region.RegionId,
                    RegionName = mockOrders[4].Region.Name,
                    CustomerId = 1,
                    LanguageId = mockOrders[4].Language.LanguageId,
                    Start = mockOrders[4].StartAt,
                    End = mockOrders[4].EndAt,
                    ExpiresAt = mockOrders[4].StartAt.AddDays(-10),
                    Status = BusinessLogic.Enums.RequestStatus.Approved
                },
                new RequestListItemModel
                {
                    OrderNumber = mockOrders[5].OrderNumber,
                    RegionId = mockOrders[5].Region.RegionId,
                    RegionName = mockOrders[5].Region.Name,
                    CustomerId = 1,
                    LanguageId = mockOrders[5].Language.LanguageId,
                    Start = mockOrders[5].StartAt,
                    End = mockOrders[5].EndAt,
                    ExpiresAt = mockOrders[5].StartAt.AddDays(-10),
                    Status = BusinessLogic.Enums.RequestStatus.Accepted
                },
            };
        }

        internal static Requisition[] MockRequisitions(Order[] orders)
        {
            return new[]
            {
                new Requisition
                {
                    Status = BusinessLogic.Enums.RequisitionStatus.DeniedByCustomer,
                    Request = orders[0].Requests[1]
                },
                new Requisition
                {
                    Status = BusinessLogic.Enums.RequisitionStatus.Approved,
                    Request = orders[1].Requests[0]
                },
                new Requisition
                {
                    Status = BusinessLogic.Enums.RequisitionStatus.Approved,
                    Request = orders[3].Requests[0]
                },
                new Requisition
                {
                    Status = BusinessLogic.Enums.RequisitionStatus.Created,
                    Request = orders[5].Requests[1]
                },
            };
        }
    }
}
