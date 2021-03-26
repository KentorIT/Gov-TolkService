﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequestModel : IModel
    {
        public int RequestId { get; set; }

        [Display(Name = "Status på förfrågan")]
        public RequestStatus Status { get; set; }

        public int BrokerId { get; set; }

        public int? ReplacingOrderRequestId { get; set; }

        public RequestStatus? ReplacedByOrderRequestStatus { get; set; }

        public int? ReplacedByOrderRequestId { get; set; }

        public int? RequestGroupId { get; set; }

        public OrderViewModel OrderViewModel { get; set; }

        public RequestViewModel ProcessReplacementRequestViewModel { get; set; }

        public int? OrderId
        {
            get
            {
                return OrderViewModel?.OrderId;
            }
        }

        public string ViewedByUser { get; set; } = string.Empty;

        public List<FileModel> Files { get; set; }

        public AttachmentListModel AttachmentListModel { get; set; }

        public Guid? FileGroupKey { get; set; }

        public RequestStatus? RequestGroupStatus { get; set; }

        public string GroupStatusCssClassColor => RequestGroupStatus.HasValue ? CssClassHelper.GetColorClassNameForRequestStatus(RequestGroupStatus.Value) : string.Empty;

        public long? CombinedMaxSizeAttachments { get; set; }

        [Display(Name = "Förfrågan besvarad av")]
        [DataType(DataType.MultilineText)]
        public string AnsweredBy { get; set; }

        public string AnswerProcessedBy { get; set; }

        public string AnswerProcessedAt { get; set; }

        [Display(Name = "Förmedling")]
        public string BrokerName { get; set; }

        [Display(Name = "Förmedlings organisationsnummer")]
        public string BrokerOrganizationNumber { get; set; }

        [Display(Name = "Orsak till avböjande")]
        [DataType(DataType.MultilineText)]
        [Required]
        [StringLength(1000)]
        [Placeholder("Beskriv anledning tydligt.")]
        public string DenyMessage { get; set; }


        [Display(Name = "Orsak till avbokning")]
        [DataType(DataType.MultilineText)]
        [Required]
        [StringLength(1000)]
        [Placeholder("Beskriv orsak till avbokning.")]
        public string CancelMessage { get; set; }

        public string Info48HCancelledByCustomer { get; set; }

        [Required]
        [Display(Name = "Tolkens kompetensnivå")]
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevel { get; set; }

        [Required]
        [Display(Name = "Tolk", Description = "I de fall tillsatt tolk har skyddad identitet skall inte tolkens namn eller kontaktuppgifter finnas i bekräftelsen. Använd i dessa fall valet ”Tolk med skyddade personuppgifter”. Överlämna tolkens uppgifter på annat sätt i enlighet med era säkerhetsrutiner")]
        public int? InterpreterId { get; set; }

        [Required]
        [EmailAddress]
        [RegularExpression(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$", ErrorMessage = "Felaktig e-postadress")]
        [Display(Name = "Tolkens e-postadress")]
        [StringLength(255)]
        public string NewInterpreterEmail { get; set; }

        [Required]
        [Display(Name = "Tolkens förnamn")]
        [StringLength(255)]
        public string NewInterpreterFirstName { get; set; }

        [Required]
        [Display(Name = "Tolkens efternamn")]
        [StringLength(255)]
        public string NewInterpreterLastName { get; set; }

        [Display(Name = "Kammarkollegiets tolknummer")]
        public string NewInterpreterOfficialInterpreterId { get; set; }

        [Required]
        [Display(Name = "Tolkens telefonnummer")]
        [StringLength(255)]
        public string NewInterpreterPhoneNumber { get; set; }

        public List<RequestRequirementAnswerModel> RequirementAnswers { get; set; }

        public List<RequestRequirementAnswerModel> RequiredRequirementAnswers { get; set; }

        public List<RequestRequirementAnswerModel> DesiredRequirementAnswers { get; set; }

        public int? RequisitionId { get; set; }

        [Range(0, 999999, ErrorMessage = "Kontrollera värdet för resekostnad")]
        [RegularExpression(@"^[^.]*$", ErrorMessage = "Värdet får inte innehålla punkttecken, ersätt med kommatecken")] // validate.js regex allows dots, despite not explicitly allowing them
        [ClientRequired(ErrorMessage = "Ange resekostnad (endast siffror, ange 0 om det inte finns någon kostnad)")]
        [Display(Name = "Bedömd resekostnad")]
        [DataType(DataType.Currency)]
        [Placeholder("Ange i SEK")]
        public decimal? ExpectedTravelCosts { get; set; }

        [Display(Name = "Kommentar till bedömd resekostnad", Description = "Här kan du kommentera den bedömda resekostnaden som angivits genom att skriva in t ex antal km för bilersättning, eventuella biljettkostnader, spilltid mm")]
        [Placeholder("Ange t ex bedömt antal km, biljettkostnad, spilltid mm")]
        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        public string ExpectedTravelCostInfo { get; set; }

        [ClientRequired]
        [Display(Name = "Inställelsesätt")]
        public InterpreterLocation? InterpreterLocation { get; set; }

        [Display(Name = "Inställelsesätt enl. svar")]
        public InterpreterLocation? InterpreterLocationAnswer
        {
            get; set;
        }

        [Display(Name = "Inkommen")]
        public DateTimeOffset? CreatedAt { get; set; }

        [Display(Name = "Svara senast")]
        public DateTimeOffset? ExpiresAt { get; set; }

        [Display(Name = "Förmedlingens bokningsnummer", Description = "Här kan ni som förmedling ange ett eget bokningsnummer att koppla till bokningen.")]
        public string BrokerReferenceNumber { get; set; }

        public bool AllowProcessing { get; set; } = true;

        public bool DisplayExpectedTravelCostInfo { get; set; }

        public EventLogModel EventLog { get; set; }

        public string ColorClassName => CssClassHelper.GetColorClassNameForRequestStatus(Status);

        [DataType(DataType.MultilineText)]
        [Display(Name = "Bokningsändringar")]
        public string DisplayOrderChangeText { get; set; }

        [Display(Name = "Vill du ange en sista tid för att besvara tillsättning för myndighet", Description = "Ange om du vill sätta en tid för när myndigheten senast ska besvara tillsättningen. Om du anger en tid och myndigheten inte svarar inom angiven tid avslutas förfrågan.")]
        [ClientRequired]
        public RadioButtonGroup SetLatestAnswerTimeForCustomer { get; set; }

        [Display(Name = "Sista tid att besvara tillsättning", Description = "Här har förmedlingen möjlighet att ange en tid för när myndigheten senast ska besvara tillsättningen. Om myndigheten inte svarar inom angiven tid avslutas förfrågan.")]
        [ClientRequired]
        public DateTimeOffset? LatestAnswerTimeForCustomer { get; set; }

        #region view stuff

        [Display(Name = "Tillsatt tolk")]
        [DataType(DataType.MultilineText)]
        public string Interpreter { get; set; }

        public bool? IsInterpreterVerified { get; set; }

        public string InterpreterVerificationMessage { get; set; }

        public PriceInformationModel OrderCalculatedPriceInformationModel { get; set; }

        public PriceInformationModel RequestCalculatedPriceInformationModel { get; set; }

        public int? ComplaintId { get; set; }

        public int? OldInterpreterId { get; set; }

        public int? OtherInterpreterId { get; set; }

        #endregion

        #region methods

        internal static RequestModel GetModelFromRequest(Request request)
        {           
            return new RequestModel
            {
                Status = request.Status,
                AnsweredBy = request.AnsweringUser?.CompleteContactInformation,
                BrokerName = request.Ranking?.Broker?.Name,
                BrokerOrganizationNumber = request.Ranking?.Broker?.OrganizationNumber,
                DenyMessage = request.DenyMessage,
                CancelMessage = request.CancelMessage,
                RequestGroupId = request.RequestGroupId,
                RequestGroupStatus = request.RequestGroup?.Status,
                RequestId = request.RequestId,
                CreatedAt = request.CreatedAt,
                ExpiresAt = request.ExpiresAt,
                BrokerReferenceNumber = request.BrokerReferenceNumber,
                Interpreter = request.Interpreter?.CompleteContactInformation,
                InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)request.CompetenceLevel,
                RequirementAnswers = request.Order.Requirements.Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    IsRequired = r.IsRequired,
                    Description = r.Description,
                    RequirementType = r.RequirementType,
                    Answer = request.RequirementAnswers != null ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId)?.Answer : string.Empty,
                    CanMeetRequirement = request.RequirementAnswers != null && (request.RequirementAnswers.Any() && request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId).CanSatisfyRequirement),
                }).ToList(),
                RequiredRequirementAnswers = request.Order.Requirements.Where(r => r.IsRequired).Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    IsRequired = true,
                    Description = r.Description,
                    RequirementType = r.RequirementType,
                    Answer = request.RequirementAnswers != null ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId)?.Answer : string.Empty,
                    CanMeetRequirement = request.RequirementAnswers != null && (request.RequirementAnswers.Any() && request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId).CanSatisfyRequirement),
                }).ToList(),
                DesiredRequirementAnswers = request.Order.Requirements.Where(r => !r.IsRequired).Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    IsRequired = false,
                    Description = r.Description,
                    RequirementType = r.RequirementType,
                    Answer = request.RequirementAnswers != null ? request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId)?.Answer : string.Empty,
                    CanMeetRequirement = request.RequirementAnswers != null && (request.RequirementAnswers.Any() && request.RequirementAnswers.FirstOrDefault(ra => ra.OrderRequirementId == r.OrderRequirementId).CanSatisfyRequirement),
                }).ToList(),
                InterpreterLocation = request.InterpreterLocation.HasValue ? (InterpreterLocation?)request.InterpreterLocation.Value : null,
                OrderViewModel = OrderViewModel.GetModelFromOrder(request.Order, request, true, true),
                LatestAnswerTimeForCustomer = request.LatestAnswerTimeForCustomer,
            };
        }

        #endregion
    }
}
