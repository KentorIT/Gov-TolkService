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
        public static List<EventLogEntryModel> GetEventLog(Order order)
        {
            var eventLog = new List<EventLogEntryModel>
            {
                // Order creation
                new EventLogEntryModel
                {
                    Timestamp = order.CreatedAt,
                    EventDetails = "Avrop skapad",
                    Actor = order.CreatedByUser.FullName,
                    Organization = order.CreatedByUser.CustomerOrganisation.Name,
                }
            };
            // Add all request logs (including their requisition and complaint logs)
            if (order.Requests.Any())
            {
                foreach (var request in order.Requests)
                {
                    eventLog.AddRange(GetEventLog(request, true));
                }
            }
            return eventLog;
        }

        public static List<EventLogEntryModel> GetEventLog(Request request, bool verbose = false)
        {
            // TODO: Handle automatic request handling i.e. replacements
            var eventLog = new List<EventLogEntryModel>
            {
                // Request creation
                new EventLogEntryModel
                {
                    Timestamp = request.CreatedAt,
                    EventDetails = verbose ? $"Förfrågan skickad till {request.Ranking.Broker.Name}" : "Förfrågan inkommen",
                    Actor = "Systemet",
                }
            };
            // Request reception
            if (request.RecievedAt.HasValue)
            {
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = request.CreatedAt,
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
                else
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
                else if (request.Status == RequestStatus.ResponseNotAnsweredByCreator)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = request.AnswerProcessedAt.Value,
                        EventDetails = $"Obesvarad tillsättning automatiskt nekad, tiden gick ut",
                        Actor = "Systemet",
                    });
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
                        EventDetails = "Reklamation avvaktar extern process",
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
                        EventDetails = "Reklamation terminerad, extern process bistod reklamation",
                        Actor = complaint.TerminatingUser.FullName,
                        Organization = complaint.TerminatingUser.CustomerOrganisation.Name,
                    });
                }
                else
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = complaint.TerminatedAt.Value,
                        EventDetails = "Reklamation terminerad, extern process avslog reklamation",
                        Actor = complaint.TerminatingUser.FullName,
                        Organization = complaint.TerminatingUser.CustomerOrganisation.Name,
                    });
                }
            }
            return eventLog;
        }
    }
}
