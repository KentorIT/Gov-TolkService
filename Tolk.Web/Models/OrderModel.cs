using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class OrderModel
    {
        /// <summary>
        /// This is the id for the row in the languages select box that should show the other language box.
        /// </summary>
        public static int OtherLanguageId { get; } = 62;

        public int? OrderId { get; set; }

        [Display(Name = "Region", Description = "Region där tolkningen ska utföras")]
        [Required]
        public int RegionId { get; set; }

        [Display(Name = "Språk")]
        [ClientRequired]
        public int? LanguageId { get; set; }

        [Display(Name = "Annan kontaktperson")]
        public int? ContactPersonId { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Beskrivning", Description = "Extra information om uppdraget i det fall det behövs")]
        public string Description { get; set; }

        [Display(Name = "Enhet/avdelning")]
        [ClientRequired]
        public string UnitName { get; set; }

        [Display(Name = "Adress")]
        [ClientRequired]
        public string LocationStreet { get; set; }

        [Display(Name = "Postnummer")]
        [ClientRequired]
        [RegularExpression("[0-9]{3} ?[0-9]{2}", ErrorMessage = "Ange postnummer enligt format 12345 eller 123 45")]
        public string LocationZipCode { get; set; }

        [Display(Name = "Ort")]
        [ClientRequired]
        public string LocationCity { get; set; }

        [Display(Name = "Startdatum och tid", Description = "Datum och tid när tolkuppdraget startar.")]
        public DateTimeOffset StartAt { get; set; }

        [Display(Name = "Slutdatum och tid")]
        public DateTimeOffset EndAt { get; set; }

        [Display(Name = "Typ av tolkuppdrag")]
        [Required]
        public AssignmentType AssignmentType { get; set; }

        [Display(Name = "Typ av distanstolkning")]
        [ClientRequired]
        public OffSiteAssignmentType? OffSiteAssignmentType { get; set; }

        [Display(Name = "Kontaktinformation för distanstolkning")]
        [ClientRequired]
        [MaxLength(255)]
        public string OffSiteContactInformation { get; set; }

        [Display(Name = "Övrigt (annat) språk", Description = "Lägg till språk här. Lägg inte till dialekter här, det görs i extra behov.")]
        [ClientRequired]
        [MaxLength(255)]
        public string OtherLanguage { get; set; }

        [Display(Name = "Erbjud flera alternativ till inställelsesätt")]
        public bool UseRankedInterpreterLocation { get; set; } = false;

        [Display(Name = "Inställelsesätt")]
        public InterpreterLocation? InterpreterLocationSelector { get; set; }

        [Display(Name = "Inställelsesätt")]
        public InterpreterLocation? InterpreterLocation { get; set; }

        [Display(Name = "Önskat inställelsesätt (den som är helst överst)")]
        public List<InterpreterLocationModel> InterpreterLocations { get; set; }

        [Display(Name = "Ert referensnummer", Description = "Extra fält för att koppla till ett ärendenummer i er verksamhet")]
        public string CustomerReferenceNumber { get; set; }

        [Display(Name = "Beställd kompetensnivå")]
        [Required]
        public CompetenceAndSpecialistLevel RequiredCompetenceLevel { get; set; }

        [Display(Name = "Accepterar restid över 2 tim landvägen eller avstånd över 100 km")]
        public bool AllowMoreThanTwoHoursTravelTime { get; set; }

        public PriceInformation PriceInformation { get; set; }

        #region details

        [Display(Name = "Status")]
        public OrderStatus Status { get; set; }

        [Display(Name = "AvropsID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Region")]
        public string RegionName { get; set; }

        [Display(Name = "Språk")]
        public string LanguageName { get; set; }

        [Display(Name = "Skapad")]
        public DateTimeOffset CreatedAt { get; set; }

        [Display(Name = "Skapad av")]
        public string CreatedBy { get; set; }

        [Display(Name = "Annan kontaktperson")]
        public string ContactPerson { get; set; }

        [Display(Name = "Kund")]
        public string CustomerName { get; set; }

        [Display(Name = "Förmedling")]
        public string BrokerName { get; set; }

        [Display(Name = "Beräknat pris inklusive förmedlingsavgift och ev. OB (exkl. moms)")]
        [DataType(DataType.Currency)]
        public decimal CalculatedPrice { get => PriceInformation?.TotalPrice ?? 0; }

        [Display(Name = "Angiven förväntad resekostnad (exkl. moms)")]
        [DataType(DataType.Currency)]
        public decimal ExpectedTravelCosts { get; set; }

        [Display(Name = "Beräknat pris enligt avropssvar inklusive förmedlingsavgift och ev. OB (exkl. moms)")]
        [DataType(DataType.Currency)]
        public decimal? CalculatedPriceActiveRequest { get; set; }

        [Display(Name = "Tillsatt tolk")]
        public string InterpreterName { get; set; }

        [Display(Name = "Tolkens kompetensnivå")]
        public CompetenceAndSpecialistLevel? CompetenceLevel { get; set; }

        [Display(Name = "Inställelsesätt enl. svar")]
        public InterpreterLocation InterpreterLocationAnswer { get; set; }

        [Display(Name = "Status på aktiv förfrågan")]
        public RequestStatus? RequestStatus { get; set; }
        public int? RequestId { get; set; }

        public List<BrokerListModel> PreviousRequests { get; set; }

        [Display(Name = "Anledning till att svaret inte godtas")]
        [DataType(DataType.MultilineText)]
        [ClientRequired]
        public string DenyMessage { get; set; }

        [Display(Name = "Anledning till att avropet avbokas")]
        [DataType(DataType.MultilineText)]
        [ClientRequired]
        public string CancelMessage { get; set; }

        #endregion

        #region extra requirements

        [Display(Name = "Extra behov")]
        public List<OrderRequirementModel> OrderRequirements { get; set; }

        #endregion

        public bool AllowDenial => ((AllowMoreThanTwoHoursTravelTime && ExpectedTravelCosts > 0) || (OrderRequirements?.Any(r => r.RequirementIsRequired) ?? false));

        public bool UseAddress => UseRankedInterpreterLocation || AssignmentType != AssignmentType.OffSite;
        public bool UseOffSiteInformation => UseRankedInterpreterLocation || AssignmentType == AssignmentType.OffSite;

        public bool AllowOrderCancellation { get; set; } = false;

        #region methods

        public void UpdateOrder(Order order)
        {
            order.LanguageId = AssignmentType != AssignmentType.Education ? LanguageId : null;
            order.OtherLanguage = OtherLanguageId == LanguageId ? OtherLanguage : null;
            order.RegionId = RegionId;
            order.ContactPersonId = ContactPersonId;
            order.AssignentType = AssignmentType;
            order.CustomerReferenceNumber = CustomerReferenceNumber;
            order.StartAt = StartAt;
            order.EndAt = EndAt;
            order.Description = Description;
            order.UnitName = UseAddress ? UnitName : null;
            order.Street = UseAddress ? LocationStreet : null;
            order.ZipCode = UseAddress ? (!string.IsNullOrEmpty(LocationZipCode) && LocationZipCode.Length > 4) ? LocationZipCode.Replace(" ", string.Empty).Insert(3, " ") : LocationZipCode : null;
            order.City = UseAddress ? LocationCity : null;
            order.AllowMoreThanTwoHoursTravelTime = UseAddress ? AllowMoreThanTwoHoursTravelTime : false;
            order.OffSiteAssignmentType = UseOffSiteInformation ? OffSiteAssignmentType : null;
            order.OffSiteContactInformation = UseOffSiteInformation ? OffSiteContactInformation : null;
            order.RequiredCompetenceLevel = RequiredCompetenceLevel;
            if (UseRankedInterpreterLocation)
            {
                //Add one(3) rows to OrderInterpreterLocation
                foreach (var location in InterpreterLocations.OrderBy(l => l.Rank))
                {
                    order.InterpreterLocations.Add(new OrderInterpreterLocation { InterpreterLocation = location.InterpreterLocation, Rank = location.Rank });
                }
            }
            else
            {
                //Add one(1) row to OrderInterpreterLocation
                // with rank 0
                order.InterpreterLocations.Add(new OrderInterpreterLocation { InterpreterLocation = InterpreterLocation.Value, Rank = 0 });
            }

            if (OrderRequirements != null)
            {
                // add all extra requirements
                foreach (var req in OrderRequirements)
                {
                    //TODO: Handle deletes too!
                    OrderRequirement requirement = null;
                    if (req.OrderRequirementId.HasValue)
                    {
                        requirement = order.Requirements.Single(r => r.OrderRequirementId == req.OrderRequirementId);
                    }
                    else
                    {
                        requirement = new OrderRequirement();
                        order.Requirements.Add(requirement);
                    }
                    requirement.RequirementType = req.RequirementType.Value;
                    requirement.IsRequired = req.RequirementIsRequired;
                    requirement.Description = req.RequirementDescription;
                }
            }
        }

        public static OrderModel GetModelFromOrder(Order order, int? activeRequestId = null)
        {
            bool useRankedInterpreterLocation = order.InterpreterLocations.Count() > 1;
            return new OrderModel
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber.ToString(),
                CreatedBy = order.CreatedByUser.NormalizedEmail,
                ContactPerson = order.ContactPersonUser?.NormalizedEmail,
                CreatedAt = order.CreatedAt,
                CustomerName = order.CustomerOrganisation.Name,
                LanguageName = order.OtherLanguage ?? order.Language?.Name ?? "-",
                RegionName = order.Region.Name,
                LanguageId = order.LanguageId,
                AllowMoreThanTwoHoursTravelTime = order.AllowMoreThanTwoHoursTravelTime,
                AssignmentType = order.AssignentType,
                RegionId = order.RegionId,
                CustomerReferenceNumber = order.CustomerReferenceNumber,
                StartAt = order.StartAt,
                EndAt = order.EndAt,
                Description = order.Description,
                UnitName = order.UnitName,
                LocationStreet = order.Street,
                LocationZipCode = order.ZipCode,
                LocationCity = order.City,
                OffSiteAssignmentType = order.OffSiteAssignmentType,
                OffSiteContactInformation = order.OffSiteContactInformation,
                RequiredCompetenceLevel = order.RequiredCompetenceLevel,
                Status = order.Status,
                UseRankedInterpreterLocation = useRankedInterpreterLocation,
                InterpreterLocation = !useRankedInterpreterLocation ? (InterpreterLocation?)order.InterpreterLocations.Single().InterpreterLocation : null,
                InterpreterLocations = order.InterpreterLocations.OrderBy(l => l.Rank).Select(l => new InterpreterLocationModel
                {
                    InterpreterLocation = l.InterpreterLocation,
                    Rank = l.Rank
                }).ToList(),
                OrderRequirements = order.Requirements.Select(r => new OrderRequirementModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    RequirementDescription = r.Description,
                    RequirementIsRequired = r.IsRequired,
                    RequirementType = r.RequirementType,
                    CanSatisfyRequirement = r.RequirementAnswers?.SingleOrDefault(a => a.RequestId == activeRequestId)?.CanSatisfyRequirement,
                    Answer = r.RequirementAnswers?.SingleOrDefault(a => a.RequestId == activeRequestId)?.Answer
                }).ToList(),
                PriceInformation = new PriceInformation
                {
                    PriceRows = order.PriceRows.Select(r => new PriceRow
                    {
                        StartAt = r.StartAt,
                        EndAt = r.EndAt,
                        IsBrokerFee = r.IsBrokerFee,
                        Price = r.TotalPrice,
                        Quantity = 1,
                        PriceListRowId = r.PriceListRowId,
                    }).ToList()
                },
                PreviousRequests = order.Requests.Where(r =>
                   r.Status == BusinessLogic.Enums.RequestStatus.DeclinedByBroker ||
                   r.Status == BusinessLogic.Enums.RequestStatus.DeniedByTimeLimit ||
                   r.Status == BusinessLogic.Enums.RequestStatus.DeniedByCreator
                ).Select(r => new BrokerListModel
                {
                    Status = r.Status,
                    BrokerName = r.Ranking.Broker.Name,
                    DenyMessage = r.DenyMessage
                }).ToList()
            };
        }

        #endregion
    }
}
