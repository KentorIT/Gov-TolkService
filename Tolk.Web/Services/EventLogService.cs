using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Models;
using System.Collections.Generic;

namespace Tolk.Web.Services
{
    public class EventLogService
    {
        private readonly TolkDbContext _dbContext;

        public EventLogService(TolkDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<EventLogEntryModel>> GetEventLogForRequisitions(int requestId, string customerName, string brokerName)
        {
            var requisitions = _dbContext.Requisitions.GetRequisitionsForRequest(requestId);
            var list = new List<EventLogEntryModel>();
                foreach (var requisition in requisitions)
            {
                // Requisition creation
                if (requisition.Status == RequisitionStatus.AutomaticGeneratedFromCancelledOrder)
                {
                    list.Add( new EventLogEntryModel
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
            var archived = await _dbContext.RequisitionStatusConfirmations.GetRequisitionsStatusConfirmationsByRequest(requestId).FirstOrDefaultAsync(r => r.RequisitionStatus == RequisitionStatus.Created);
            if (archived != null)
            {
                list.Add(new EventLogEntryModel
                {
                    Timestamp = archived.ConfirmedAt,
                    EventDetails = "Rekvisition arkiverad",
                    Actor = archived.ConfirmedByUser.FullName,
                    Organization = customerName,
                    ActorContactInfo = GetContactinfo(archived.ConfirmedByUser)
                });
            }
            return list;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Prettier code, really")]
        public IEnumerable<EventLogEntryModel> GetEventLogForComplaint(Complaint complaint, string customerName, string brokerName)
        {
            if (complaint == null)
            {
                yield break;
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

        private static EventLogEntryModel GetEventRowForNewContactPerson(OrderChangeLogEntry ocPrevious, Order order, int findElementAt)
        {

            var orderContactPersons = order.OrderChangeLogEntries.Where(oc => oc.OrderChangeLogType == OrderChangeLogType.ContactPerson).OrderBy(ch => ch.LoggedAt);
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

    }
}

