using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;
using Tolk.Web.Services;


namespace Tolk.Web.Models
{
    public class OrderModel
    {
        /// <summary>
        /// This is the id for the row in the languages select box that should show the other language box.
        /// </summary>
        public static int OtherLanguageId { get; } = 1000;

        public int? OrderId { get; set; }

        public int? ReplacingOrderId { get; set; }

        public long SystemTime { get; set; }

        [Display(Name = "Region", Description = "Det län där tolkningen ska äga rum")]
        [Required]
        public int? RegionId { get; set; }

        [Display(Name = "Språk")]
        [ClientRequired]
        public int? LanguageId { get; set; }

        [Display(Name = "Dialekt")]
        [RequiredIf(nameof(DialectIsRequired), true, OtherPropertyType = typeof(bool))]
        public string Dialect { get; set; }

        [Display(Name = "Dialekt är ett krav")]
        public bool DialectIsRequired { get; set; }

        [Display(Name = "Annan kontaktperson")]
        public int? ContactPersonId { get; set; }

        public int? ChangeContactPersonId { get; set; }

        public AttachmentListModel RequestAttachmentListModel { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Extra information", Description = "Exempelvis vägbeskrivning eller ärendeinformation")]
        public string Description { get; set; }

        [Display(Name = "Språk och dialekt")]
        [DataType(DataType.MultilineText)]
        public string LanguageAndDialect => $"{LanguageName}\n{DialectDescription}";

        [Display(Name = "Myndighetens enhet/avdelning")]
        [ClientRequired]
        public string UnitName { get; set; }

        [Display(Name = "Datum och tid", Description = "Datum och tid för tolkuppdraget")]
        [ClientRequired(ErrorMessage = "Ange datum")]
        public virtual TimeRange TimeRange { get; set; }

        [Display(Name = "Datum och tid", Description = "Datum och tid för tolkuppdraget")]
        [ClientRequired(ErrorMessage = "Ange datum")]
        public virtual SplitTimeRange SplitTimeRange { get; set; }

        [Display(Name = "Sista svarstid", Description = "Eftersom uppdraget sker imorgon, måste senaste svarstid anges")]
        [ClientRequired]
        public DateTimeOffset? LatestAnswerBy { get; set; }

        [Display(Name = "Uppdragstyp")]
        [Required]
        public RadioButtonGroup AssignmentType { get; set; }

        [Display(Name = "Övrigt (annat) språk", Description = "Ange annat språk. Dialekt läggs till i fältet bredvid.")]
        [ClientRequired]
        [StringLength(255)]
        public string OtherLanguage { get; set; }

        [Display(Name = "Första hand")]
        [Required]
        public InterpreterLocation? RankedInterpreterLocationFirst { get; set; }

        [Display(Name = "Andra hand")]
        public InterpreterLocation? RankedInterpreterLocationSecond { get; set; }

        [Display(Name = "Tredje hand")]
        public InterpreterLocation? RankedInterpreterLocationThird { get; set; }
        public InterpreterLocationAddressModel RankedInterpreterLocationFirstAddressModel { get; set; }
        public InterpreterLocationAddressModel RankedInterpreterLocationSecondAddressModel { get; set; }
        public InterpreterLocationAddressModel RankedInterpreterLocationThirdAddressModel { get; set; }

        [Display(Name = "Myndighetens ärendenummer", Description = "Fält för att koppla till ett ärendenummer i er verksamhet")]
        public string CustomerReferenceNumber { get; set; }

        [Display(Name = "Avropande myndighet")]
        [DataType(DataType.MultilineText)]
        public string CustomerCompactInfo
        { get => CustomerName + "\nEnhet/avdelning: " + UnitName + (string.IsNullOrWhiteSpace(CustomerReferenceNumber) ? string.Empty : "\nReferensnummer: " + CustomerReferenceNumber); }

        [NoDisplayName]
        public RadioButtonGroup CompetenceLevelDesireType { get; set; }

        [Display(Name = "Krav på kompetensnivå tolk", Description = "OBS! Ingen prioritetsordning")]
        [RequiredChecked(Min = 1, Max = 2)]
        public CheckboxGroup RequiredCompetenceLevels { get; set; }

        [Display(Name = "Önskemål om kompetensnivå tolk")]
        [Prefix(PrefixPosition = PrefixAttribute.Position.Value, Text = "<span class=\"competence-ranking-num\">1.</span>")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelFirst { get; set; }

        [NoDisplayName]
        [Prefix(PrefixPosition = PrefixAttribute.Position.Value, Text = "<span class=\"competence-ranking-num\">2.</span>")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelSecond { get; set; }

        [NoDisplayName]
        [Prefix(PrefixPosition = PrefixAttribute.Position.Value, Text = "<span class=\"competence-ranking-num\">3.</span>")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelThird { get; set; }

        [Display(Name = "Accepterar restid eller resväg som överskrider gränsvärden")]
        public bool AllowMoreThanTwoHoursTravelTime { get; set; }

        public bool IsOnSiteOrOffSiteDesignatedLocationSelected
        {
            get
            {
                return (RankedInterpreterLocationFirst == InterpreterLocation.OnSite
                    || RankedInterpreterLocationSecond == InterpreterLocation.OnSite
                    || RankedInterpreterLocationThird == InterpreterLocation.OnSite
                    || RankedInterpreterLocationFirst == InterpreterLocation.OffSiteDesignatedLocation
                    || RankedInterpreterLocationSecond == InterpreterLocation.OffSiteDesignatedLocation
                    || RankedInterpreterLocationThird == InterpreterLocation.OffSiteDesignatedLocation);
            }
        }

        [Display(Name = "Kompetensnivå är ett krav")]
        public bool SpecificCompetenceLevelRequired
        {
            get
            {
                return EnumHelper.Parse<DesireType>(CompetenceLevelDesireType.SelectedItem.Value) == DesireType.Requirement;
            }
        }

        public PriceInformation PriceInformation { get; set; }

        public AttachmentListModel AttachmentListModel { get; set; }

        public List<FileModel> Files { get; set; }

        public Guid? FileGroupKey { get; set; }

        public long? CombinedMaxSizeAttachments { get; set; }


        #region details

        [Display(Name = "Status på bokningen")]
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

        [Display(Name = "Bokning skapad")]
        public DateTimeOffset CreatedAt { get; set; }

        [Display(Name = "Bokning skapad av")]
        [DataType(DataType.MultilineText)]
        public string CreatedBy { get; set; }

        public int CreatedById { get; set; }

        [Display(Name = "Bokning besvarad av")]
        [DataType(DataType.MultilineText)]
        public string AnsweredBy { get; set; }

        [Display(Name = "Annan kontaktperson", Description = "Person som har rätt att handlägga rekvisitionen")]
        [DataType(DataType.MultilineText)]
        public string ContactPerson { get; set; }

        [Display(Name = "Myndighet")]
        public string CustomerName { get; set; }

        [Display(Name = "Förmedling")]
        public string BrokerName { get; set; }

        [Display(Name = "Förmedlingens organisationsnummer")]
        public string BrokerOrganizationNumber { get; set; }

        public PriceInformationModel OrderCalculatedPriceInformationModel { get; set; }

        [Display(Name = "Angiven förväntad resekostnad (exkl. moms)")]
        [DataType(DataType.Currency)]
        public decimal ExpectedTravelCosts { get; set; }

        public PriceInformationModel ActiveRequestPriceInformationModel { get; set; }

        [Display(Name = "Tillsatt tolk")]
        [DataType(DataType.MultilineText)]
        public string InterpreterName { get; set; }

        [Display(Name = "Tolkens kompetensnivå")]
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevel { get; set; }

        [Display(Name = "Inställelsesätt enl. svar")]
        public InterpreterLocation InterpreterLocationAnswer { get; set; }

        [Display(Name = "Status på aktiv förfrågan")]
        public RequestStatus? RequestStatus { get; set; }
        public int? RequestId { get; set; }
        public RequestModel ActiveRequest { get; set; }

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

        [Display(Name = "Tillkommande krav och/eller önskemål")]
        public List<OrderRequirementModel> OrderRequirements { get; set; }


        [Display(Name = "Tillkommande önskemål")]
        public List<OrderRequirementModel> OrderDesiredRequirements { get; set; }

        #endregion

        public bool AllowDenial => (AllowMoreThanTwoHoursTravelTime && ExpectedTravelCosts > 0) || (OrderRequirements?.Any(r => r.RequirementIsRequired) ?? false);

        public bool AllowEditContactPerson => (Status != OrderStatus.CancelledByBrokerConfirmed && Status != OrderStatus.CancelledByCreatorConfirmed && Status != OrderStatus.CancelledByCreator && Status != OrderStatus.NoBrokerAcceptedOrder && Status != OrderStatus.ResponseNotAnsweredByCreator);

        public bool AllowOrderCancellation { get; set; } = false;
        public bool AllowReplacementOnCancel { get; set; } = false;

        [Display(Name = "Skapa ersättningsuppdrag")]
        public bool AddReplacementOrder { get; set; } = false;

        public bool AllowComplaintCreation { get; set; } = false;

        public int? ComplaintId { get; set; }

        public bool ActiveRequestIsAnswered { get; set; }

        [Display(Name = "Reklamationens status")]
        public ComplaintStatus? ComplaintStatus { get; set; }

        [Display(Name = "Typ av reklamation")]
        public ComplaintType? ComplaintType { get; set; }
        [Display(Name = "Reklamationens beskriving")]
        public string ComplaintMessage { get; set; }

        public bool IsReplacement => ReplacingOrderId.HasValue;

        public EventLogModel EventLog { get; set; }

        private string DialectDescription
        {
            get
            {
                if (OrderRequirements != null && OrderRequirements.Any(or => or.RequirementType == RequirementType.Dialect))
                {
                    StringBuilder sb = new StringBuilder();
                    List<OrderRequirementModel> reqs;
                    reqs = OrderRequirements.Where(or => or.RequirementType == RequirementType.Dialect).ToList();
                    foreach (OrderRequirementModel orm in reqs)
                    {
                        sb.Append(orm.RequirementIsRequired ? $"Krav på dialekt: {orm.RequirementDescription}" : $"Önskemål om dialekt: {orm.RequirementDescription}");
                    }
                    return sb.ToString();
                }
                return string.Empty;
            }
        }

        public List<CompetenceAndSpecialistLevel> RequestedCompetenceLevels
        {
            get
            {
                List<CompetenceAndSpecialistLevel> list = new List<CompetenceAndSpecialistLevel>();
                if (SpecificCompetenceLevelRequired)
                {
                    return RequiredCompetenceLevels.SelectedItems
                        .Select(item => EnumHelper.Parse<CompetenceAndSpecialistLevel>(item.Value))
                        .ToList();
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

        public IEnumerable<InterpreterLocation> RankedInterpreterLocations
        {
            get
            {
                if (RankedInterpreterLocationFirstAddressModel?.InterpreterLocation != null)
                {
                    yield return RankedInterpreterLocationFirstAddressModel.InterpreterLocation.Value;
                }
                if (RankedInterpreterLocationSecondAddressModel?.InterpreterLocation != null)
                {
                    yield return RankedInterpreterLocationSecondAddressModel.InterpreterLocation.Value;
                }
                if (RankedInterpreterLocationThirdAddressModel?.InterpreterLocation != null)
                {
                    yield return RankedInterpreterLocationThirdAddressModel.InterpreterLocation.Value;
                }
            }
        }

        #region methods

        public void UpdateOrder(Order order, bool isReplace = false)
        {
            order.CustomerReferenceNumber = CustomerReferenceNumber;
            order.StartAt = SplitTimeRange?.StartAt ?? TimeRange.StartDateTime;
            order.EndAt = SplitTimeRange?.EndAt ?? TimeRange.EndDateTime;
            order.Description = Description;
            order.UnitName = UnitName;
            order.ContactPersonId = ContactPersonId;
            order.Attachments = Files?.Select(f => new OrderAttachment { AttachmentId = f.Id }).ToList();
            var location = RankedInterpreterLocationFirst.Value;
            order.InterpreterLocations.Add(GetInterpreterLocation(location, 1, RankedInterpreterLocationFirstAddressModel));
            if (RankedInterpreterLocationSecond.HasValue)
            {
                order.InterpreterLocations.Add(GetInterpreterLocation(RankedInterpreterLocationSecond.Value, 2, RankedInterpreterLocationSecondAddressModel));
                if (RankedInterpreterLocationThird.HasValue)
                {
                    order.InterpreterLocations.Add(GetInterpreterLocation(RankedInterpreterLocationThird.Value, 3, RankedInterpreterLocationThirdAddressModel));
                }
            }
            if (isReplace)
            {
                order.ReplacingOrderId = ReplacingOrderId;
            }
            else
            {
                order.LanguageId = LanguageId;
                order.OtherLanguage = OtherLanguageId == LanguageId ? OtherLanguage : null;
                order.RegionId = RegionId.Value;
                order.AssignentType = EnumHelper.Parse<AssignmentType>(AssignmentType.SelectedItem.Value);
                order.AllowMoreThanTwoHoursTravelTime = AllowMoreThanTwoHoursTravelTime;
                order.SpecificCompetenceLevelRequired = SpecificCompetenceLevelRequired;
                if (Dialect != null)
                {
                    order.Requirements.Add(new OrderRequirement
                    {
                        RequirementType = RequirementType.Dialect,
                        IsRequired = DialectIsRequired,
                        Description = Dialect
                    });
                }
                if (OrderRequirements != null)
                {
                    // add all extra requirements
                    foreach (var req in OrderRequirements)
                    {
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
                if (OrderDesiredRequirements != null)
                {
                    // add all extra desired requirements
                    foreach (var req in OrderDesiredRequirements)
                    {
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
                    foreach (var entry in RequiredCompetenceLevels.SelectedItems)
                    {
                        order.CompetenceRequirements.Add(new OrderCompetenceRequirement
                        {
                            CompetenceLevel = EnumHelper.Parse<CompetenceAndSpecialistLevel>(entry.Value)
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

        private static InterpreterLocationAddressModel GetInterpreterLocation(OrderInterpreterLocation location)
        {
            if (location == null)
            {
                return null;
            }
            return new InterpreterLocationAddressModel
            {
                InterpreterLocation = location.InterpreterLocation,
                Rank = location.Rank,
                LocationStreet = location.Street,
                LocationCity = location.City,
                OffSiteContactInformation = location.OffSiteContactInformation
            };
        }

        public static OrderModel GetModelFromOrder(Order order, int? activeRequestId = null)
        {
            bool useRankedInterpreterLocation = order.InterpreterLocations.Count() > 1;

            OrderCompetenceRequirement competenceFirst = null;
            OrderCompetenceRequirement competenceSecond = null;
            OrderCompetenceRequirement competenceThird = null;
            HashSet<CompetenceAndSpecialistLevel> requiredCompetenceLevels = null;
            var competenceRequirements = order.CompetenceRequirements.Select(r => new OrderCompetenceRequirement
            {
                CompetenceLevel = r.CompetenceLevel,
                Rank = r.Rank,
            }).ToList();

            if (order.SpecificCompetenceLevelRequired)
            {
                requiredCompetenceLevels = order.CompetenceRequirements
                    .Select(i => i.CompetenceLevel)
                    .ToHashSet();
            }
            else
            {
                competenceRequirements = competenceRequirements.OrderBy(r => r.Rank).ToList();
                competenceFirst = competenceRequirements.Count > 0 ? competenceRequirements[0] : null;
                competenceSecond = competenceRequirements.Count > 1 ? competenceRequirements[1] : null;
                competenceThird = competenceRequirements.Count > 2 ? competenceRequirements[2] : null;
            }

            return new OrderModel
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber.ToString(),
                ReplacingOrderNumber = order?.ReplacingOrder?.OrderNumber,
                ReplacedByOrderNumber = order?.ReplacedByOrder?.OrderNumber,
                ReplacedByOrderId = order?.ReplacedByOrder?.OrderId,
                ReplacingOrderId = order.ReplacingOrderId,
                CreatedBy = order.CreatedByUser.CompleteContactInformation,
                CreatedById = order.CreatedBy,
                ContactPerson = order.ContactPersonUser?.CompleteContactInformation,
                ChangeContactPersonId = order.ContactPersonId,
                CreatedAt = order.CreatedAt,
                CustomerName = order.CustomerOrganisation.Name,
                LanguageName = order.OtherLanguage ?? order.Language?.Name ?? "-",
                Dialect = order.Requirements.Any(r => r.RequirementType == RequirementType.Dialect) ? order.Requirements.Single(r => r.RequirementType == RequirementType.Dialect)?.Description : string.Empty,
                RegionName = order.Region.Name,
                LanguageId = order.LanguageId,
                AllowMoreThanTwoHoursTravelTime = order.AllowMoreThanTwoHoursTravelTime,
                AssignmentType = new RadioButtonGroup { SelectedItem = SelectListService.AssignmentTypes.Single(e => e.Value == order.AssignentType.ToString()) },
                RegionId = order.RegionId,
                CustomerReferenceNumber = order.CustomerReferenceNumber,
                TimeRange = new TimeRange
                {
                    StartDateTime = order.StartAt,
                    EndDateTime = order.EndAt
                },
                Description = order.Description,
                UnitName = order.UnitName,
                CompetenceLevelDesireType = new RadioButtonGroup
                {
                    SelectedItem = order.SpecificCompetenceLevelRequired
                    ? SelectListService.DesireTypes.Single(item => EnumHelper.Parse<DesireType>(item.Value) == DesireType.Requirement)
                    : SelectListService.DesireTypes.Single(item => EnumHelper.Parse<DesireType>(item.Value) == DesireType.Request)
                },
                RequiredCompetenceLevels = new CheckboxGroup
                {
                    SelectedItems = SelectListService.CompetenceLevels
                        .Where(item => order.CompetenceRequirements
                            .Select(r => r.CompetenceLevel)
                            .ToHashSet()
                            .Contains(EnumHelper.Parse<CompetenceAndSpecialistLevel>(item.Value))
                        ).ToHashSet()
                },
                RequestedCompetenceLevelFirst = order.SpecificCompetenceLevelRequired ? null : competenceFirst?.CompetenceLevel,
                RequestedCompetenceLevelSecond = order.SpecificCompetenceLevelRequired ? null : competenceSecond?.CompetenceLevel,
                RequestedCompetenceLevelThird = order.SpecificCompetenceLevelRequired ? null : competenceThird?.CompetenceLevel,
                Status = order.Status,
                RankedInterpreterLocationFirst = order.InterpreterLocations.Single(l => l.Rank == 1)?.InterpreterLocation,
                RankedInterpreterLocationSecond = order.InterpreterLocations.SingleOrDefault(l => l.Rank == 2)?.InterpreterLocation,
                RankedInterpreterLocationThird = order.InterpreterLocations.SingleOrDefault(l => l.Rank == 3)?.InterpreterLocation,
                RankedInterpreterLocationFirstAddressModel = GetInterpreterLocation(order.InterpreterLocations.Single(l => l.Rank == 1)),
                RankedInterpreterLocationSecondAddressModel = GetInterpreterLocation(order.InterpreterLocations.SingleOrDefault(l => l.Rank == 2)),
                RankedInterpreterLocationThirdAddressModel = GetInterpreterLocation(order.InterpreterLocations.SingleOrDefault(l => l.Rank == 3)),
                // Add the InterpreterLocation
                OrderRequirements = order.Requirements.Select(r => new OrderRequirementModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    RequirementDescription = r.Description,
                    RequirementIsRequired = r.IsRequired,
                    RequirementType = r.RequirementType,
                    CanSatisfyRequirement = r.RequirementAnswers?.SingleOrDefault(a => a.RequestId == activeRequestId)?.CanSatisfyRequirement,
                    Answer = r.RequirementAnswers?.SingleOrDefault(a => a.RequestId == activeRequestId)?.Answer
                }).ToList(),
                AttachmentListModel = new AttachmentListModel
                {
                    AllowDelete = false,
                    AllowDownload = true,
                    AllowUpload = false,
                    Title = "Bifogade filer från myndighet",
                    DisplayFiles = order.Attachments.Select(a => new FileModel
                    {
                        Id = a.Attachment.AttachmentId,
                        FileName = a.Attachment.FileName,
                        Size = a.Attachment.Blob.Length
                    }).ToList()
                },
                PriceInformation = new PriceInformation
                {
                    PriceRows = order.PriceRows.OfType<PriceRowBase>().ToList()
                },

                PreviousRequests = order.Requests.Where(r =>
                       r.Status == BusinessLogic.Enums.RequestStatus.DeclinedByBroker ||
                       r.Status == BusinessLogic.Enums.RequestStatus.DeniedByTimeLimit ||
                       r.Status == BusinessLogic.Enums.RequestStatus.DeniedByCreator
                ).Select(r => new BrokerListModel
                {
                    Status = r.Status,
                    BrokerName = r.Ranking.Broker.Name,
                    DenyMessage = r.DenyMessage,
                }).ToList(),
            };
        }

        public static OrderModel GetModelFromOrderForConfirmation(Order order)
        {
            bool useRankedInterpreterLocation = order.InterpreterLocations.Count() > 1;

            OrderCompetenceRequirement competenceFirst = null;
            OrderCompetenceRequirement competenceSecond = null;
            OrderCompetenceRequirement competenceThird = null;
            HashSet<CompetenceAndSpecialistLevel> requiredCompetenceLevels = null;
            var competenceRequirements = order.CompetenceRequirements.Select(r => new OrderCompetenceRequirement
            {
                CompetenceLevel = r.CompetenceLevel,
                Rank = r.Rank,
            }).ToList();

            if (order.SpecificCompetenceLevelRequired)
            {
                requiredCompetenceLevels = order.CompetenceRequirements
                    .Select(i => i.CompetenceLevel)
                    .ToHashSet();
            }
            else
            {
                competenceRequirements = competenceRequirements.OrderBy(r => r.Rank).ToList();
                competenceFirst = competenceRequirements.Count > 0 ? competenceRequirements[0] : null;
                competenceSecond = competenceRequirements.Count > 1 ? competenceRequirements[1] : null;
                competenceThird = competenceRequirements.Count > 2 ? competenceRequirements[2] : null;
            }

            return new OrderModel
            {
                AllowMoreThanTwoHoursTravelTime = order.AllowMoreThanTwoHoursTravelTime,
                AssignmentType = new RadioButtonGroup { SelectedItem = SelectListService.AssignmentTypes.Single(e => e.Value == order.AssignentType.ToString()) },
                RegionId = order.RegionId,
                CustomerReferenceNumber = order.CustomerReferenceNumber,
                TimeRange = new TimeRange
                {
                    StartDateTime = order.StartAt,
                    EndDateTime = order.EndAt
                },
                Description = order.Description,
                UnitName = order.UnitName,
                CompetenceLevelDesireType = new RadioButtonGroup
                {
                    SelectedItem = order.SpecificCompetenceLevelRequired
                    ? SelectListService.DesireTypes.Single(item => EnumHelper.Parse<DesireType>(item.Value) == DesireType.Requirement)
                    : SelectListService.DesireTypes.Single(item => EnumHelper.Parse<DesireType>(item.Value) == DesireType.Request)
                },
                RequiredCompetenceLevels = new CheckboxGroup
                {
                    SelectedItems = SelectListService.CompetenceLevels
                        .Where(item => order.CompetenceRequirements
                            .Select(r => r.CompetenceLevel)
                            .ToHashSet()
                            .Contains(EnumHelper.Parse<CompetenceAndSpecialistLevel>(item.Value))
                        ).ToHashSet()
                },
                RequestedCompetenceLevelFirst = competenceFirst?.CompetenceLevel,
                RequestedCompetenceLevelSecond = competenceSecond?.CompetenceLevel,
                RequestedCompetenceLevelThird = competenceThird?.CompetenceLevel,
                RankedInterpreterLocationFirst = order.InterpreterLocations.Single(l => l.Rank == 1)?.InterpreterLocation,
                RankedInterpreterLocationSecond = order.InterpreterLocations.SingleOrDefault(l => l.Rank == 2)?.InterpreterLocation,
                RankedInterpreterLocationThird = order.InterpreterLocations.SingleOrDefault(l => l.Rank == 3)?.InterpreterLocation,
                RankedInterpreterLocationFirstAddressModel = GetInterpreterLocation(order.InterpreterLocations.Single(l => l.Rank == 1)),
                RankedInterpreterLocationSecondAddressModel = GetInterpreterLocation(order.InterpreterLocations.SingleOrDefault(l => l.Rank == 2)),
                RankedInterpreterLocationThirdAddressModel = GetInterpreterLocation(order.InterpreterLocations.SingleOrDefault(l => l.Rank == 3)),
                OrderRequirements = order.Requirements.Select(r => new OrderRequirementModel
                {
                    OrderRequirementId = r.OrderRequirementId,
                    RequirementDescription = r.Description,
                    RequirementIsRequired = r.IsRequired,
                    RequirementType = r.RequirementType
                }).ToList(),
            };
        }

        private OrderInterpreterLocation GetInterpreterLocation(InterpreterLocation location, int rank, InterpreterLocationAddressModel addressModel)
        {
            return new OrderInterpreterLocation
            {
                InterpreterLocation = location,
                Rank = rank,
                Street = (location == InterpreterLocation.OffSiteDesignatedLocation || location == InterpreterLocation.OnSite) ? addressModel.LocationStreet : null,
                City = (location == InterpreterLocation.OffSiteDesignatedLocation || location == InterpreterLocation.OnSite) ? addressModel.LocationCity : null,
                OffSiteContactInformation = (location == InterpreterLocation.OffSiteVideo || location == InterpreterLocation.OffSitePhone) ? addressModel.OffSiteContactInformation : null,
            };
        }

        #endregion
    }
}
