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

        [Display(Name = "tatus")]
        public ComplaintStatus Status { get; set; }

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

        public bool AllowAnwserOnDispute
        {
            get
            {
                return Status == ComplaintStatus.Disputed && IsCustomer;
            }
        }

        public EventLogModel EventLog { get; set; }

        #region methods

        public static ComplaintViewModel GetViewModelFromComplaint(Complaint complaint, bool isBroker, bool isCustomer)
        {
            return new ComplaintViewModel
            {
                ComplaintId = complaint.ComplaintId,
                RequestId = complaint.RequestId,
                BrokerName = complaint.Request.Ranking.Broker.Name,
                CustomerName = complaint.Request.Order.CustomerOrganisation.Name,
                CustomerReferenceNumber = complaint.Request.Order.CustomerReferenceNumber,
                InterpreterName = complaint.Request.Interpreter.User.CompleteContactInformation,
                LanguageName = complaint.Request.Order.OtherLanguage ?? complaint.Request.Order.Language?.Name ?? "-",
                OrderNumber = complaint.Request.Order.OrderNumber.ToString(),
                RegionName = complaint.Request.Ranking.Region.Name,
                AssignmentType = complaint.Request.Order.AssignentType,
                CreatedBy = complaint.CreatedByUser.CompleteContactInformation,
                CreatedAt = complaint.CreatedAt,
                ComplaintType = complaint.ComplaintType,
                Message = complaint.ComplaintMessage,
                Status = complaint.Status,
                DisputeMessage = complaint.AnswerMessage,
                AnswerDisputedMessage = complaint.AnswerDisputedMessage,
                IsBroker = isBroker,
                IsCustomer = isCustomer,
                EventLog = new EventLogModel { Entries = EventLogHelper.GetEventLog(complaint).OrderBy(e => e.Timestamp).ToList() },
            };
        }

        #endregion
    }
}
