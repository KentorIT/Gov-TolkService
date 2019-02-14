using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequisitionModel
    {
        public int RequestId { get; set; }

        [Display(Name = "BokningsID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Språk")]
        public string LanguageName { get; set; }

        [Display(Name = "Län")]
        public string RegionName { get; set; }

        [Display(Name = "Myndighetens referensnummer")]
        public string CustomerReferenceNumber { get; set; }

        [Display(Name = "Tolk")]
        [DataType(DataType.MultilineText)]
        public string Interpreter { get; set; }

        [Display(Name = "Tolkens skattsedel")]
        [ClientRequired]
        public TaxCard? InterpreterTaxCard { get; set; }

        [Display(Name = "Tolkförmedling")]
        public string BrokerName { get; set; }

        [Display(Name = "Tolkförmedlingens organisationsnummer")]
        public string BrokerOrganizationnumber { get; set; }

        [Display(Name = "Myndighet")]
        public string CustomerOrganizationName { get; set; }

        [Display(Name = "Myndighet")]
        [DataType(DataType.MultilineText)]
        public string CustomerCompactInfo
        { get => CustomerOrganizationName + (string.IsNullOrWhiteSpace(CustomerReferenceNumber) ? string.Empty : "\nReferensnummer: " + CustomerReferenceNumber); }

        [Display(Name = "Bokning skapad av")]
        [DataType(DataType.MultilineText)]
        public string OrderCreatedBy { get; set; }

        [Display(Name = "Rekvisition registrerad av")]
        [DataType(DataType.MultilineText)]
        public string RequisitionCreatedBy { get; set; }
                          
        [Display(Name = "Förväntad resekostnad (exkl. moms) i SEK")]
        [DataType(DataType.Currency)]
        public decimal ExpectedTravelCosts { get; set; }

        [Display(Name = "Faktisk resekostnad (exkl. moms) i SEK")]
        [DataType(DataType.Currency)]
        public decimal TotalTravelCosts { get => (Outlay ?? 0) + (PerDiem ?? 0) + (CarCompensation ?? 0); }

        [Display(Name = "Utlägg för resa (exkl. moms) i SEK", Description = "Uppgift om utlägg måste anges. Finns inga kostnader anges 0.")]
        [RegularExpression("^[0-9]{1,6}(\\.[0-9]{0,4})?$", ErrorMessage = "Kontrollera värdet för utlägg (inga bokstäver tillåtna, ange 0 om det inte finns någon kostnad)")]
        [ClientRequired(ErrorMessage = "Ange utlägg (inga bokstäver tillåtna, ange 0 om det inte finns någon kostnad)")]
        [DataType(DataType.Currency)]
        public decimal? Outlay { get; set; }

        [Display(Name = "Bilersättning (exkl. moms) i SEK", Description = "Bilersättning ska anges. Finns ingen kostnad för bilersättning anges 0.")]
        [RegularExpression("^[0-9]{1,6}(\\.[0-9]{0,4})?$", ErrorMessage = "Kontrollera värdet för bilersättning (inga bokstäver tillåtna, ange 0 om det inte finns någon kostnad för bilersättning)")]
        [ClientRequired(ErrorMessage = "Ange bilersättning (inga bokstäver tillåtna, ange 0 om det inte finns någon kostnad)")]
        [DataType(DataType.Currency)]
        public decimal? CarCompensation { get; set; }

        [Display(Name = "Traktamente (exkl. moms) i SEK", Description = "Traktamente ska anges. Om inget traktamente ska erhållas ange 0.")]
        [RegularExpression("^[0-9]{1,6}(\\.[0-9]{0,4})?$", ErrorMessage = "Kontrollera värdet för traktamente (inga bokstäver tillåtna, ange 0 om det inte ska erhållas något traktamente)")]
        [ClientRequired(ErrorMessage = "Ange traktamente (inga bokstäver tillåtna, ange 0 om det inte ska erhållas något traktamente)")]
        [DataType(DataType.Currency)]
        public decimal? PerDiem { get; set; }

        [Display(Name = "Förväntad startid")]
        public DateTimeOffset ExpectedStartedAt { get; set; }

        [Display(Name = "Förväntad sluttid")]
        public DateTimeOffset ExpectedEndedAt { get; set; }

        [Range(31, 600, ErrorMessage = "Kontrollera värdet för total tidsspillan")]
        [Display(Name = "Eventuell total tidsspillan (utanför förväntad start- och sluttid)", Description = "Avser tid i minuter för total tidsspillan som restid, väntetider mm som överstiger 30 minuter och som inträffat utanför förväntad start- och sluttid")]
        public int? TimeWasteTotalTime { get; set; }

        [Range(0, 600, ErrorMessage = "Kontrollera värdet för spilltid som inträffat under obekväm arbetstid")]
        [Display(Name = "Andel av total tidsspillan som inträffat under obekväm arbetstid", Description = "Avser tid i minuter av den totala tidsspillan som angetts och som inträffat utanför vardagar 07:00-18:00")]
        public int? TimeWasteIWHTime { get; set; }

        [Display(Name = "Angiven tidsspillan")]
        public string TimeWasteInfo
        {
            get => (TimeWasteTotalTime != null && TimeWasteTotalTime > 0) ? $"{TimeWasteTotalTime} min varav {TimeWasteIWHTime ?? 0} min obekväm tid" : "Ingen tidsspillan har angivits";
        }

        [Display(Name = "Tolkens kompetensnivå")]
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevel { get; set; }

        [Display(Name = "Faktisk startid")]
        public DateTimeOffset SessionStartedAt { get; set; }

        [Display(Name = "Faktisk sluttid")]
        public DateTimeOffset SessionEndedAt { get; set; }

        [Display(Name = "Startid för måltidspaus")]
        public DateTimeOffset MealBreakStartAt { get; set; }

        [Display(Name = "Sluttid för måltidspaus")]
        public DateTimeOffset MealBreakEndAt { get; set; }

        public List<MealBreak> MealBreaks { get; set; }
               
        [DataType(DataType.MultilineText)]
        [Required]
        [Display(Name = "Specifikation", Description = "Var tydlig med var alla tider och kostnader kommer ifrån.")]
        [StringLength(1000)]
        public string Message { get; set; }

        public string ViewedByUser { get; set; } = string.Empty;

        public int RequestViewId { get; set; } = 0;

        public int? ReplacingRequisitionId { get; set; }

        public PreviousRequisitionViewModel PreviousRequisition { get; set; }

        public List<FileModel> Files { get; set; }

        public Guid? FileGroupKey { get; set; }

        public long? CombinedMaxSizeAttachments { get; set; }

        public PriceInformationModel ResultPriceInformationModel { get; set; }

        public PriceInformationModel RequestPriceInformationModel { get; set; }

        public bool RequestOrReplacingOrderPricesAreUsed { get; set; }

        [Display(Name = "Inställelsesätt", Description = "Tolkning på plats och på distans i anvisad lokal kan medföra reskostnader för tolken.")]
        public InterpreterLocation? InterpreterLocation { get; set; }

        #region methods

        public static RequisitionModel GetModelFromRequest(Request request)
        {
            return new RequisitionModel
            {
                RequestId = request.RequestId,
                BrokerName = request.Ranking.Broker.Name,
                CustomerOrganizationName = request.Order.CustomerOrganisation.Name,
                CustomerReferenceNumber = request.Order.CustomerReferenceNumber,
                OrderCreatedBy = request.Order.CreatedByUser.CompleteContactInformation,
                ExpectedEndedAt = request.Order.EndAt,
                ExpectedStartedAt = request.Order.StartAt,
                SessionEndedAt = request.Order.EndAt,
                SessionStartedAt = request.Order.StartAt,
                ExpectedTravelCosts = request.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0,
                Interpreter = request.Interpreter.CompleteContactInformation,
                InterpreterCompetenceLevel = (CompetenceAndSpecialistLevel?)request.CompetenceLevel,
                LanguageName = request.Order.OtherLanguage ?? request.Order.Language?.Name ?? "-",
                OrderNumber = request.Order.OrderNumber.ToString(),
                RegionName = request.Ranking.Region.Name,
                PreviousRequisition = PreviousRequisitionViewModel.GetViewModelFromPreviousRequisition(request.Requisitions.SingleOrDefault(r => r.Status == RequisitionStatus.DeniedByCustomer && !r.ReplacedByRequisitionId.HasValue)),
                InterpreterLocation = (InterpreterLocation)request.InterpreterLocation
            };
        }

        #endregion
    }
}
