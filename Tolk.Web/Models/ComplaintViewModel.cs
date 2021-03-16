using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;
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

        [DataType(DataType.MultilineText)]
        [ClientRequired]
        [Display(Name = "Meddelande vid bestridande")]
        [StringLength(1000)]
        [Placeholder("Beskriv anledning till bestridande.")]
        public string DisputeMessage { get; set; }

        [DataType(DataType.MultilineText)]
        [ClientRequired]
        [Display(Name = "Meddelande vid svar på bestridande")]
        [StringLength(1000)]
        [Placeholder("Skriv svar angående bestridande.")]
        public string AnswerDisputedMessage { get; set; }

        [DataType(DataType.MultilineText)]
        [ClientRequired]
        [Display(Name = "Meddelande vid svar på bestridande")]
        [StringLength(1000)]
        [Placeholder("Skriv svar angående bestridande.")]
        public string RefuteMessage { get; set; }

        public bool IsBroker { get; set; }

        public bool IsCustomer { get; set; }

        public bool AllowAnwser => Status == ComplaintStatus.Created && IsBroker;

        public bool IsAdmin { get; set; } = false;

        public bool AllowAnwserOnDispute { get; set; } = false;

        public EventLogModel EventLog { get; set; }

        #region methods

        internal static ComplaintViewModel GetViewModelFromComplaint(Complaint complaint, string eventLogPath)
        {
            string customerName = complaint.Request.Order.CustomerOrganisation.Name;
            string brokerName = complaint.Request.Ranking.Broker.Name;
            return new ComplaintViewModel
            {
                ComplaintId = complaint.ComplaintId,
                BrokerName = brokerName,
                CustomerName = customerName,
                CustomerReferenceNumber = complaint.Request.Order.CustomerReferenceNumber,
                InterpreterName = complaint.Request.Interpreter.CompleteContactInformation,
                CreatedBy = complaint.CreatedByUser.CompleteContactInformation,
                CreatedAt = complaint.CreatedAt,
                ComplaintType = complaint.ComplaintType,
                Message = complaint.ComplaintMessage,
                Status = complaint.Status,
                DisputeMessage = complaint.AnswerMessage,
                AnswerDisputedMessage = complaint.AnswerDisputedMessage,
                EventLog = new EventLogModel
                {
                    Header = "Reklamationshändelser",
                    Id = "EventLog_Complaints",
                    DynamicLoadPath = eventLogPath,
                },
            };
        }

        #endregion
    }
}
