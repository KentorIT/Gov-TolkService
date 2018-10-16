using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Models;

namespace Tolk.Web.Helpers
{
    public static class EventLogHelper
    {
        public struct OrderMetaData
        {
            public Request TerminatingRequest { get; set; }
        }

        public static List<EventLogEntryModel> GetEventLog(Order order, OrderMetaData? orderMetaData = null)
        {
            var eventLog = new List<EventLogEntryModel>
            {
                // Order creation
                new EventLogEntryModel
                {
                    Timestamp = order.CreatedAt,
                    EventDetails = order.ReplacingOrder != null ? $"Ersättningsavrop skapad (ersätter {order.ReplacingOrder.OrderNumber})" : "Avrop skapad",
                    Actor = order.CreatedByUser.FullName,
                    Organization = order.CreatedByUser.CustomerOrganisation.Name,
                }
            };
            // Add all request logs (including their requisition and complaint logs)
            if (order.Requests.Any())
            {
                foreach (var request in order.Requests)
                {
                    eventLog.AddRange(GetEventLog(request, false));
                }
            }
            // Order replaced
            if (order.ReplacedByOrder != null)
            {
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = order.ReplacedByOrder.CreatedAt,
                    EventDetails = $"Avrop ersatt av {order.ReplacedByOrder.OrderNumber}",
                    Actor = order.ReplacedByOrder.CreatedByUser.FullName,
                    Organization = order.ReplacedByOrder.CreatedByUser.CustomerOrganisation.Name,
                });
            }
            if (orderMetaData.HasValue)
            {
                if (orderMetaData.Value.TerminatingRequest != null)
                {
                    // No one accepted order
                    if (order.Status == OrderStatus.NoBrokerAcceptedOrder)
                    {
                        eventLog.Add(new EventLogEntryModel
                        {
                            Weight = 200,
                            Timestamp = orderMetaData.Value.TerminatingRequest.Status == RequestStatus.DeniedByTimeLimit 
                                ? orderMetaData.Value.TerminatingRequest.ExpiresAt 
                                : orderMetaData.Value.TerminatingRequest.AnswerDate.Value,
                            EventDetails = "Avrop avslutat, ingen förmedling tillsatte uppdraget",
                            Actor = "Systemet",
                        });
                    }
                }
            }
            return eventLog;
        }

        public static List<EventLogEntryModel> GetEventLog(Request request, bool isRequestDetailView = true)
        {
            var eventLog = new List<EventLogEntryModel>();
            if (isRequestDetailView && request.ReplacingRequestId.HasValue)
            {
                // Include event log for previous request, if this is the requests detail view
                eventLog.AddRange(GetEventLog(request.ReplacingRequest));
            }
            if (!request.ReplacingRequestId.HasValue)
            {
                // Request creation
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = request.CreatedAt,
                    EventDetails = isRequestDetailView ? "Förfrågan inkommen" : $"Förfrågan skickad till {request.Ranking.Broker.Name}",
                    Actor = "Systemet",
                });
            }
            // Request reception
            if (request.RecievedAt.HasValue && !request.ReplacingRequestId.HasValue)
            {
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = request.RecievedAt.Value,
                    EventDetails = $"Förfrågan mottagen",
                    Actor = request.ReceivedByUser.FullName,
                    Organization = request.ReceivedByUser.Broker.Name,
                });
            }
            // Request expired
            if (request.Status == RequestStatus.DeniedByTimeLimit)
            {
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = request.ExpiresAt,
                    EventDetails = "Förfrågan obesvarad, tiden gick ut",
                    Actor = "Systemet",
                });
            }
            // Request answered by broker
            if (request.AnswerDate.HasValue)
            {
                if (request.Status == RequestStatus.DeclinedByBroker)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = request.AnswerDate.Value,
                        EventDetails = $"Förfrågan nekad av förmedling",
                        Actor = request.AnsweringUser.FullName,
                        Organization = request.AnsweringUser.Broker.Name,
                    });
                }
                else if (!request.ReplacingRequestId.HasValue)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = request.AnswerDate.Value,
                        EventDetails = $"Tolk tillsatt av förmedling",
                        Actor = request.AnsweringUser.FullName,
                        Organization = request.AnsweringUser.Broker.Name,
                    });
                }
            }
            // Request answer processed by customer organization
            if (request.AnswerProcessedAt.HasValue)
            {
                if (request.Status == RequestStatus.DeniedByCreator)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = request.AnswerProcessedAt.Value,
                        EventDetails = $"Tillsättning nekad av avropare",
                        Actor = request.ProcessingUser.FullName,
                        Organization = request.ProcessingUser.CustomerOrganisation.Name,
                    });
                }
                else
                {
                    if (request.ReplacingRequestId.HasValue)
                    {
                        if (request.Order.AllowMoreThanTwoHoursTravelTime)
                        {
                            eventLog.Add(new EventLogEntryModel
                            {
                                Weight = 200,
                                Timestamp = request.AnswerDate.Value,
                                EventDetails = $"Tolkbyte godkänd av avropare",
                                Actor = request.ProcessingUser.FullName,
                                Organization = request.ProcessingUser.CustomerOrganisation.Name,
                            });
                        }
                        else
                        {
                            eventLog.Add(new EventLogEntryModel
                            {
                                Weight = 200,
                                Timestamp = request.AnswerDate.Value,
                                EventDetails = $"Tolkbyte automatiskt godkänd",
                                Actor = "Systemet",
                            });
                        }
                    }
                    else
                    {
                        eventLog.Add(new EventLogEntryModel
                        {
                            Timestamp = request.AnswerProcessedAt.Value,
                            EventDetails = $"Tillsättning godkänd av avropare",
                            Actor = request.ProcessingUser.FullName,
                            Organization = request.ProcessingUser.CustomerOrganisation.Name,
                        });
                    }
                }
            }
            else if (request.Status == RequestStatus.ResponseNotAnsweredByCreator)
            {
                // TODO: Check when unanswered response should expire
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = request.Order.StartAt.AddHours(-1.0),
                    EventDetails = $"Obesvarad tillsättning automatiskt nekad, tiden gick ut",
                    Actor = "Systemet",
                });
            }
            // Request cancellation
            if (request.CancelledAt.HasValue)
            {
                if (request.Status == RequestStatus.CancelledByCreator || request.Status == RequestStatus.CancelledByCreatorConfirmed)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = request.CancelledAt.Value,
                        EventDetails = "Avrop avbokat av avropare",
                        Actor = request.CancelledByUser.FullName,
                        Organization = request.CancelledByUser.CustomerOrganisation.Name,
                    });
                }
                else if (request.Status == RequestStatus.CancelledByBroker || request.Status == RequestStatus.CancelledByBrokerConfirmed)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = request.CancelledAt.Value,
                        EventDetails = "Avrop avbokat av förmedling",
                        Actor = request.CancelledByUser.FullName,
                        Organization = request.CancelledByUser.Broker.Name,
                    });
                }
            }
            // Request cancellation confirmation
            if (request.CancelConfirmedAt.HasValue)
            {
                if (request.Status == RequestStatus.CancelledByCreatorConfirmed)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = request.CancelConfirmedAt.Value,
                        EventDetails = "Avbokning bekräftad av förmedling",
                        Actor = request.CancelConfirmedByUser.FullName,
                        Organization = request.CancelConfirmedByUser.Broker.Name,
                    });
                }
                else if (request.Status == RequestStatus.CancelledByBrokerConfirmed)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = request.CancelConfirmedAt.Value,
                        EventDetails = "Avbokning bekräftad av avropare",
                        Actor = request.CancelConfirmedByUser.FullName,
                        Organization = request.CancelConfirmedByUser.CustomerOrganisation.Name,
                    });
                }
            }
            // Interpreter replacement
            if (request.Status == RequestStatus.InterpreterReplaced)
            {
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = request.ReplacedByRequest.AnswerDate.Value,
                    EventDetails = "Tolk ersatt av förmedling",
                    Actor = request.ReplacedByRequest.AnsweringUser.FullName,
                    Organization = request.ReplacedByRequest.AnsweringUser.Broker.Name,
                });
            }
            // Add all requisition logs
            if (request.Requisitions.Any())
            {
                foreach (var requisition in request.Requisitions)
                {
                    eventLog.AddRange(GetEventLog(requisition));
                }
            }
            // Add all complaint logs
            if (request.Complaints.Any())
            {
                foreach (var complaints in request.Complaints)
                {
                    eventLog.AddRange(GetEventLog(complaints));
                }
            }
            return eventLog;
        }

        public static List<EventLogEntryModel> GetEventLog(Requisition requisition)
        {
            var eventLog = new List<EventLogEntryModel>();
            // Requisition creation
            if (requisition.Status == RequisitionStatus.AutomaticApprovalFromCancelledOrder)
            {
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = requisition.CreatedAt,
                    EventDetails = "Rekvisition automatiskt skapad och godkänd, sen avbokning",
                    Actor = "Systemet",
                });
            }
            else
            {
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = requisition.CreatedAt,
                    EventDetails = "Rekvisition registrerad",
                    Actor = requisition.CreatedByUser.FullName,
                    Organization = requisition.CreatedByUser.Broker.Name,
                });
            }
            // Requisition processing
            if (requisition.ProcessedAt.HasValue)
            {
                if (requisition.Status == RequisitionStatus.Approved)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = requisition.ProcessedAt.Value,
                        EventDetails = "Rekvisition godkänd",
                        Actor = requisition.ProcessedUser.FullName,
                        Organization = requisition.ProcessedUser.CustomerOrganisation.Name,
                    });
                }
                else if (requisition.Status == RequisitionStatus.DeniedByCustomer)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = requisition.ProcessedAt.Value,
                        EventDetails = "Rekvisition underkänd",
                        Actor = requisition.ProcessedUser.FullName,
                        Organization = requisition.ProcessedUser.CustomerOrganisation.Name,
                    });
                }
            }
            return eventLog;
        }

        public static List<EventLogEntryModel> GetEventLog(Complaint complaint)
        {
            var eventLog = new List<EventLogEntryModel>
            {
                // Complaint creation
                new EventLogEntryModel
                {
                    Timestamp = complaint.CreatedAt,
                    EventDetails = "Reklamation registrerad",
                    Actor = complaint.CreatedByUser.FullName,
                    Organization = complaint.CreatedByUser.CustomerOrganisation.Name,
                }
            };
            // Complaint answer
            if (complaint.AnsweredAt.HasValue)
            {
                if (complaint.Status == ComplaintStatus.Confirmed)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = complaint.AnsweredAt.Value,
                        EventDetails = "Reklamation accepterad av förmedling",
                        Actor = complaint.AnsweringUser.FullName,
                        Organization = complaint.AnsweringUser.Broker.Name,
                    });
                }
                else
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = complaint.AnsweredAt.Value,
                        EventDetails = "Reklamation bestridd av förmedling",
                        Actor = complaint.AnsweringUser.FullName,
                        Organization = complaint.AnsweringUser.Broker.Name,
                    });
                }
            }
            // Complaint answer disputation
            if (complaint.AnswerDisputedAt.HasValue)
            {
                if (complaint.Status == ComplaintStatus.TerminatedAsDisputeAccepted)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = complaint.AnswerDisputedAt.Value,
                        EventDetails = "Reklamationens bestridande accepterad av avropare",
                        Actor = complaint.AnswerDisputingUser.FullName,
                        Organization = complaint.AnswerDisputingUser.CustomerOrganisation.Name,
                    });
                }
                else
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = complaint.AnswerDisputedAt.Value,
                        EventDetails = "Reklamationens bestridande avslagen av avropare, avvaktar extern process",
                        Actor = complaint.AnswerDisputingUser.FullName,
                        Organization = complaint.AnswerDisputingUser.CustomerOrganisation.Name,
                    });
                }
            }
            // Complaint termination
            if (complaint.TerminatedAt.HasValue)
            {
                if (complaint.Status == ComplaintStatus.TerminatedTrialConfirmedComplaint)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = complaint.TerminatedAt.Value,
                        EventDetails = "Reklamation avslutad, extern process bistod reklamation",
                        Actor = complaint.TerminatingUser.FullName,
                        Organization = complaint.TerminatingUser.CustomerOrganisation.Name,
                    });
                }
                else
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = complaint.TerminatedAt.Value,
                        EventDetails = "Reklamation avslutad, extern process avslog reklamation",
                        Actor = complaint.TerminatingUser.FullName,
                        Organization = complaint.TerminatingUser.CustomerOrganisation.Name,
                    });
                }
            }
            return eventLog;
        }
    }
}
