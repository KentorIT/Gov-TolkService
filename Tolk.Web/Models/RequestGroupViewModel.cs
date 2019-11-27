﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class RequestGroupViewModel : RequestGroupBaseModel
    {
        public bool AllowConfirmationDenial { get; set; } = false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<RequestRequirementAnswerModel> RequirementAnswers { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<RequestRequirementAnswerModel> RequiredRequirementAnswers { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<RequestRequirementAnswerModel> DesiredRequirementAnswers { get; set; }


        [Display(Name = "Tillsatt tolk")]
        [DataType(DataType.MultilineText)]
        public string Interpreter { get; set; }

        [Display(Name = "Tillsatt extra tolk")]
        [DataType(DataType.MultilineText)]
        public string ExtraInterpreter { get; set; }

        public bool? IsInterpreterVerified { get; set; }

        public bool? IsExtraInterpreterInterpreterVerified { get; set; }

        public string InterpreterVerificationMessage { get; set; }

        public string ExtraInterpreterInterpreterVerificationMessage { get; set; }

        public bool DisplayExpectedTravelCostInfo { get; set; }

        [Display(Name = "Förmedling")]
        public string BrokerName { get; set; }

        [Display(Name = "Förmedlings organisationsnummer")]
        public string BrokerOrganizationNumber { get; set; }

        [Display(Name = "Förfrågan besvarad av")]
        [DataType(DataType.MultilineText)]
        public string AnsweredBy { get; set; }

        [Display(Name = "Bedömd resekostnad")]
        [DataType(DataType.Currency)]
        public decimal? ExpectedTravelCosts { get; set; }

        [Display(Name = "Kommentar till bedömd resekostnad")]
        [DataType(DataType.MultilineText)]
        public string ExpectedTravelCostInfo { get; set; }

        [Display(Name = "Bedömd resekostnad för extra tolk")]
        [DataType(DataType.Currency)]
        public decimal? ExtraInterpreterExpectedTravelCosts { get; set; }

        [Display(Name = "Kommentar till bedömd resekostnad för extra tolk")]
        [DataType(DataType.MultilineText)]
        public string ExtraInterpreterExpectedTravelCostInfo { get; set; }

        [Display(Name = "Status på bokning")]
        public OrderStatus OrderStatus { get; set; }

        [Required]
        [Display(Name = "Tolks kompetensnivå")]
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevel { get; set; }

        [Required]
        [Display(Name = "Extra tolks kompetensnivå")]
        public CompetenceAndSpecialistLevel? ExtraInterpreterCompetenceLevel { get; set; }

        public RequestStatus? ExtraInterpreterStatus { get; set; }

        public bool RequestIsAnswered => Status != RequestStatus.InterpreterReplaced && Status != RequestStatus.Created && Status != RequestStatus.Received;

        public bool RequestIsDeclinedByBroker => Status == RequestStatus.DeclinedByBroker || Status == RequestStatus.DeniedByTimeLimit;

        public bool ExtraInterpreterRequestIsAnswered => ExtraInterpreterStatus != RequestStatus.InterpreterReplaced && ExtraInterpreterStatus != RequestStatus.Created && ExtraInterpreterStatus != RequestStatus.Received;

        public bool ExtraInterpreterRequestIsDeclinedByBroker => ExtraInterpreterStatus == RequestStatus.DeclinedByBroker || ExtraInterpreterStatus == RequestStatus.DeniedByTimeLimit;


        #region methods

        internal static RequestGroupViewModel GetModelFromRequestGroup(RequestGroup requestGroup)
        {
            return new RequestGroupViewModel
            {
                OrderGroupNumber = requestGroup.OrderGroup.OrderGroupNumber,
                Status = requestGroup.Status,
                CreatedAt = requestGroup.CreatedAt,
                RequestGroupId = requestGroup.RequestGroupId
            };
        }

        internal static RequestGroupViewModel GetModelFromRequestGroupCustomer(RequestGroup requestGroup)
        {
            OrderGroup orderGroup = requestGroup.OrderGroup;
            Order order = requestGroup.Requests.First().Order;
            Request request = requestGroup.FirstRequestForFirstInterpreter;
            Request requestExtraInterpreter = requestGroup.HasExtraInterpreter ? requestGroup.FirstRequestForExtraInterpreter : null;

            var verificationResult = (request.InterpreterCompetenceVerificationResultOnStart ?? request.InterpreterCompetenceVerificationResultOnAssign);
            bool? isInterpreterVerified = verificationResult.HasValue ? (bool?)(verificationResult == VerificationResult.Validated) : null;
            var extraInterpreterVerificationResult = requestExtraInterpreter != null ? (requestExtraInterpreter.InterpreterCompetenceVerificationResultOnStart ?? requestExtraInterpreter.InterpreterCompetenceVerificationResultOnAssign) : null;
            bool? isExtraInterpreterVerified = extraInterpreterVerificationResult.HasValue ? (bool?)(extraInterpreterVerificationResult == VerificationResult.Validated) : null;

            return new RequestGroupViewModel
            {

                OrderGroupId = requestGroup.OrderGroupId,
                RequestGroupId = requestGroup.RequestGroupId,
                OrderGroupNumber = orderGroup.OrderGroupNumber,
                AnsweredBy = request.AnsweringUser?.CompleteContactInformation,
                BrokerName = request.Ranking?.Broker?.Name,
                BrokerOrganizationNumber = request.Ranking?.Broker?.OrganizationNumber,
                DenyMessage = request.DenyMessage,
                CancelMessage = request.CancelMessage,
                ExpiresAt = requestGroup.ExpiresAt.Value,
                AttachmentListModel = new AttachmentListModel
                {
                    AllowDelete = false,
                    AllowDownload = true,
                    AllowUpload = false,
                    Title = "Bifogade filer från förmedling",
                    DisplayFiles = requestGroup.Attachments.Select(a => new FileModel
                    {
                        Id = a.Attachment.AttachmentId,
                        FileName = a.Attachment.FileName,
                        Size = a.Attachment.Blob.Length
                    }).ToList()
                },
                //svaren är inte kopplade här ännu annars bör man bara visa första här? om ej ändras
                InterpreterAnswerModel = new InterpreterAnswerModel
                {
                    RequiredRequirementAnswers = orderGroup.Requirements.Where(r => r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderGroupRequirementId,
                        IsRequired = true,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                    }).ToList(),
                    DesiredRequirementAnswers = orderGroup.Requirements.Where(r => !r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderGroupRequirementId,
                        IsRequired = false,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                    }).ToList(),
                },
                ExtraInterpreterAnswerModel = requestGroup.HasExtraInterpreter ? new InterpreterAnswerModel
                {
                    RequiredRequirementAnswers = orderGroup.Requirements.Where(r => r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderGroupRequirementId,
                        IsRequired = true,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                    }).ToList(),
                    DesiredRequirementAnswers = orderGroup.Requirements.Where(r => !r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderGroupRequirementId,
                        IsRequired = false,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                    }).ToList(),
                } : null,
                HasExtraInterpreter = requestGroup.HasExtraInterpreter,
                AllowExceedingTravelCost = orderGroup.AllowExceedingTravelCost == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldBeApproved || orderGroup.AllowExceedingTravelCost == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldNotBeApproved,
                Description = order.Description,
                LanguageName = orderGroup.LanguageName,
                Dialect = orderGroup.Requirements.Any(r => r.RequirementType == RequirementType.Dialect) ? orderGroup.Requirements.Single(r => r.RequirementType == RequirementType.Dialect)?.Description : string.Empty,
                LanguageHasAuthorizedInterpreter = orderGroup.LanguageHasAuthorizedInterpreter,
                InterpreterLocation = request.InterpreterLocation.HasValue ? (InterpreterLocation?)request.InterpreterLocation.Value : null,
                DisplayExpectedTravelCostInfo = GetDisplayExpectedTravelCostInfo(request.Order, request.InterpreterLocation ?? 0),
                Interpreter = request.Interpreter?.CompleteContactInformation,
                ExtraInterpreter = requestGroup.HasExtraInterpreter ? requestExtraInterpreter.Interpreter?.CompleteContactInformation : null,

                IsInterpreterVerified = verificationResult.HasValue ? (bool?)(verificationResult == VerificationResult.Validated) : null,
                IsExtraInterpreterInterpreterVerified = extraInterpreterVerificationResult.HasValue ? (bool?)(extraInterpreterVerificationResult == VerificationResult.Validated) : null,
                InterpreterVerificationMessage = verificationResult.HasValue ? verificationResult.Value.GetDescription() : null,
                ExtraInterpreterInterpreterVerificationMessage = extraInterpreterVerificationResult.HasValue ? extraInterpreterVerificationResult.Value.GetDescription() : null,
                InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)request.CompetenceLevel,
                ExtraInterpreterCompetenceLevel = requestGroup.HasExtraInterpreter ? (CompetenceAndSpecialistLevel?)requestExtraInterpreter.CompetenceLevel : null,
                ExpectedTravelCosts = request.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0,
                ExpectedTravelCostInfo = request.ExpectedTravelCostInfo,
                ExtraInterpreterExpectedTravelCosts = requestGroup.HasExtraInterpreter ? requestExtraInterpreter.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0 : 0,
                ExtraInterpreterExpectedTravelCostInfo = requestGroup.HasExtraInterpreter ? requestExtraInterpreter.ExpectedTravelCostInfo : null,
                RegionName = orderGroup.Region.Name,
                SpecificCompetenceLevelRequired = orderGroup.SpecificCompetenceLevelRequired,
                Status = request.Status,
                ExtraInterpreterStatus = requestExtraInterpreter?.Status,
                OrderStatus = orderGroup.Status,
                RequirementAnswers = request.Order.Requirements.Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    IsRequired = r.IsRequired,
                    Description = r.Description,
                    RequirementType = r.RequirementType,
                    Answer = request.RequirementAnswers != null ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId)?.Answer : string.Empty,
                    CanMeetRequirement = request.RequirementAnswers != null ? request.RequirementAnswers.Any() ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId).CanSatisfyRequirement : false : false,
                }).ToList(),
                RequiredRequirementAnswers = request.Order.Requirements.Where(r => r.IsRequired).Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    IsRequired = true,
                    Description = r.Description,
                    RequirementType = r.RequirementType,
                    Answer = request.RequirementAnswers != null ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId)?.Answer : string.Empty,
                    CanMeetRequirement = request.RequirementAnswers != null ? request.RequirementAnswers.Any() ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId).CanSatisfyRequirement : false : false,
                }).ToList(),
                DesiredRequirementAnswers = request.Order.Requirements.Where(r => !r.IsRequired).Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    IsRequired = false,
                    Description = r.Description,
                    RequirementType = r.RequirementType,
                    Answer = request.RequirementAnswers != null ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId)?.Answer : string.Empty,
                    CanMeetRequirement = request.RequirementAnswers != null ? request.RequirementAnswers.Any() ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId).CanSatisfyRequirement : false : false,
                }).ToList()
            };
        }

        private static bool GetDisplayExpectedTravelCostInfo(Order o, int locationAnswer)
        {
            if (o.AllowExceedingTravelCost.HasValue && (o.AllowExceedingTravelCost.Value == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldBeApproved || o.AllowExceedingTravelCost.Value == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldNotBeApproved))
            {
                return locationAnswer == (int)BusinessLogic.Enums.InterpreterLocation.OnSite || locationAnswer == (int)BusinessLogic.Enums.InterpreterLocation.OffSiteDesignatedLocation;
            }
            return false;
        }

        #endregion
    }
}
