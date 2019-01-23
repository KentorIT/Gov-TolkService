using System.Collections.Generic;
using System.Linq;
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

        public static List<EventLogEntryModel> GetEventLog(Order order, Request terminatingRequest = null)
        {
            var customerName = order.CustomerOrganisation.Name;
            var eventLog = new List<EventLogEntryModel>
            {
                // Order creation
                new EventLogEntryModel
                {
                    Timestamp = order.CreatedAt,
                    EventDetails = order.ReplacingOrder != null ? $"Ersättningsuppdrag skapat (ersätter {order.ReplacingOrder.OrderNumber})" : "Bokningsförfrågan skapad",
                    Actor = order.CreatedByUser.FullName,
                    Organization = customerName,
                    ActorContactInfo = GetContactinfo(order.CreatedByUser),
                }
            };
            // Add all request logs (including their requisition and complaint logs)
            if (order.Requests.Any())
            {
                foreach (var request in order.Requests)
                {
                    eventLog.AddRange(GetEventLog(request, customerName, false));
                }
            }
            // Order replaced
            if (order.ReplacedByOrder != null)
            {
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = order.ReplacedByOrder.CreatedAt,
                    EventDetails = $"Bokning ersatt av {order.ReplacedByOrder.OrderNumber}",
                    Actor = order.ReplacedByOrder.CreatedByUser.FullName,
                    Organization = customerName,
                    ActorContactInfo = GetContactinfo(order.ReplacedByOrder.CreatedByUser),
                });
            }
            // Change of contact person  
            if (order.OrderContactPersonHistory.Any())
            {
                int i = 0;
                foreach (OrderContactPersonHistory cph in order.OrderContactPersonHistory.OrderBy(ch => ch.OrderContactPersonHistoryId))
                {
                    string newContactPersonName = string.Empty;
                    string previousContactPersonName = string.Empty;
                    //if previous contact is null, a new contact person is added - get the new contact
                    if (cph.PreviousContactPersonId == null)
                    {
                        EventLogEntryModel eventRow = GetEventRowForNewContactPerson(cph, order, i + 1);
                        if (eventRow != null)
                        {
                            eventLog.Add(eventRow);
                        }
                    }
                    //if previous contact person is not null, then contact person is changed or just removed
                    else
                    {
                        //add a row for removed person
                        eventLog.Add(new EventLogEntryModel
                        {
                            Timestamp = cph.ChangedAt,
                            EventDetails = $"Kontaktperson {cph.PreviousContactPersonUser?.FullName} borttagen",
                            Actor = cph.ChangedByUser.FullName,
                            Organization = customerName,
                            ActorContactInfo = GetContactinfo(cph.ChangedByUser),
                        });
                        //find if removed or changed (if removed we don't add a row else add row for new contact)
                        EventLogEntryModel eventRow = GetEventRowForNewContactPerson(cph, order, i + 1);
                        if (eventRow != null)
                        {
                            eventLog.Add(eventRow);
                        }
                    }
                    i++;
                }
            }
            {
                if (terminatingRequest != null)
                {
                    // No one accepted order
                    if (order.Status == OrderStatus.NoBrokerAcceptedOrder)
                    {
                        eventLog.Add(new EventLogEntryModel
                        {
                            Weight = 200,
                            Timestamp = terminatingRequest.Status == RequestStatus.DeniedByTimeLimit
                                ? terminatingRequest.ExpiresAt
                                : terminatingRequest.AnswerDate.Value,
                            EventDetails = "Bokningsförfrågan avslutad, pga avböjd av samtliga förmedlingar",
                            Actor = "Systemet",
                        });
                    }
                }
            }
            if (order.OrderStatusConfirmations.Any(os => os.OrderStatus == OrderStatus.NoBrokerAcceptedOrder))
            {
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = order.OrderStatusConfirmations.First(os => os.OrderStatus == OrderStatus.NoBrokerAcceptedOrder).ConfirmedAt.Value,
                    EventDetails = $"Bekräftat bokningsförfrågan avslutad",
                    Actor = order.OrderStatusConfirmations.First(os => os.OrderStatus == OrderStatus.NoBrokerAcceptedOrder).ConfirmedByUser.FullName,
                    Organization = customerName,
                    ActorContactInfo = GetContactinfo(order.OrderStatusConfirmations.First(os => os.OrderStatus == OrderStatus.NoBrokerAcceptedOrder).ConfirmedByUser),
                });
            }
            return eventLog;
        }

        private static EventLogEntryModel GetEventRowForNewContactPerson(OrderContactPersonHistory cphPrevious, Order order, int findElementAt)
        {
            //try find next row if any else take info from Order.ContactPersonUser
            string newContactPersonName = order.OrderContactPersonHistory.Count() > findElementAt ? order.OrderContactPersonHistory.ElementAt(findElementAt).PreviousContactPersonUser?.FullName : order.ContactPersonUser?.FullName;
            return string.IsNullOrWhiteSpace(newContactPersonName) ? null : new EventLogEntryModel
            {
                Timestamp = cphPrevious.ChangedAt,
                EventDetails = $"Kontaktperson {newContactPersonName} tillagd",
                Actor = cphPrevious.ChangedByUser.FullName,
                Organization = order.CustomerOrganisation.Name,
                ActorContactInfo = GetContactinfo(cphPrevious.ChangedByUser),
            };
        }

        public static List<EventLogEntryModel> GetEventLog(Request request, string customerName, bool isRequestDetailView = true, IEnumerable<Request> previousRequests = null)
        {
            var eventLog = new List<EventLogEntryModel>();
            if (isRequestDetailView && request.ReplacingRequestId.HasValue && previousRequests != null)
            {
                // Include event log for all previous requests, if this is the requests detail view
                foreach (Request r in previousRequests)
                {
                    eventLog.AddRange(GetEventLog(r, customerName));
                }
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
                    ActorContactInfo = GetContactinfo(request.ReceivedByUser),
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
                        ActorContactInfo = GetContactinfo(request.AnsweringUser),
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
                        ActorContactInfo = GetContactinfo(request.AnsweringUser),
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
                        EventDetails = $"Tillsättning avböjd av myndighet",
                        Actor = request.ProcessingUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(request.ProcessingUser),
                    });
                    if (request.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.DeniedByCreator))
                    {
                        eventLog.Add(new EventLogEntryModel
                        {
                            Timestamp = request.RequestStatusConfirmations.First(rs => rs.RequestStatus == RequestStatus.DeniedByCreator).ConfirmedAt.Value,
                            EventDetails = $"Avböjande bekräftat",
                            Actor = request.RequestStatusConfirmations.First(rs => rs.RequestStatus == RequestStatus.DeniedByCreator).ConfirmedByUser.FullName,
                            Organization = request.AnsweringUser.Broker.Name,
                            ActorContactInfo = GetContactinfo(request.RequestStatusConfirmations.First(rs => rs.RequestStatus == RequestStatus.DeniedByCreator).ConfirmedByUser),
                        });
                    }
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
                                EventDetails = $"Tolkbyte godkänt av myndighet",
                                Actor = request.ProcessingUser.FullName,
                                Organization = customerName,
                                ActorContactInfo = GetContactinfo(request.ProcessingUser),
                            });
                        }
                        else
                        {
                            eventLog.Add(new EventLogEntryModel
                            {
                                Weight = 200,
                                Timestamp = request.AnswerDate.Value,
                                EventDetails = $"Tolkbyte automatiskt godkänt",
                                Actor = "Systemet",
                            });
                        }
                    }
                    else
                    {
                        eventLog.Add(new EventLogEntryModel
                        {
                            Timestamp = request.AnswerProcessedAt.Value,
                            EventDetails = $"Tillsättning godkänd av myndighet",
                            Actor = request.ProcessingUser.FullName,
                            Organization = customerName,
                            ActorContactInfo = GetContactinfo(request.ProcessingUser),
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
                if (request.Status == RequestStatus.CancelledByCreatorWhenApproved || request.Status == RequestStatus.CancelledByCreator)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = request.CancelledAt.Value,
                        EventDetails = "Uppdrag avbokat av myndighet",
                        Actor = request.CancelledByUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(request.CancelledByUser),
                    });
                }
                else if (request.Status == RequestStatus.CancelledByBroker)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = request.CancelledAt.Value,
                        EventDetails = "Uppdrag avbokat av förmedling",
                        Actor = request.CancelledByUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(request.CancelledByUser),
                    });
                }
            }
            // Request cancellation confirmation
            if (request.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.CancelledByCreatorWhenApproved))
            {
                RequestStatusConfirmation rsc = request.RequestStatusConfirmations.First(rs => rs.RequestStatus == RequestStatus.CancelledByCreatorWhenApproved);
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = rsc.ConfirmedAt.Value,
                    EventDetails = $"Avbokning bekräftad av förmedling",
                    Actor = rsc.ConfirmedByUser.FullName,
                    Organization = request.Ranking.Broker.Name,
                    ActorContactInfo = GetContactinfo(rsc.ConfirmedByUser),
                });
            }
            else if (request.RequestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.CancelledByBroker))
            {
                RequestStatusConfirmation rsc = request.RequestStatusConfirmations.First(rs => rs.RequestStatus == RequestStatus.CancelledByBroker);
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = rsc.ConfirmedAt.Value,
                    EventDetails = $"Avbokning bekräftad av myndighet",
                    Actor = rsc.ConfirmedByUser.FullName,
                    Organization = customerName,
                    ActorContactInfo = GetContactinfo(rsc.ConfirmedByUser),
                });
            }
            // Interpreter replacement
            if (request.Status == RequestStatus.InterpreterReplaced)
            {
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = request.ReplacedByRequest.AnswerDate.Value,
                    EventDetails = $"Tolk {request.Interpreter?.FullName} är ersatt av tolk {request.ReplacedByRequest.Interpreter?.FullName}",
                    Actor = request.ReplacedByRequest.AnsweringUser.FullName,
                    Organization = request.ReplacedByRequest.AnsweringUser.Broker.Name,
                    ActorContactInfo = GetContactinfo(request.ReplacedByRequest.AnsweringUser),
                });
            }
            // Add all requisition logs
            if (request.Requisitions != null && request.Requisitions.Any())
            {
                eventLog.AddRange(GetEventLog(request.Requisitions, customerName));
            }
            // Add all complaint logs
            if (request.Complaints != null && request.Complaints.Any())
            {
                foreach (var complaints in request.Complaints)
                {
                    eventLog.AddRange(GetEventLog(complaints, customerName));
                }
            }
            return eventLog;
        }

        public static IEnumerable<EventLogEntryModel> GetEventLog(IEnumerable<Requisition> requisitions, string customerName)
        {
            foreach (var requisition in requisitions)
            {
                // Requisition creation
                if (requisition.Status == RequisitionStatus.AutomaticApprovalFromCancelledOrder)
                {
                    yield return new EventLogEntryModel
                    {
                        Timestamp = requisition.CreatedAt,
                        EventDetails = "Rekvisition automatiskt skapad och godkänd, pga avbokning",
                        Actor = "Systemet",
                    };
                }
                else
                {
                    yield return new EventLogEntryModel
                    {
                        Timestamp = requisition.CreatedAt,
                        EventDetails = "Rekvisition registrerad",
                        Actor = requisition.CreatedByUser.FullName,
                        Organization = requisition.CreatedByUser.Broker?.Name, //interpreter has no org.
                        ActorContactInfo = GetContactinfo(requisition.CreatedByUser),
                    };
                }
                // Requisition processing
                if (requisition.ProcessedAt.HasValue)
                {
                    if (requisition.Status == RequisitionStatus.Approved)
                    {
                        yield return new EventLogEntryModel
                        {
                            Timestamp = requisition.ProcessedAt.Value,
                            EventDetails = "Rekvisition godkänd",
                            Actor = requisition.ProcessedUser.FullName,
                            Organization = customerName,
                            ActorContactInfo = GetContactinfo(requisition.ProcessedUser),
                        };
                    }
                    else if (requisition.Status == RequisitionStatus.DeniedByCustomer)
                    {
                        yield return new EventLogEntryModel
                        {
                            Timestamp = requisition.ProcessedAt.Value,
                            EventDetails = "Rekvisition underkänd",
                            Actor = requisition.ProcessedUser.FullName,
                            Organization = customerName,
                            ActorContactInfo = GetContactinfo(requisition.ProcessedUser),
                        };
                    }
                }
            }
        }

        public static List<EventLogEntryModel> GetEventLog(Complaint complaint, string customerName)
        {
            var eventLog = new List<EventLogEntryModel>
            {
                // Complaint creation
                new EventLogEntryModel
                {
                    Timestamp = complaint.CreatedAt,
                    EventDetails = "Reklamation registrerad",
                    Actor = complaint.CreatedByUser.FullName,
                    Organization = customerName,
                    ActorContactInfo = GetContactinfo(complaint.CreatedByUser)
,                }
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
                        ActorContactInfo = GetContactinfo(complaint.AnsweringUser),
                    });
                }
                else
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = complaint.AnsweredAt.Value,
                        EventDetails = "Reklamation är bestriden av förmedling",
                        Actor = complaint.AnsweringUser.FullName,
                        Organization = complaint.AnsweringUser.Broker.Name,
                        ActorContactInfo = GetContactinfo(complaint.AnsweringUser),
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
                        EventDetails = "Reklamation är återtagen av myndighet",
                        Actor = complaint.AnswerDisputingUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(complaint.AnswerDisputingUser),
                    });
                }
                else
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = complaint.AnswerDisputedAt.Value,
                        EventDetails = "Reklamation kvarstår, avvaktar extern process",
                        Actor = complaint.AnswerDisputingUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(complaint.AnswerDisputingUser),
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
                        EventDetails = "Reklamation avslutad, bistådd av extern process",
                        Actor = complaint.TerminatingUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(complaint.TerminatingUser),
                    });
                }
                else
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = complaint.TerminatedAt.Value,
                        EventDetails = "Reklamation avslutad, avslagen av extern process",
                        Actor = complaint.TerminatingUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(complaint.TerminatingUser),
                    });
                }
            }
            return eventLog;
        }

        private static string GetContactinfo(AspNetUser user)
        {
            string contactInfo = string.Empty;
            if (!string.IsNullOrEmpty(user.Email))
            {
                contactInfo += $"Email: {user.Email}\n";
            }

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                contactInfo += $"Telefon: {user.PhoneNumber}\n";
            }

            if (!string.IsNullOrEmpty(user.PhoneNumberCellphone))
            {
                contactInfo += $"Mobil: {user.PhoneNumberCellphone}\n";
            }
            return contactInfo;
        }
    }
}
