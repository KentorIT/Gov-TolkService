using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class OrderModel
    {
        public int? OrderId { get; set; }

        [Display(Name = "Region", Description = "Region där tolkningen ska utföras")]
        [Required]
        public int RegionId { get; set; }

        [Display(Name = "Språk")]
        [Required]
        public int LanguageId { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Beskrivning")]
        [Required]
        public string Description { get; set; }

        [Display(Name = "Enhet/avdelning")]
        [Required]
        public string UnitName { get; set; }

        [Display(Name = "Adress")]
        [Required]
        public string LocationStreet { get; set; }

        [Display(Name = "Postnummer")]
        [Required]
        public string LocationZipCode { get; set; }

        [Display(Name = "Ort")]
        [Required]
        public string LocationCity { get; set; }

        [Display(Name = "Startdatum och tid", Description = "Datum och tid när tolkuppdraget startar.")]
        public DateTimeOffset StartDateTime { get; set; }

        [Display(Name = "Slutdatum och tid")]
        public DateTimeOffset EndDateTime { get; set; }

        [Display(Name = "Typ av tolkuppdrag")]
        [Required]
        public AssignmentType AssignmentType { get; set; }

        [Display(Name = "Erbjud flera alternativ till inställelsesätt")]
        public bool UseRankedInterpreterLocation { get; set; } = false;

        [Display(Name = "Inställelsesätt")]
        public InterpreterLocation? InterpreterLocation { get; set; }

        [Display(Name = "Önskat inställelsesätt (den som är helst överst)")]
        public List<InterpreterLocationModel> InterpreterLocations { get; set; }

        [Display(Name = "Ert referensnummer", Description = "Extra fält för att koppla till ett ärendenummer i er verksamhet")]
        public string CustomerReferenceNumber { get; set; }

        [Display(Name = "Kompetensnivå")]
        [Required]
        public CompetenceAndSpecialistLevel RequiredCompetenceLevel { get; set; }

        [Display(Name = "Accepterar mer än två timmar restidskostnad")]
        public bool AllowMoreThanTwoHoursTravelTime { get; set; }

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

        [Display(Name = "Kund")]
        public string CustomerName { get; set; }

        [Display(Name = "Förmedling")]
        public string BrokerName { get; set; }

        [Display(Name = "Beräknat pris inklusive förmedlingsavgift och ev. OB")]
        [DataType(DataType.Currency)]
        public decimal CalculatedPrice { get; set; }

        [Display(Name = "Beräknat resekostnad")]
        [DataType(DataType.Currency)]
        public decimal ExpectedTravelCosts { get; set; }

        [Display(Name = "Tillsatt tolk")]
        public string InterpreterName  { get; set; }

        [Display(Name = "Status på aktiv förfrågan")]
        public RequestStatus? RequestStatus { get; set; }
        public int? RequestId { get; set; }

        public bool AllowDenial
        {
            get
            {
                return OrderRequirements?.Any(r => r.RequirementIsRequired) ?? false;
            }
        }

        [Display(Name = "Anledning till att svaret inte godtas")]
        [DataType(DataType.MultilineText)]
        public string DenyMessage { get; set; }

        #endregion

        #region extra requirements

        [Display(Name = "Extra behov")]
        public List<OrderRequirementModel> OrderRequirements { get; set; }

        #endregion

        #region methods

        public void UpdateOrder(Order order)
        {
            order.LanguageId = LanguageId;
            order.AllowMoreThanTwoHoursTravelTime = AllowMoreThanTwoHoursTravelTime;
            order.AssignentType = AssignmentType;
            order.RegionId = RegionId;
            order.CustomerReferenceNumber = CustomerReferenceNumber;
            order.StartDateTime = StartDateTime;
            order.EndDateTime = EndDateTime;
            order.Description = Description;
            order.UnitName = UnitName;
            order.Street = LocationStreet;
            order.ZipCode = LocationZipCode;
            order.City = LocationCity;
            order.RequiredCompetenceLevel = RequiredCompetenceLevel;

            if (UseRankedInterpreterLocation)
            {
                //Add one(3) rows to OrderInterpreterLocation
                foreach (var location in InterpreterLocations.OrderBy(l => l.Rank))
                {
                    order.InterpreterLocations.Add(new OrderInterpreterLocation { InterpreterLocation = location.InterpreterLocation, Rank = location.Rank});
                }
            }
            else
            {
                //Add one(1) row to OrderInterpreterLocation
                // with rank 0
                order.InterpreterLocations.Add(new OrderInterpreterLocation { InterpreterLocation = InterpreterLocation.Value, Rank = 0});
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
                CreatedBy = order.CreatedByUser?.NormalizedEmail,
                CreatedAt = order.CreatedAt,
                CustomerName = order.CustomerOrganisation?.Name,
                LanguageName = order.Language?.Name,
                RegionName = order.Region?.Name,
                LanguageId = order.LanguageId,
                AllowMoreThanTwoHoursTravelTime = order.AllowMoreThanTwoHoursTravelTime,
                AssignmentType = order.AssignentType,
                RegionId = order.RegionId,
                CustomerReferenceNumber = order.CustomerReferenceNumber,
                StartDateTime = order.StartDateTime,
                EndDateTime = order.EndDateTime,
                Description = order.Description,
                UnitName = order.UnitName,
                LocationStreet = order.Street,
                LocationZipCode = order.ZipCode,
                LocationCity = order.City,
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
                }).ToList()
            };

        }

        #endregion
    }
}
