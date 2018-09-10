﻿using System;
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

        public int? ReplacingOrderId { get; set; }

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

        [Display(Name = "Datum och tid", Description = "Datum och tid för tolkuppdraget.")]
        [Required]
        public virtual TimeRange TimeRange { get; set; }

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

        [Display(Name = "Kompetensnivå är ett krav")]
        public bool SpecificCompetenceLevelRequired { get; set; }

        [Display(Name = "Kravade kompetensnivåer")]
        [ClientRequired]
        public CompetenceAndSpecialistLevel? RequiredCompetenceLevelFirst { get; set; }

        [NoDisplayName]
        public CompetenceAndSpecialistLevel? RequiredCompetenceLevelSecond { get; set; }

        [Display(Name = "Önskade kompetensnivåer")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelFirst { get; set; }

        [NoDisplayName]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelSecond { get; set; }

        [NoDisplayName]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelThird { get; set; }

        [Display(Name = "Accepterar restid över 2 tim landvägen eller avstånd över 100 km")]
        public bool AllowMoreThanTwoHoursTravelTime { get; set; }

        public PriceInformation PriceInformation { get; set; }

        #region details

        [Display(Name = "Status")]
        public OrderStatus Status { get; set; }

        [Display(Name = "AvropsID")]
        public string OrderNumber { get; set; }

        public int? ReplacedByOrderId { get; set; }

        [Display(Name = "Ersatt av AvropsID")]
        public string ReplacedByOrderNumber { get; set; }

        [Display(Name = "Ersätter AvropsID")]
        public string ReplacingOrderNumber { get; set; }

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

        public PriceInformationModel OrderCalculatedPriceInformationModel { get; set; }

        [Display(Name = "Angiven förväntad resekostnad (exkl. moms)")]
        [DataType(DataType.Currency)]
        public decimal ExpectedTravelCosts { get; set; }

        public PriceInformationModel ActiveRequestPriceInformationModel { get; set; }

        [Display(Name = "Tillsatt tolk")]
        public string InterpreterName { get; set; }

        [Display(Name = "Tolkens kompetensnivå")]
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevel { get; set; }

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

        public bool UseAddress => UseRankedInterpreterLocation || (InterpreterLocation == null ? true : InterpreterLocation.Value != BusinessLogic.Enums.InterpreterLocation.OffSite);
        public bool UseOffSiteInformation => UseRankedInterpreterLocation || (InterpreterLocation == null ? true : InterpreterLocation.Value == BusinessLogic.Enums.InterpreterLocation.OffSite);

        public bool AllowOrderCancellation { get; set; } = false;
        public bool AllowReplacementOnCancel { get; set; } = false;

        [Display(Name = "Skapa ersättningsuppdrag")]
        public bool AddReplacementOrder { get; set; } = false;

        public bool AllowComplaintCreation { get; set; } = false;

        public int? ComplaintId { get; set; }

        public bool ActiveRequestIsAnswered => (RequestStatus != null && RequestStatus != BusinessLogic.Enums.RequestStatus.Created && RequestStatus != BusinessLogic.Enums.RequestStatus.Received);

        [Display(Name = "Reklamationens status")]
        public ComplaintStatus? ComplaintStatus { get; set; }

        [Display(Name = "Typ av reklamation")]
        public ComplaintType? ComplaintType { get; set; }
        [Display(Name = "Reklamationens beskriving")]
        public string ComplaintMessage { get; set; }

        public bool IsReplacement => ReplacingOrderId.HasValue;

        public List<CompetenceAndSpecialistLevel> RequestedCompetenceLevels
        {
            get
            {
                List<CompetenceAndSpecialistLevel> list = new List<CompetenceAndSpecialistLevel>();
                if (SpecificCompetenceLevelRequired)
                {
                    if (RequiredCompetenceLevelFirst.HasValue)
                    {
                        list.Add(RequiredCompetenceLevelFirst.Value);
                    }
                    if (RequiredCompetenceLevelSecond.HasValue)
                    {
                        list.Add(RequiredCompetenceLevelSecond.Value);
                    }
                }
                else
                {
                    if (RequestedCompetenceLevelFirst.HasValue)
                    {
                        list.Add(RequestedCompetenceLevelFirst.Value);
                    }
                    if (RequestedCompetenceLevelSecond.HasValue)
                    {
                        list.Add(RequestedCompetenceLevelSecond.Value);
                    }
                    if (RequestedCompetenceLevelThird.HasValue)
                    {
                        list.Add(RequestedCompetenceLevelThird.Value);
                    }
                }

                return list;
            }
        }

        #region methods

        public void UpdateOrder(Order order, bool isReplace = false)
        {
            order.CustomerReferenceNumber = CustomerReferenceNumber;
            order.StartAt = TimeRange.StartDateTime;
            order.EndAt = TimeRange.EndDateTime;
            order.Description = Description;
            order.UnitName = UnitName;
            order.Street = UseAddress ? LocationStreet : null;
            order.ZipCode = UseAddress ? (!string.IsNullOrEmpty(LocationZipCode) && LocationZipCode.Length > 4) ? LocationZipCode.Replace(" ", string.Empty).Insert(3, " ") : LocationZipCode : null;
            order.City = UseAddress ? LocationCity : null;
            order.OffSiteContactInformation = UseOffSiteInformation ? OffSiteContactInformation : null;
            order.OffSiteAssignmentType = UseOffSiteInformation ? OffSiteAssignmentType : null;
            order.ContactPersonId = ContactPersonId;
            if (isReplace)
            {
                order.ReplacingOrderId = ReplacingOrderId;
            }
            else
            {
                order.LanguageId = AssignmentType != AssignmentType.Education ? LanguageId : null;
                order.OtherLanguage = OtherLanguageId == LanguageId ? OtherLanguage : null;
                order.RegionId = RegionId;
                order.AssignentType = AssignmentType;
                order.AllowMoreThanTwoHoursTravelTime = UseAddress ? AllowMoreThanTwoHoursTravelTime : false;
                order.SpecificCompetenceLevelRequired = SpecificCompetenceLevelRequired;
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
            // OrderCompetenceRequirements
            if (RequestedCompetenceLevels.Count > 0)
            {
                if (SpecificCompetenceLevelRequired)
                {
                    if (RequiredCompetenceLevelFirst.HasValue)
                    {
                        order.CompetenceRequirements.Add(new OrderCompetenceRequirement
                        {
                            CompetenceLevel = RequiredCompetenceLevelFirst.Value,
                        });
                    }
                    if (RequiredCompetenceLevelSecond.HasValue)
                    {
                        order.CompetenceRequirements.Add(new OrderCompetenceRequirement
                        {
                            CompetenceLevel = RequiredCompetenceLevelSecond.Value,
                        });
                    }
                }
                else
                {
                    // Counting rank for cases where e.g. first option is undefined, but second and third are defined
                    int rank = 0;
                    if (RequestedCompetenceLevelFirst.HasValue)
                    {
                        order.CompetenceRequirements.Add(new OrderCompetenceRequirement
                        {
                            CompetenceLevel = RequestedCompetenceLevelFirst.Value,
                            Rank = ++rank
                        });
                    }
                    if (RequestedCompetenceLevelSecond.HasValue)
                    {
                        order.CompetenceRequirements.Add(new OrderCompetenceRequirement
                        {
                            CompetenceLevel = RequestedCompetenceLevelSecond.Value,
                            Rank = ++rank
                        });
                    }
                    if (RequestedCompetenceLevelThird.HasValue)
                    {
                        order.CompetenceRequirements.Add(new OrderCompetenceRequirement
                        {
                            CompetenceLevel = RequestedCompetenceLevelThird.Value,
                            Rank = ++rank
                        });
                    }
                }
            }
        }

        public static OrderModel GetModelFromOrder(Order order, int? activeRequestId = null)
        {

            bool useRankedInterpreterLocation = order.InterpreterLocations.Count() > 1;
            var competenceRequirements = order.CompetenceRequirements.Select(r => new OrderCompetenceRequirement
            {
                CompetenceLevel = r.CompetenceLevel,
                Rank = r.Rank,
            }).ToList();
            if (!order.SpecificCompetenceLevelRequired)
            {
                competenceRequirements = competenceRequirements.OrderBy(r => r.Rank).ToList();
            }
            var competenceFirst = competenceRequirements.Count > 0 ? competenceRequirements[0] : null;
            var competenceSecond = competenceRequirements.Count > 1 ? competenceRequirements[1] : null;
            var competenceThird = competenceRequirements.Count > 2 ? competenceRequirements[2] : null;
            return new OrderModel
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber.ToString(),
                ReplacingOrderNumber = order?.ReplacingOrder?.OrderNumber,
                ReplacedByOrderNumber = order?.ReplacedByOrder?.OrderNumber,
                ReplacedByOrderId = order?.ReplacedByOrder?.OrderId,
                ReplacingOrderId = order.ReplacingOrderId,
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
                TimeRange = new TimeRange
                {
                    StartDateTime = order.StartAt,
                    EndDateTime = order.EndAt
                },
                Description = order.Description,
                UnitName = order.UnitName,
                LocationStreet = order.Street,
                LocationZipCode = order.ZipCode,
                LocationCity = order.City,
                OffSiteAssignmentType = order.OffSiteAssignmentType,
                OffSiteContactInformation = order.OffSiteContactInformation,
                SpecificCompetenceLevelRequired = order.SpecificCompetenceLevelRequired,
                RequiredCompetenceLevelFirst = order.SpecificCompetenceLevelRequired ? competenceFirst?.CompetenceLevel : null,
                RequiredCompetenceLevelSecond = order.SpecificCompetenceLevelRequired ? competenceSecond?.CompetenceLevel : null,
                RequestedCompetenceLevelFirst = order.SpecificCompetenceLevelRequired ? null : competenceFirst?.CompetenceLevel,
                RequestedCompetenceLevelSecond = order.SpecificCompetenceLevelRequired ? null : competenceSecond?.CompetenceLevel,
                RequestedCompetenceLevelThird = order.SpecificCompetenceLevelRequired ? null : competenceThird?.CompetenceLevel,
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
