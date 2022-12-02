using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Models;

namespace Tolk.Web.Services
{
    public class EventLogService
    {
        private readonly TolkDbContext _dbContext;

        public EventLogService(TolkDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<EventLogEntryModel>> GetEventLogForOrder(int orderId)
        {
            var order = await _dbContext.Orders.GetOrderForEventLog(orderId);
            if (order == null)
            {
                return new List<EventLogEntryModel>();
            }
            var orderChangeLogEntries = await _dbContext.OrderChangeLogEntries.GetOrderChangeLogEntitesForOrderEventLog(orderId).ToListAsync();
            var orderStatusConfirmations = await _dbContext.OrderStatusConfirmation.GetStatusConfirmationsForOrderEventLog(orderId).ToListAsync();
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
            // Add all request logs (including their requisition and complaint logs)'
            eventLog.AddRange(await GetEventLogForRequestsOnOrder(orderId, customerName, null));
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
            int i = 0;
            var orderContactPersons = orderChangeLogEntries.Where(oc => oc.OrderChangeLogType == OrderChangeLogType.ContactPerson);
            foreach (OrderChangeLogEntry oc in orderChangeLogEntries.Where(oc => oc.OrderChangeLogType == OrderChangeLogType.ContactPerson))
            {
                //if previous contact is null, a new contact person is added - get the new contact
                if (oc.OrderContactPersonHistory.PreviousContactPersonId == null)
                {
                    EventLogEntryModel eventRow = GetEventRowForNewContactPerson(oc, order, orderContactPersons, i + 1);
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
                        Timestamp = oc.LoggedAt,
                        EventDetails = $"{oc.OrderContactPersonHistory.PreviousContactPersonUser?.FullName} fråntogs rätt att granska rekvisition",
                        Actor = oc.UpdatedByUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(oc.UpdatedByUser),
                    });
                    //find if removed or changed (if removed we don't add a row else add row for new contact)
                    EventLogEntryModel eventRow = GetEventRowForNewContactPerson(oc, order, orderContactPersons, i + 1);
                    if (eventRow != null)
                    {
                        eventLog.Add(eventRow);
                    }
                }
                i++;
            }
            if (orderChangeLogEntries.Any(oc => oc.OrderChangeLogType != OrderChangeLogType.ContactPerson))
            {
                foreach (OrderChangeLogEntry oc in orderChangeLogEntries.Where(oc => oc.OrderChangeLogType != OrderChangeLogType.ContactPerson))
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = oc.LoggedAt,
                        EventDetails = oc.OrderChangeLogType.GetDescription() + " ändrade",
                        Actor = oc.UpdatedByUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(oc.UpdatedByUser),
                    });
                    if (oc.OrderChangeConfirmation != null)
                    {
                        eventLog.Add(new EventLogEntryModel
                        {
                            Timestamp = oc.OrderChangeConfirmation.ConfirmedAt,
                            EventDetails = "Bokningsändring bekräftad",
                            Actor = oc.OrderChangeConfirmation.ConfirmedByUser.FullName,
                            Organization = oc.Broker.Name,
                            ActorContactInfo = GetContactinfo(oc.OrderChangeConfirmation.ConfirmedByUser)
                        });
                    }
                }
                eventLog = eventLog.Distinct(new EventLogEntryModel.EventLogEntryComparer()).ToList();
            }
            if (orderStatusConfirmations.Any(os => os.OrderStatus == OrderStatus.NoBrokerAcceptedOrder))
            {
                var c = orderStatusConfirmations.First(os => os.OrderStatus == OrderStatus.NoBrokerAcceptedOrder);
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = c.ConfirmedAt,
                    EventDetails = $"Bekräftat bokningsförfrågan avslutad",
                    Actor = c.ConfirmedByUser.FullName,
                    Organization = customerName,
                    ActorContactInfo = GetContactinfo(c.ConfirmedByUser),
                });
            }

            if (order.Status == OrderStatus.ResponseNotAnsweredByCreator)
            {
                var req = await _dbContext.Requests.GetLastRequestForOrder(orderId);
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = req.LatestAnswerTimeForCustomer ?? order.StartAt,
                    EventDetails = "Obesvarad tillsättning, tiden gick ut, bokning avslutad",
                    Actor = "Systemet",
                });
                // Check if confirmed
                if (orderStatusConfirmations.Any(os => os.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator))
                {
                    OrderStatusConfirmation osc = orderStatusConfirmations.First(os => os.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator);
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = osc.ConfirmedAt,
                        EventDetails = $"Obesvarad tillsättning bekräftad av myndighet",
                        Actor = osc.ConfirmedByUser?.FullName ?? "Systemet",
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(osc.ConfirmedByUser),
                    });
                }
            }
            return eventLog;
        }

        public async Task<List<EventLogEntryModel>> GetEventLogForRequestsOnOrder(int orderId, string customerName, string brokerName, int? brokerId = null)
        {
            var requests = await _dbContext.Requests.GetRequestsForOrderForEventLog(orderId, brokerId).ToListAsync();
            var statusConfirmations = await _dbContext.RequestStatusConfirmation.GetRequestStatusConfirmationsForOrder(orderId).ToListAsync();

            var eventLog = new List<EventLogEntryModel>();

            foreach (Request request in requests)
            {
                eventLog.AddRange(GetEventLogForRequest(request, customerName, brokerId.HasValue ? brokerName : request.Ranking.Broker.Name, statusConfirmations.Where(c => c.RequestId == request.RequestId), brokerId.HasValue));
            }
            //Order change history just in detailed view (for broker, customer has its' own)
            if (brokerId.HasValue)
            {
                var orderChangeLogEntries = await _dbContext.OrderChangeLogEntries.GetOrderChangeLogEntitesWithUserIncludes(orderId)
                    .Where(oc => (oc.OrderChangeLogType != OrderChangeLogType.ContactPerson) && oc.BrokerId == brokerId).OrderBy(ch => ch.LoggedAt).ToListAsync();
                foreach (OrderChangeLogEntry oc in orderChangeLogEntries)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = oc.LoggedAt,
                        EventDetails = oc.OrderChangeLogType.GetDescription() + " ändrade",
                        Actor = oc.UpdatedByUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(oc.UpdatedByUser),
                    });
                    if (oc.OrderChangeConfirmation != null)
                    {
                        eventLog.Add(new EventLogEntryModel
                        {
                            Timestamp = oc.OrderChangeConfirmation.ConfirmedAt,
                            EventDetails = "Bokningsändring bekräftad",
                            Actor = oc.OrderChangeConfirmation.ConfirmedByUser.FullName,
                            Organization = brokerName,
                            ActorContactInfo = GetContactinfo(oc.OrderChangeConfirmation.ConfirmedByUser)
                        });
                    }
                }
                eventLog = eventLog.Distinct(new EventLogEntryModel.EventLogEntryComparer()).ToList();
            }
            else
            {
                if (requests.All(r => r.Status == RequestStatus.DeclinedByBroker || r.Status == RequestStatus.DeniedByTimeLimit))
                {
                    var terminatingRequest = requests.OrderBy(r => r.RequestId).Last();

                    // No one accepted order
                    if (terminatingRequest.Order.Status == OrderStatus.NoBrokerAcceptedOrder)
                    {
                        eventLog.Add(new EventLogEntryModel
                        {
                            Weight = 200,
                            Timestamp = terminatingRequest.Status == RequestStatus.DeniedByTimeLimit
                                ? terminatingRequest.ExpiresAt ?? terminatingRequest.Order.StartAt
                                : terminatingRequest.AnswerDate.Value,
                            EventDetails = "Bokningsförfrågan avslutad, pga avböjd av samtliga förmedlingar",
                            Actor = "Systemet",
                        });
                    }
                    else if (terminatingRequest.Order.Status == OrderStatus.NoDeadlineFromCustomer)
                    {
                        eventLog.Add(new EventLogEntryModel
                        {
                            Weight = 200,
                            Timestamp = terminatingRequest.Order.StartAt,
                            EventDetails = "Bokningsförfrågan avslutad, pga utebliven sista svarstid från myndighet",
                            Actor = "Systemet",
                        });
                    }
                }
            }

            // Add all requisition logs

            var requisitions = _dbContext.Requisitions.GetRequisitionsForOrder(orderId, brokerId);
            eventLog.AddRange(GetEventLogEntriesFromRequisitionList(requisitions, customerName, brokerId.HasValue ? brokerName : null));

            var archived = await _dbContext.RequisitionStatusConfirmations.GetRequisitionsStatusConfirmationsForOrder(orderId, brokerId).FirstOrDefaultAsync(r => r.RequisitionStatus == RequisitionStatus.Created);
            if (archived != null)
            {
                eventLog.Add(GetArchivedRequisitionConfirmation(archived, customerName));
            }

            // Add all complaint logs
            var complaint = await _dbContext.Complaints.GetComplaintForEventLogByOrderId(orderId, brokerId);
            if (complaint != null)
            {
                eventLog.AddRange(GetEventLogForComplaint(complaint, customerName, brokerId.HasValue ? brokerName : null));
            }

            return eventLog;
        }

        public async Task<IEnumerable<EventLogEntryModel>> GetEventLogForRequisitions(int requestId, string customerName, string brokerName)
        {
            List<EventLogEntryModel> list = GetEventLogEntriesFromRequisitionList(_dbContext.Requisitions.GetRequisitionsForRequest(requestId), customerName, brokerName);
            var archived = await _dbContext.RequisitionStatusConfirmations.GetRequisitionsStatusConfirmationsByRequest(requestId).FirstOrDefaultAsync(r => r.RequisitionStatus == RequisitionStatus.Created);
            if (archived != null)
            {
                list.Add(GetArchivedRequisitionConfirmation(archived, customerName));
            }
            return list;
        }

        public IEnumerable<EventLogEntryModel> GetEventLogForComplaint(Complaint complaint, string customerName, string brokerName = null)
        {
            if (complaint == null)
            {
                yield break;
            }
            if (string.IsNullOrEmpty(brokerName))
            {
                brokerName = complaint.Request.Ranking.Broker.Name;
            }
            // Complaint creation
            yield return new EventLogEntryModel
            {
                Timestamp = complaint.CreatedAt,
                EventDetails = "Reklamation registrerad",
                Actor = complaint.CreatedByUser.FullName,
                Organization = customerName,
                ActorContactInfo = GetContactinfo(complaint.CreatedByUser)
            };
            // Complaint answer
            if (complaint.AnsweredAt.HasValue)
            {
                if (complaint.Status == ComplaintStatus.AutomaticallyConfirmedDueToNoAnswer)
                {
                    yield return new EventLogEntryModel
                    {
                        Timestamp = complaint.AnsweredAt.Value,
                        EventDetails = complaint.Status.GetDescription(),
                        Actor = "Systemet",
                    };
                }
                else if (complaint.Status == ComplaintStatus.Confirmed)
                {
                    yield return new EventLogEntryModel
                    {
                        Timestamp = complaint.AnsweredAt.Value,
                        EventDetails = "Reklamation accepterad av förmedling",
                        Actor = complaint.AnsweringUser.FullName,
                        Organization = brokerName,
                        ActorContactInfo = GetContactinfo(complaint.AnsweringUser),
                    };
                }
                else
                {
                    yield return new EventLogEntryModel
                    {
                        Timestamp = complaint.AnsweredAt.Value,
                        EventDetails = "Reklamation är bestriden av förmedling",
                        Actor = complaint.AnsweringUser.FullName,
                        Organization = brokerName,
                        ActorContactInfo = GetContactinfo(complaint.AnsweringUser),
                    };
                }
            }
            // Complaint answer disputation
            if (complaint.AnswerDisputedAt.HasValue)
            {
                if (complaint.Status == ComplaintStatus.TerminatedAsDisputeAccepted)
                {
                    yield return new EventLogEntryModel
                    {
                        Timestamp = complaint.AnswerDisputedAt.Value,
                        EventDetails = "Reklamation är återtagen av myndighet",
                        Actor = complaint.AnswerDisputingUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(complaint.AnswerDisputingUser),
                    };
                }
                else
                {
                    yield return new EventLogEntryModel
                    {
                        Timestamp = complaint.AnswerDisputedAt.Value,
                        EventDetails = "Reklamation kvarstår, avvaktar extern process",
                        Actor = complaint.AnswerDisputingUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(complaint.AnswerDisputingUser),
                    };
                }
            }
            // Complaint termination
            if (complaint.TerminatedAt.HasValue)
            {
                if (complaint.Status == ComplaintStatus.TerminatedTrialConfirmedComplaint)
                {
                    yield return new EventLogEntryModel
                    {
                        Timestamp = complaint.TerminatedAt.Value,
                        EventDetails = "Reklamation avslutad, bistådd av extern process",
                        Actor = complaint.TerminatingUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(complaint.TerminatingUser),
                    };
                }
                else
                {
                    yield return new EventLogEntryModel
                    {
                        Timestamp = complaint.TerminatedAt.Value,
                        EventDetails = "Reklamation avslutad, avslagen av extern process",
                        Actor = complaint.TerminatingUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(complaint.TerminatingUser),
                    };
                }
            }
        }

        private static List<EventLogEntryModel> GetEventLogForRequest(Request request, string customerName, string brokerName, IEnumerable<RequestStatusConfirmation> confirmations, bool isRequestDetailView = true)
        {
            if (request == null)
            {
                return new List<EventLogEntryModel>();
            }
            var eventLog = new List<EventLogEntryModel>();
            if (!request.ReplacingRequestId.HasValue && request.ExpiresAt.HasValue && request.RequestUpdateLatestAnswerTime == null)
            {
                // Request creation
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = request.CreatedAt,
                    EventDetails = isRequestDetailView ? request.Order?.ReplacingOrder != null ? $"Ersättningsuppdrag inkommet (ersätter {request.Order.ReplacingOrder.OrderNumber})" : "Förfrågan inkommen" : $"Förfrågan skickad till {brokerName}",
                    Actor = "Systemet",
                });
            }
            if (request.RequestUpdateLatestAnswerTime != null)
            {
                // Request sent to broker when latest answer time is updated
                if (!isRequestDetailView)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = request.RequestUpdateLatestAnswerTime.UpdatedAt,
                        EventDetails = $"Sista svarstid satt",
                        Actor = request.RequestUpdateLatestAnswerTime.UpdatedByUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(request.RequestUpdateLatestAnswerTime.UpdatedByUser)
                    });
                }
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = request.RequestUpdateLatestAnswerTime.UpdatedAt,
                    EventDetails = isRequestDetailView ? $"Förfrågan inkommen" : $"Förfrågan skickad till {brokerName}",
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
                    Organization = brokerName,
                    ActorContactInfo = GetContactinfo(request.ReceivedByUser),
                });
            }
            // Request expired
            if (request.Status == RequestStatus.DeniedByTimeLimit)
            {
                if (request.LastAcceptAt.HasValue)
                {
                    if (request.AcceptedAt.HasValue)
                    {
                        eventLog.Add(new EventLogEntryModel
                        {
                            Timestamp = request.ExpiresAt ?? request.Order.StartAt,
                            EventDetails = "Förfrågan obesvarad efter bekräftelse, tiden gick ut",
                            Actor = "Systemet",
                        });
                    }
                    else
                    {
                        eventLog.Add(new EventLogEntryModel
                        {
                            Timestamp = request.LastAcceptAt.Value,
                            EventDetails = "Förfrågan obesvarad, tiden gick ut",
                            Actor = "Systemet",
                        });
                    }
                }
                else
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = request.ExpiresAt ?? request.Order.StartAt,
                        EventDetails = "Förfrågan obesvarad, tiden gick ut",
                        Actor = "Systemet",
                    });
                } 
            }
            else if (request.Status == RequestStatus.NoDeadlineFromCustomer)
            {
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = request.Order.StartAt,
                    EventDetails = "Sista svarstid ej satt, tiden gick ut",
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
                        Organization = brokerName,
                        ActorContactInfo = GetContactinfo(request.AnsweringUser),
                    });
                }
                else if (!request.ReplacingRequestId.HasValue || request.ReplacingRequest.Status != RequestStatus.InterpreterReplaced)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = request.AnswerDate.Value,
                        EventDetails = $"Tolk tillsatt av förmedling",
                        Actor = request.AnsweringUser.FullName,
                        Organization = brokerName,
                        ActorContactInfo = GetContactinfo(request.AnsweringUser),
                    });
                }
            }
            if (request.AcceptedAt.HasValue && !request.ReplacingRequestId.HasValue)
            {
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = request.AcceptedAt.Value,
                    EventDetails = $"Förfrågan bekräftad av förmedling",
                    Actor = request.AcceptingUser.FullName,
                    Organization = brokerName,
                    ActorContactInfo = GetContactinfo(request.AcceptingUser),
                });
            }
            // Request answer processed by customer organization or system
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
                    if (confirmations.Any(rs => rs.RequestStatus == RequestStatus.DeniedByCreator))
                    {
                        var confirmation = confirmations.First(rs => rs.RequestStatus == RequestStatus.DeniedByCreator);
                        eventLog.Add(new EventLogEntryModel
                        {
                            Timestamp = confirmation.ConfirmedAt,
                            EventDetails = $"Avböjande bekräftat",
                            Actor = confirmation.ConfirmedByUser.FullName,
                            Organization = brokerName,
                            ActorContactInfo = GetContactinfo(confirmation.ConfirmedByUser),
                        });
                    }
                }
                else
                {
                    //interpreter changed
                    if (request.ReplacingRequestId.HasValue && request.ReplacingRequest.Status == RequestStatus.InterpreterReplaced)
                    {
                        if (request.ProcessingUser != null)
                        {
                            eventLog.Add(new EventLogEntryModel
                            {
                                Weight = 200,
                                Timestamp = request.AnswerProcessedAt.Value,
                                EventDetails = $"Tolkbyte godkänt av myndighet",
                                Actor = request.ProcessingUser.FullName,
                                Organization = customerName,
                                ActorContactInfo = GetContactinfo(request.ProcessingUser),
                            });
                        }
                        //if auto accepted by system 
                        else
                        {
                            eventLog.Add(new EventLogEntryModel
                            {
                                Timestamp = request.AnswerProcessedAt.Value,
                                EventDetails = $"Tolkbyte godkänt av systemet",
                                Weight = 200,
                                Actor = "Systemet",
                            });
                        }
                    }
                    else
                    {
                        if (request.ProcessingUser != null)
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
                        //if auto accepted by system 
                        else
                        {
                            eventLog.Add(new EventLogEntryModel
                            {
                                Timestamp = request.AnswerProcessedAt.Value,
                                EventDetails = $"Tillsättning godkänd av systemet",
                                Weight = 200,
                                Actor = "Systemet",
                            });
                        }
                    }
                }
            }
            else if (isRequestDetailView && request.Status == RequestStatus.ResponseNotAnsweredByCreator)
            {
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = request.LatestAnswerTimeForCustomer ?? request.Order.StartAt,
                    EventDetails = "Obesvarad tillsättning, tiden gick ut, bokning avslutad",
                    Actor = "Systemet",
                });
                // Request no answer confirmations
                if (confirmations.Any(rs => rs.RequestStatus == RequestStatus.ResponseNotAnsweredByCreator))
                {
                    RequestStatusConfirmation rsc = confirmations.First(rs => rs.RequestStatus == RequestStatus.ResponseNotAnsweredByCreator);
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = rsc.ConfirmedAt,
                        EventDetails = $"Obesvarad tillsättning bekräftad av förmedling",
                        Actor = rsc.ConfirmedByUser?.FullName ?? "Systemet",
                        Organization = brokerName,
                        ActorContactInfo = GetContactinfo(rsc.ConfirmedByUser),
                    });
                }
            }
            if (confirmations.Any(rs => rs.RequestStatus == RequestStatus.Approved))
            {
                RequestStatusConfirmation rsc = confirmations.First(rs => rs.RequestStatus == RequestStatus.Approved);
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = rsc.ConfirmedAt,
                    EventDetails = $"Arkiverad utan rekvisition",
                    Actor = rsc.ConfirmedByUser.FullName,
                    Organization = brokerName,
                    ActorContactInfo = GetContactinfo(rsc.ConfirmedByUser),
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
                        Organization = brokerName,
                        ActorContactInfo = GetContactinfo(request.CancelledByUser),
                    });
                }
                // Order replaced, just in detailed view (for broker)
                if (isRequestDetailView && request.Order?.ReplacedByOrder != null)
                {
                    eventLog.Add(new EventLogEntryModel
                    {
                        Timestamp = request.Order.ReplacedByOrder.CreatedAt,
                        EventDetails = $"Uppdrag ersatt av {request.Order.ReplacedByOrder.OrderNumber}",
                        Actor = request.Order.ReplacedByOrder.CreatedByUser.FullName,
                        Organization = customerName,
                        ActorContactInfo = GetContactinfo(request.Order.ReplacedByOrder.CreatedByUser),
                    });
                }
            }
            // Request cancellation confirmation
            if (confirmations.Any(rs => rs.RequestStatus == RequestStatus.CancelledByCreatorWhenApproved || rs.RequestStatus == RequestStatus.CancelledByCreator))
            {
                RequestStatusConfirmation rsc = confirmations.First(rs => rs.RequestStatus == RequestStatus.CancelledByCreatorWhenApproved || rs.RequestStatus == RequestStatus.CancelledByCreator);
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = rsc.ConfirmedAt,
                    EventDetails = $"Avbokning bekräftad av förmedling",
                    Actor = rsc.ConfirmedByUser.FullName,
                    Organization = brokerName,
                    ActorContactInfo = GetContactinfo(rsc.ConfirmedByUser),
                });
            }
            else if (confirmations.Any(rs => rs.RequestStatus == RequestStatus.CancelledByBroker))
            {
                RequestStatusConfirmation rsc = confirmations.First(rs => rs.RequestStatus == RequestStatus.CancelledByBroker);
                eventLog.Add(new EventLogEntryModel
                {
                    Timestamp = rsc.ConfirmedAt,
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
                    Organization = brokerName,
                    ActorContactInfo = GetContactinfo(request.ReplacedByRequest.AnsweringUser),
                });
            }
            return eventLog;
        }

        private static List<EventLogEntryModel> GetEventLogEntriesFromRequisitionList(IQueryable<Requisition> requisitions, string customerName, string brokerName = null)
        {
            var list = new List<EventLogEntryModel>();
            bool getBrokerNameFromRequisition = string.IsNullOrEmpty(brokerName);
            foreach (var requisition in requisitions)
            {
                if (getBrokerNameFromRequisition)
                {
                    brokerName = requisition.Request.Ranking.Broker.Name;
                }
                // Requisition creation
                if (requisition.Status == RequisitionStatus.AutomaticGeneratedFromCancelledOrder)
                {
                    list.Add(new EventLogEntryModel
                    {
                        Timestamp = requisition.CreatedAt,
                        EventDetails = "Rekvisition automatiskt genererad pga avbokning",
                        Actor = "Systemet",
                    });
                }
                else
                {
                    list.Add(new EventLogEntryModel
                    {
                        Timestamp = requisition.CreatedAt,
                        EventDetails = "Rekvisition registrerad",
                        Actor = requisition.CreatedByUser.FullName,
                        Organization = brokerName, //interpreter "works" for broker
                        ActorContactInfo = GetContactinfo(requisition.CreatedByUser),
                    });
                }
                // Requisition processing
                if (requisition.ProcessedAt.HasValue)
                {
                    if (requisition.Status == RequisitionStatus.Reviewed)
                    {
                        list.Add(new EventLogEntryModel
                        {
                            Timestamp = requisition.ProcessedAt.Value,
                            EventDetails = "Rekvisition granskad",
                            Actor = requisition.ProcessedUser.FullName,
                            Organization = customerName,
                            ActorContactInfo = GetContactinfo(requisition.ProcessedUser),
                        });
                    }
                    else if (requisition.Status == RequisitionStatus.Commented)
                    {
                        list.Add(new EventLogEntryModel
                        {
                            Timestamp = requisition.ProcessedAt.Value,
                            EventDetails = "Rekvisition kommenterad",
                            Actor = requisition.ProcessedUser.FullName,
                            Organization = customerName,
                            ActorContactInfo = GetContactinfo(requisition.ProcessedUser),
                        });
                    }
                }
            }

            return list;
        }

        private static string GetContactinfo(AspNetUser user)
        {
            if (user == null)
            {
                return string.Empty;
            }
            string contactInfo = string.Empty;
            if (!string.IsNullOrEmpty(user.Email))
            {
                contactInfo += $"E-post: {user.Email}\n";
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

        private static EventLogEntryModel GetEventRowForNewContactPerson(OrderChangeLogEntry ocPrevious, Order order, IEnumerable<OrderChangeLogEntry> orderContactPersons, int findElementAt)
        {
            //try find next row if any else take info from Order.ContactPersonUser
            string newContactPersonName = orderContactPersons.Count() > findElementAt ? orderContactPersons.ElementAt(findElementAt).OrderContactPersonHistory.PreviousContactPersonUser?.FullName : order.ContactPersonUser?.FullName;
            return string.IsNullOrWhiteSpace(newContactPersonName) ? null : new EventLogEntryModel
            {
                Timestamp = ocPrevious.LoggedAt,
                EventDetails = $"{newContactPersonName} tilldelades rätt att granska rekvisition",
                Actor = ocPrevious.UpdatedByUser.FullName,
                Organization = order.CustomerOrganisation.Name,
                ActorContactInfo = GetContactinfo(ocPrevious.UpdatedByUser),
            };
        }

        private static EventLogEntryModel GetArchivedRequisitionConfirmation(RequisitionStatusConfirmation archived, string customerName)
        {
            return new EventLogEntryModel
            {
                Timestamp = archived.ConfirmedAt,
                EventDetails = "Rekvisition arkiverad",
                Actor = archived.ConfirmedByUser.FullName,
                Organization = customerName,
                ActorContactInfo = GetContactinfo(archived.ConfirmedByUser)
            };
        }
    }
}

