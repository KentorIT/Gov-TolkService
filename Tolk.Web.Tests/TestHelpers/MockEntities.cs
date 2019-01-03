using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Tests.TestHelpers
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
                    CustomerOrganisationId = 2,
                    Status = BusinessLogic.Enums.OrderStatus.RequestRespondedNewInterpreter,
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
                    Status = BusinessLogic.Enums.OrderStatus.DeliveryAccepted,
                    Requests = new List<Request>
                    {
                        new Request(mockRankings[0], new DateTimeOffset(2018,06,26,14,56,00, new TimeSpan(02,00,00)), DateTimeOffset.Now),
                    },
                },
                new Order {
                    OrderId = 2,
                    OrderNumber = "2018-000042",
                    StartAt = new DateTimeOffset(2018,08,07,13,00,00, new TimeSpan(02,00,00)),
                    EndAt = new DateTimeOffset(2018,08,07,13,00,00, new TimeSpan(02,00,00)),
                    Region = Region.Regions.Where(r => r.Name == "Skåne").Single(),
                    Language = mockLanguages.Where(l => l.Name == "French").Single(),
                    CustomerOrganisationId = 1,
                    Status = BusinessLogic.Enums.OrderStatus.Requested,
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
                    Status = BusinessLogic.Enums.OrderStatus.Delivered,
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
                    Status = BusinessLogic.Enums.OrderStatus.RequestResponded,
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
                    Status = BusinessLogic.Enums.OrderStatus.Delivered,
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
                    Status = BusinessLogic.Enums.OrderStatus.CancelledByCreator,
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
                    Status = BusinessLogic.Enums.OrderStatus.Delivered,
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

        internal static Request[] GetRequestsFromOrders(Order[] mockOrders)
        {
            List<Request> mockRequests = new List<Request>();
            foreach (Order o in mockOrders)
            {
                mockRequests.AddRange(o.Requests);
            }
            return mockRequests.ToArray();
        }

        internal static Request[] MockRequests(Order[] mockOrders)
        {
            return new[]
            {
                new Request
                {
                    Order = mockOrders[3],
                    ExpiresAt = mockOrders[3].StartAt.AddDays(-10),
                    Status = BusinessLogic.Enums.RequestStatus.Accepted
                },
                new Request
                {
                    Order = mockOrders[0],
                    ExpiresAt = mockOrders[0].StartAt.AddDays(-10),
                    Status = BusinessLogic.Enums.RequestStatus.InterpreterReplaced
                },
                new Request
                {
                    Order = mockOrders[1],
                    ExpiresAt = mockOrders[1].StartAt.AddDays(-3d),
                    Status = BusinessLogic.Enums.RequestStatus.AcceptedNewInterpreterAppointed
                },
                new Request
                {
                    Order = mockOrders[2],
                    ExpiresAt = mockOrders[2].StartAt.AddDays(-10),
                    Status = BusinessLogic.Enums.RequestStatus.ToBeProcessedByBroker
                },
                new Request
                {
                    Order = mockOrders[4],
                    ExpiresAt = mockOrders[4].StartAt.AddDays(-10),
                    Status = BusinessLogic.Enums.RequestStatus.Approved
                },
                new Request
                {
                    Order = mockOrders[5],
                    ExpiresAt = mockOrders[5].StartAt.AddDays(-10),
                    Status = BusinessLogic.Enums.RequestStatus.Accepted
                },
            };
        }

        internal static Requisition[] MockRequisitions(Order[] orders)
        {
            var requisitions = new List<Requisition>
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

        internal static Order[] LinkRequisitionsInOrdersRequests(Order[] orders, Requisition[] requisitions)
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
    }
}
