using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class ComplaintViewModel : ComplaintModel
    {
        public int ComplaintId { get; set; }

        [Display(Name = "Reklamation registrerad av")]
        [DataType(DataType.MultilineText)]
        public string CreatedBy { get; set; }

        [Display(Name = "Reklamation registrerad")]
        public DateTimeOffset CreatedAt { get; set; }

        [Display(Name = "Status")]
        public ComplaintStatus Status { get; set; }

        public string ColorClassName { get => CssClassHelper.GetColorClassNameForComplaintStatus(Status); }

        [Display(Name = "Typ av tolkuppdrag")]
        [Required]
        public AssignmentType AssignmentType { get; set; }

        [DataType(DataType.MultilineText)]
        [ClientRequired]
        [Display(Name = "Meddelande vid bestridande")]
        public string DisputeMessage { get; set; }

        [DataType(DataType.MultilineText)]
        [ClientRequired]
        [Display(Name = "Meddelande vid svar på bestridande")]
        public string AnswerDisputedMessage { get; set; }

        public bool IsBroker { get; set; }

        public bool IsCustomer { get; set; }

        public bool AllowAnwser
        {
            get
            {
                return Status == ComplaintStatus.Created && IsBroker;
            }
        }

        public bool AllowAnwserOnDispute { get; set; } = false;

        public EventLogModel EventLog { get; set; }

        #region methods

        public static ComplaintViewModel GetViewModelFromComplaint(Complaint complaint, bool isCustomer)
        {
            return new ComplaintViewModel
            {
                ComplaintId = complaint.ComplaintId,
                BrokerName = complaint.Request.Ranking.Broker.Name,
                CustomerName = complaint.Request.Order.CustomerOrganisation.Name,
                CustomerReferenceNumber = complaint.Request.Order.CustomerReferenceNumber,
                InterpreterName = complaint.Request.Interpreter.CompleteContactInformation,
                CreatedBy = complaint.CreatedByUser.CompleteContactInformation,
                CreatedAt = complaint.CreatedAt,
                ComplaintType = complaint.ComplaintType,
                Message = complaint.ComplaintMessage,
                Status = complaint.Status,
                DisputeMessage = complaint.AnswerMessage,
                AnswerDisputedMessage = complaint.AnswerDisputedMessage,
                IsBroker = !isCustomer,
                EventLog = new EventLogModel { Entries = EventLogHelper.GetEventLog(complaint).OrderBy(e => e.Timestamp).ToList() },
            };
        }

        #endregion
    }
}
