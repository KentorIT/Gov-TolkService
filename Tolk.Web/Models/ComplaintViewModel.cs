using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class ComplaintViewModel : ComplaintModel
    {
        public int ComplaintId { get; set; }

        [Display(Name = "Registrerad av")]
        public string CreatedBy { get; set; }

        [Display(Name = "Registrerad")]
        public DateTimeOffset CreatedAt { get; set; }

        [Display(Name = "Status")]
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
        [Display(Name = "Meddelande vid ogillande av bestridning")]
        public string RefuteMessage { get; set; }

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
                //TODO: Should be Name!
                InterpreterName = complaint.Request.Interpreter.User.Email,
                LanguageName = complaint.Request.Order.OtherLanguage ?? complaint.Request.Order.Language?.Name ?? "-",
                OrderNumber = complaint.Request.Order.OrderNumber.ToString(),
                RegionName = complaint.Request.Ranking.Region.Name,
                AssignmentType = complaint.Request.Order.AssignentType,
                //TODO: Should be Name!
                CreatedBy = complaint.CreatedByUser.Email,
                CreatedAt = complaint.CreatedAt,
                ComplaintType = complaint.ComplaintType,
                Message = complaint.ComplaintMessage,
                Status = complaint.Status,
                DisputeMessage = complaint.AnswerMessage,
                RefuteMessage = complaint.AnswerDisputedMessage,
                IsBroker = isBroker,
                IsCustomer = isCustomer,
            };
        }

        #endregion
    }
}
