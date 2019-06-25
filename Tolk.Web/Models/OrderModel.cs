﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;
using Tolk.Web.Services;
using Tolk.BusinessLogic.Helpers;

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

        public string LastTimeForRequiringLatestAnswerBy { get; set; }

        public string NextLastTimeForRequiringLatestAnswerBy { get; set; }

        [Display(Name = "Län", Description = "Län för den plats där tolkbehovet finns. I det fall tolkning sker på distans anges länet där myndigheten som använder den aktuella tolktjänsten är placerad. Om tolkning ska genomföras vid en myndighets lokalkontor anges det län där lokalkontoret är placerat.")]
        [Required]
        public int? RegionId { get; set; }

        [Display(Name = "Enhet")]
        [ClientRequired]
        public int? CustomerUnitId { get; set; }

        [Display(Name = "Språk", Description = "Om önskat språk inte finns i listan, välj Övrigt språk och ange själv språk i textfältet som visas.")]
        [ClientRequired]
        public int? LanguageId { get; set; }

        public bool? LanguageHasAuthorizedInterpreter { get; set; }

        [Display(Name = "Dialekt", Description = "Om dialekt är krav måste förmedlingen tillsätta tolk som uppfyller kravet. Annars betraktas det som ett önskemål, och förmedlingen behöver inte uppfylla kravet.")]
        [RequiredIf(nameof(DialectIsRequired), true, OtherPropertyType = typeof(bool))]
        [StringLength(255)]
        public string Dialect { get; set; }

        [Display(Name = "Dialekt är ett krav")]
        public bool DialectIsRequired { get; set; }

        [Display(Name = "Rätt att granska rekvisition", Description = "Välj vid behov en annan person som skall ges rätt att granska rekvisition, t ex person som deltar vid tolktillfället. Denna uppgift kan du även komplettera eller ändra senare.")]
        public int? ContactPersonId { get; set; }

        public int? ChangeContactPersonId { get; set; }

        public AttachmentListModel RequestAttachmentListModel { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Övrig information om uppdraget", Description = "Eventuell annan information som är viktig eller relevant för förmedling eller tolk, t ex vägbeskrivning, ärendeinformation eller förutsättningar i övrigt för tolkuppdragets genomförande. Här kan du även ange kontaktuppgifter till person som tolken skall kontakta. Beakta eventuell sekretess avseende informationen.")]
        [Placeholder("T ex vägbeskrivning, ärendeinformation eller övriga förutsättningar för tolkuppdraget. Beakta eventuell sekretess avseende informationen.")]
        [StringLength(1000)]
        public string Description { get; set; }

        [Display(Name = "Språk och dialekt")]
        [DataType(DataType.MultilineText)]
        public string LanguageAndDialect => $"{LanguageName}\n{DialectDescription}";

        [Display(Name = "Myndighetens avdelning")]
        [StringLength(100)]
        public string UnitName { get; set; }

        [Display(Name = "Datum och tid", Description = "Datum och tid för tolkuppdraget")]
        [ClientRequired(ErrorMessage = "Ange datum")]
        public virtual TimeRange TimeRange { get; set; }

        [Display(Name = "Extra tolk", Description = "Om denna checkbox kryssas i så betyder det att man vill ha två tolkar till samma tillfälle. Det innebär självklart arvode och förmedlingsavgift för båda tolkarna för hela tilfället.")]
        public bool ExtraInterpreter { get; set; }

        [Display(Name = "Boka flera tillfällen", Description = "Om denna checkbox kryssas i så kan man lägga till flera tillfällen. Det är tvingande för förmedlingen att tillsätta samma tolk för alla tillfällen. Detta innebär självklart arvode och förmedlingsavgift för varje tillfälle. Fyll i ett fullständigt tillfälle för att kunna lägga till fler.")]
        public bool SeveralOccasions { get; set; }

        [Display(Name = "Datum och tid", Description = "Sluttid kan anges för nästa dag vid dygnspassering, t ex 01:00. Om start- eller sluttid kan ha viss flexibilitet, beskriv detta i fritextfältet &quotÖvrig information om uppdraget&quot nedan.")]
        [ClientRequired(ErrorMessage = "Ange datum")]
        public virtual SplitTimeRange SplitTimeRange { get; set; }

        [Display(Name = "Sista svarstid", Description = "Eftersom uppdraget sker i närtid, måste sista svarstid anges.")]
        [ClientRequired(ErrorMessage = "Ange sista svarstid")]
        public DateTimeOffset? LatestAnswerBy { get; set; }

        [Display(Name = "Uppdragstyp", Description = "Avistatolkning sker genom en kombination av tal och skrift, till exempel uppläsning av dokument.")]
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

        public List<OrderOccasionModel> Occasions { get; set; }


        [Display(Name = "Tredje hand")]
        public InterpreterLocation? RankedInterpreterLocationThird { get; set; }
        public InterpreterLocationAddressModel RankedInterpreterLocationFirstAddressModel { get; set; }
        public InterpreterLocationAddressModel RankedInterpreterLocationSecondAddressModel { get; set; }
        public InterpreterLocationAddressModel RankedInterpreterLocationThirdAddressModel { get; set; }

        [Display(Name = "Myndighetens ärendenummer", Description = "Fält för att koppla till ett ärendenummer i er verksamhet.")]
        [StringLength(100)]
        public string CustomerReferenceNumber { get; set; }

        [NoDisplayName]
        [ClientRequired]
        public RadioButtonGroup CompetenceLevelDesireType { get; set; }

        [Display(Name = "Krav på kompetensnivå tolk", Description = "OBS! Ingen prioritetsordning")]
        [RequiredChecked(Min = 1, Max = 2)]
        public CheckboxGroup RequiredCompetenceLevels { get; set; }

        [Display(Name = "Önskemål om kompetensnivå tolk")]
        [Prefix(PrefixPosition = PrefixAttribute.Position.Value, Text = "<span class=\"competence-ranking-num\">1. </span>")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelFirst { get; set; }

        [NoDisplayName]
        [Prefix(PrefixPosition = PrefixAttribute.Position.Value, Text = "<span class=\"competence-ranking-num\">2. </span>")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelSecond { get; set; }

        [ClientRequired]
        [Display(Name = "Accepterar restid eller resväg som överskrider gränsvärden", Description = "Vid tolkning med inställelsesätt På plats eller Distans i anvisad lokal har förmedlingen rätt att debitera kostnader för tolkens resor upp till ramavtalets gränsvärden på 2 timmars restid eller 100 km resväg. Resekostnader som överskrider gränsvärdena måste godkännas av myndighet i förväg. Genom att du markerat denna ruta måste förmedlingen ange bedömd resekostnad för tillsatt tolk i sin bekräftelse. Du får ett e-postmeddelande när bekräftelsen kommit. Om du underkänner bedömd resekostnad går förfrågan vidare till nästa förmedling enligt rangordningen.")]
        public RadioButtonGroup AllowExceedingTravelCost { get; set; }

        public bool DisplayForBroker { get; set; } = false;

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
        public bool SpecificCompetenceLevelRequired { get => (RequiredCompetenceLevels == null || CompetenceLevelDesireType == null) ? false : EnumHelper.Parse<DesireType>(CompetenceLevelDesireType.SelectedItem.Value) == DesireType.Requirement; }

        public bool DisplayExpectedTravelcost { get => (AllowExceedingTravelCost == null || AllowExceedingTravelCost.SelectedItem == null) ? false : DisplayForBroker ? EnumHelper.Parse<TrueFalse>(AllowExceedingTravelCost.SelectedItem.Value) == TrueFalse.Yes : (EnumHelper.Parse<AllowExceedingTravelCost>(AllowExceedingTravelCost.SelectedItem.Value) == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldBeApproved || EnumHelper.Parse<AllowExceedingTravelCost>(AllowExceedingTravelCost.SelectedItem.Value) == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldNotBeApproved); }

        public string WarningOrderTimeInfo { get; set; } = string.Empty;

        public PriceInformation PriceInformation { get; set; }

        public AttachmentListModel AttachmentListModel { get; set; }

        public List<FileModel> Files { get; set; }

        public Guid? FileGroupKey { get; set; }

        public long? CombinedMaxSizeAttachments { get; set; }


        #region details

        [Display(Name = "Status på bokningen")]
        public OrderStatus Status { get; set; }

        public string ColorClassName { get => CssClassHelper.GetColorClassNameForOrderStatus(Status); }

        [Display(Name = "BokningsID")]
        public string OrderNumber { get; set; }

        public int? ReplacedByOrderId { get; set; }

        [Display(Name = "Ersatt av BokningsID")]
        public string ReplacedByOrderNumber { get; set; }

        [Display(Name = "Ersätter BokningsID")]
        public string ReplacingOrderNumber { get; set; }

        [Display(Name = "Län")]
        public string RegionName { get; set; }

        [Display(Name = "Språk")]
        public string LanguageName { get; set; }

        [Display(Name = "Myndighetens enhet")]
        public string CustomerUnitName { get; set; }

        [Display(Name = "Bokning skapad")]
        public DateTimeOffset CreatedAt { get; set; }

        [Display(Name = "Bokning skapad av")]
        [DataType(DataType.MultilineText)]
        public string CreatedBy { get; set; }

        public int CreatedById { get; set; }

        [Display(Name = "Bokning besvarad av")]
        [DataType(DataType.MultilineText)]
        public string AnsweredBy { get; set; }

        [Display(Name = "Person med rätt att granska rekvisition", Description = "Person som har rätt att granska rekvisition")]
        [DataType(DataType.MultilineText)]
        public string ContactPerson { get; set; }

        [Display(Name = "Myndighet")]
        public string CustomerName { get; set; }

        [Display(Name = "Förmedling")]
        public string BrokerName { get; set; }

        [Display(Name = "Förmedlingens organisationsnummer")]
        public string BrokerOrganizationNumber { get; set; }

        public PriceInformationModel OrderCalculatedPriceInformationModel { get; set; }

        [Display(Name = "Angiven bedömd resekostnad (exkl. moms)")]
        [DataType(DataType.Currency)]
        public decimal ExpectedTravelCosts { get; set; }

        [Display(Name = "Kommentar till bedömd resekostnad")]
        [DataType(DataType.MultilineText)]
        public string ExpectedTravelCostInfo { get; set; }

        public PriceInformationModel ActiveRequestPriceInformationModel { get; set; }

        [Display(Name = "Tillsatt tolk")]
        [DataType(DataType.MultilineText)]
        public string InterpreterName { get; set; }

        [Display(Name = "Tolkens kompetensnivå", Description = "Kompetensnivå kan anges som krav eller önskemål. Maximalt två alternativ kan anges. Om kompetensnivå anges som krav ska förmedlingen tillsätta tolk med någon av angivna alternativ. Om kompetensnivå anges som önskemål kan förmedlingen tillsätta tolk enligt något av alternativen. Om inget krav eller önskemål om kompetensnivå har angetts, eller om förmedlingen inte kan tillgodose angivna önskemål, måste förmedlingen tillsätta tolk med högsta möjliga kompetensnivå enligt principen om kompetensprioritering.")]
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
        [StringLength(1000)]
        [Placeholder("Beskriv anledning till varför du inte godtar svaret.")]
        public string DenyMessage { get; set; }

        [Display(Name = "Anledning till att bokningen avbokas")]
        [DataType(DataType.MultilineText)]
        [ClientRequired]
        [StringLength(1000)]
        [Placeholder("Beskriv anledning till avbokning.")]
        public string CancelMessage { get; set; }


        #endregion

        #region extra requirements

        [Display(Name = "Tillkommande krav och/eller önskemål", Description = "Klicka på +-ikonen för att lägga till andra krav såsom tolkens kön, specifik tolk eller andra krav. Förmedlingen behöver inte uppfylla önskemål.")]
        public List<OrderRequirementModel> OrderRequirements { get; set; }


        [Display(Name = "Tillkommande önskemål", Description = "Klicka på +-ikonen för att lägga till andra önskemål såsom tolkens kön, specifik tolk eller andra önskemål. Önskemål är inte tvingande för förmedlingen")]
        public List<OrderRequirementModel> OrderDesiredRequirements { get; set; }

        #endregion

        public bool AllowDenial => AllowExceedingTravelCost != null && EnumHelper.Parse<AllowExceedingTravelCost>(AllowExceedingTravelCost.SelectedItem.Value) == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldBeApproved;

        public bool AllowEditContactPerson { get; set; } = false;

        public bool AllowOrderCancellation { get; set; } = false;

        public bool AllowReplacementOnCancel { get; set; } = false;

        [Display(Name = "Skapa ersättningsuppdrag")]
        public bool AddReplacementOrder { get; set; } = false;

        public bool AllowComplaintCreation { get; set; } = false;

        public bool AllowProcessing { get; set; } = false;

        public bool AllowNoAnswerConfirmation { get; set; } = false;

        public bool AllowUpdateExpiry { get; set; } = false;

        public bool AllowConfirmCancellation { get; set; } = false;

        public bool ActiveRequestIsAnswered { get; set; }

        public bool IsReplacement => ReplacingOrderId.HasValue;

        public bool HasOnsiteLocation => RankedInterpreterLocationFirst == InterpreterLocation.OnSite || RankedInterpreterLocationFirst == InterpreterLocation.OffSiteDesignatedLocation
        || RankedInterpreterLocationSecond == InterpreterLocation.OnSite || RankedInterpreterLocationSecond == InterpreterLocation.OffSiteDesignatedLocation
        || RankedInterpreterLocationThird == InterpreterLocation.OnSite || RankedInterpreterLocationThird == InterpreterLocation.OffSiteDesignatedLocation;

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

        public bool IsMultipleOrders
        {
            get => (!SeveralOccasions && ExtraInterpreter) || (SeveralOccasions && (Occasions.Count() > 1 || Occasions.Single().ExtraInterpreter));
        }

        public IEnumerable<OrderOccasionDisplayModel> UniqueOrdersFromOccasions
        {
            get
            {
                int id = 0;
                if (SeveralOccasions)
                {
                    foreach (var occasion in Occasions)
                    {
                        yield return new OrderOccasionDisplayModel(occasion) { ExtraInterpreter = false, OrderOccasionId = id++ };
                        if (occasion.ExtraInterpreter)
                        {
                            yield return new OrderOccasionDisplayModel(occasion) { ExtraInterpreterFor = id, OrderOccasionId = id++ };
                        }
                    }
                }
                else
                {
                    yield return new OrderOccasionDisplayModel
                    {
                        OccasionStartDateTime = SplitTimeRange.StartAt.Value.DateTime,
                        OccasionEndDateTime = SplitTimeRange.EndAt.Value.DateTime,
                        ExtraInterpreter = false,
                        OrderOccasionId = id++
                    };
                    if (ExtraInterpreter)
                    {
                        yield return new OrderOccasionDisplayModel
                        {
                            OccasionStartDateTime = SplitTimeRange.StartAt.Value.DateTime,
                            OccasionEndDateTime = SplitTimeRange.EndAt.Value.DateTime,
                            ExtraInterpreter = true,
                            ExtraInterpreterFor = id,
                            OrderOccasionId = id++
                        };
                    }
                }
            }
        }

        public IEnumerable<OrderOccasionDisplayModel> OrderOccasionDisplayModels { get; set; }

        public decimal TotalPrice
        {
            get => OrderOccasionDisplayModels?.Sum(o => o.PriceInformationModel.TotalPriceToDisplay) ?? 0;
        }

        #region methods

        public void UpdateOrder(Order order, DateTimeOffset startAt, DateTimeOffset endAt, bool isReplace = false)
        {
            order.CustomerReferenceNumber = CustomerReferenceNumber;
            order.StartAt = startAt;
            order.EndAt = endAt;
            order.Description = Description;
            order.UnitName = UnitName;
            order.ContactPersonId = ContactPersonId;
            order.Attachments = Files?.Select(f => new OrderAttachment { AttachmentId = f.Id }).ToList();
            if (isReplace)
            {
                // need to be able to change the locations after getting the replaced order´s information copied...
                order.InterpreterLocations.Clear();
            }
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
                order.LanguageHasAuthorizedInterpreter = LanguageHasAuthorizedInterpreter ?? false;
                order.RegionId = RegionId.Value;
                order.AssignentType = EnumHelper.Parse<AssignmentType>(AssignmentType.SelectedItem.Value);
                order.CustomerUnitId = (CustomerUnitId.HasValue && CustomerUnitId > 0) ? CustomerUnitId : null;
                if (HasOnsiteLocation && AllowExceedingTravelCost != null)
                {
                    order.AllowExceedingTravelCost = EnumHelper.Parse<AllowExceedingTravelCost>(AllowExceedingTravelCost.SelectedItem.Value);
                }
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
            //set OtherInterpreter as a requirement for languages that lacks authorized interpreters
            if (LanguageHasAuthorizedInterpreter.HasValue && !LanguageHasAuthorizedInterpreter.Value)
            {
                order.SpecificCompetenceLevelRequired = true;
                order.CompetenceRequirements.Add(new OrderCompetenceRequirement
                {
                    CompetenceLevel = CompetenceAndSpecialistLevel.OtherInterpreter
                });
            }
            else
            {
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
                        // Counting rank for cases where e.g. first option is undefined, but second is defined
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

        public static OrderModel GetModelFromOrder(Order order, int? activeRequestId = null, bool displayForBroker = false)
        {
            bool useRankedInterpreterLocation = order.InterpreterLocations.Count() > 1;

            OrderCompetenceRequirement competenceFirst = null;
            OrderCompetenceRequirement competenceSecond = null;
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
            }

            return new OrderModel
            {
                DisplayForBroker = displayForBroker,
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
                CustomerUnitName = order.CustomerUnit?.Name ?? string.Empty,
                Dialect = order.Requirements.Any(r => r.RequirementType == RequirementType.Dialect) ? order.Requirements.Single(r => r.RequirementType == RequirementType.Dialect)?.Description : string.Empty,
                RegionName = order.Region.Name,
                LanguageId = order.LanguageId,
                LanguageHasAuthorizedInterpreter = order.LanguageHasAuthorizedInterpreter,
                AllowExceedingTravelCost = displayForBroker ? new RadioButtonGroup { SelectedItem = order.AllowExceedingTravelCost == null ? null : SelectListService.BoolList.Single(e => e.Value == EnumHelper.Parent<AllowExceedingTravelCost, TrueFalse>(order.AllowExceedingTravelCost.Value).ToString()) } : new RadioButtonGroup { SelectedItem = order.AllowExceedingTravelCost == null ? null : SelectListService.AllowExceedingTravelCost.Single(e => e.Value == order.AllowExceedingTravelCost.ToString()) },
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
            }

            return new OrderModel
            {
                AllowExceedingTravelCost = new RadioButtonGroup { SelectedItem = order.AllowExceedingTravelCost == null ? null : SelectListService.AllowExceedingTravelCost.Single(e => e.Value == order.AllowExceedingTravelCost.ToString()) },
                AssignmentType = new RadioButtonGroup { SelectedItem = SelectListService.AssignmentTypes.Single(e => e.Value == order.AssignentType.ToString()) },
                RegionId = order.RegionId,
                CustomerReferenceNumber = order.CustomerReferenceNumber,
                CustomerUnitId = order.CustomerUnitId,
                TimeRange = new TimeRange
                {
                    StartDateTime = order.StartAt,
                    EndDateTime = order.EndAt
                },
                Description = order.Description,
                UnitName = order.UnitName,
                LanguageHasAuthorizedInterpreter = order.LanguageHasAuthorizedInterpreter,
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
