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

namespace Tolk.Web.Models
{
    public class RequestModel
    {
        public int RequestId { get; set; }

        public int BrokerId { get; set; }

        public RequestStatus SetStatus { get; set; }

        public OrderModel OrderModel { get; set; }

        public int OrderId
        {
            get
            {
                return OrderModel.OrderId.Value;
            }
        }

        [Display(Name = "Meddelande")]
        [DataType(DataType.MultilineText)]
        public string DenyMessage { get; set; }

        [Required]
        [Display(Name = "Kompetensnivå")]
        public CompetenceAndSpecialistLevel? CompetenceLevel { get; set; }

        [Required]
        [Display(Name = "Tolk")]
        public int? InterpreterId { get; set; }

        public List<RequestRequirementAnswerModel> RequirementAnswers { get; set; }

        [Display(Name = "Förväntad resekostnad")]
        public decimal? ExpectedTravelCosts { get; set; }

        #region methods

        public static RequestModel GetModelFromRequest(Request request)
        {
            return new RequestModel
            {
                RequestId = request.RequestId,
                RequirementAnswers = request.Order.Requirements.Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    IsRequired = r.IsRequired,
                    Requirement = $"{r.Description}({EnumHelper.GetDescription(r.RequirementType)}){(r.IsRequired ? " krav": string.Empty)}"
                }).ToList(),
                OrderModel = OrderModel.GetModelFromOrder(request.Order),
            };
        }

        #endregion
    }
}
