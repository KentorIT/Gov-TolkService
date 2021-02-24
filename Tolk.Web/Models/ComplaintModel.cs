using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;

namespace Tolk.Web.Models
{
    public class ComplaintModel : IModel
    {
        public int RequestId { get; set; }

        public int OrderId { get; set; }

        [Display(Name = "BokningsID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Språk")]
        public string LanguageName { get; set; }

        [Display(Name = "Län")]
        public string RegionName { get; set; }

        [Display(Name = "Myndighetens ärendenummer")]
        public string CustomerReferenceNumber { get; set; }

        [Display(Name = "Tolk")]
        [DataType(DataType.MultilineText)]
        public string InterpreterName { get; set; }

        [Display(Name = "Förmedling")]
        public string BrokerName { get; set; }

        [Display(Name = "Myndighet")]
        public string CustomerName { get; set; }

        [Display(Name = "Startid")]
        public DateTimeOffset StartAt { get; set; }

        [Display(Name = "Sluttid")]
        public DateTimeOffset EndAt { get; set; }

        [Display(Name = "Typ av reklamation")]
        [Required]
        public ComplaintType? ComplaintType { get; set; }

        [DataType(DataType.MultilineText)]
        [Required]
        [Display(Name = "Reklamationsbeskrivning")]
        [Placeholder("Orsak till reklamation. Beakta eventuell sekretess avseende informationen.")]
        [StringLength(1000)]
        public string Message { get; set; }

        #region methods

        internal static ComplaintModel GetModelFromRequest(Request request)
        {
            return new ComplaintModel
            {
                OrderId = request.OrderId,
                RequestId = request.RequestId,
                BrokerName = request.Ranking.Broker.Name,
                CustomerName = request.Order.CustomerOrganisation.Name,
                CustomerReferenceNumber = request.Order.CustomerReferenceNumber,
                EndAt = request.Order.EndAt,
                StartAt = request.Order.StartAt,
                InterpreterName = request.Interpreter.CompleteContactInformation,
                LanguageName = request.Order.OtherLanguage ?? request.Order.Language?.Name ?? "-",
                OrderNumber = request.Order.OrderNumber,
                RegionName = request.Ranking.Region.Name,
            };
        }

        #endregion
    }
}
