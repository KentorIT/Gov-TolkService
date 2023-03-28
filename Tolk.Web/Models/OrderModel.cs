using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;
using Tolk.Web.Services;

namespace Tolk.Web.Models
{
    public class OrderModel : OrderBaseModel
    {
        /// <summary>
        /// This is the id for the row in the languages select box that should show the other language box.
        /// </summary>
        public static int OtherLanguageId { get; } = 1000;

        public int? OrderId { get; set; }

        public int? ReplacingOrderId { get; set; }

        public int? OrderGroupId { get; set; }

        public string OrderGroupNumber { get; set; }

        public OrderStatus? OrderGroupStatus { get; set; }

        public string GroupStatusCssClassColor => OrderGroupStatus.HasValue ? CssClassHelper.GetColorClassNameForOrderStatus(OrderGroupStatus.Value) : string.Empty;

        public string LastTimeForRequiringLatestAnswerBy { get; set; }

        public string NextLastTimeForRequiringLatestAnswerBy { get; set; }

        [Display(Name = "Län", Description = "Län för den plats där tolkbehovet finns. I det fall tolkning sker på distans anges länet där myndigheten som använder den aktuella tolktjänsten är placerad. Om tolkning ska genomföras vid en myndighets lokalkontor anges det län där lokalkontoret är placerat.")]
        [Required]
        public int? RegionId { get; set; }

        [Display(Name = "Enhet", Description = "Välj vilken enhet bokningen ska kopplas till. Om du inte vill att bokningen ska kopplas till en enhet väljer du valet Koppla inte till någon enhet längst ner i rullistan.")]
        [ClientRequired]
        public int? CustomerUnitId { get; set; }

        [Display(Name = "Språk", Description = "Om önskat språk inte finns i listan, välj Övrigt språk och ange själv språk i textfältet som visas.")]
        [ClientRequired]
        public int? LanguageId { get; set; }

        [Display(Name = "Dialekt är ett krav")]
        public bool DialectIsRequired { get; set; }

        [Display(Name = "Dialekt", Description = "Om dialekt är krav måste förmedlingen tillsätta tolk som uppfyller kravet. Annars betraktas det som ett önskemål, och förmedlingen behöver inte uppfylla kravet.")]
        [RequiredIf(nameof(DialectIsRequired), true, OtherPropertyType = typeof(bool))]
        [StringLength(255)]
        public string Dialect { get; set; }

        [Display(Name = "Rätt att granska rekvisition", Description = "Välj vid behov en annan person som skall ges rätt att granska rekvisition, t ex person som deltar vid tolktillfället. Denna uppgift kan du även komplettera eller ändra senare.")]
        public int? ContactPersonId { get; set; }

        public int? ChangeContactPersonId { get; set; }

        public AttachmentListModel RequestAttachmentListModel { get; set; }

        [Display(Name = "Datum och tid", Description = "Datum och tid för tolkuppdraget")]
        [ClientRequired(ErrorMessage = "Ange datum")]
        public virtual TimeRange TimeRange { get; set; }

        [Display(Name = "Extra tolk", Description = "Om det finns behov av två tolkar till samma tillfälle så kryssa i rutan Extra tolk. Detta innebär att arvode och förmedlingsavgift utgår för båda tolkarna för hela tillfället.")]
        public bool ExtraInterpreter { get; set; }

        [Display(Name = "Flexibel tid", Description = "I det fall ni är flexibla inom den tid vilket uppdraget kan utföras kryssar ni i denna rutan och anger inom vilket tidsspann uppdraget ska utföras, samt uppdragets längd. Observera att denna tid inte går att ändra i efterhand.")]
        public bool FlexibleOrder { get; set; }

        [Display(Name = "Uppdragets längd", Description = "Ange uppdragets längd. Observera att denna uppgift inte går att ändra i efterhand.")]
        [RequiredIf(nameof(FlexibleOrder), true, OtherPropertyType = typeof(bool), AlwaysDisplayRequiredStar = true)]
        public TimeSpan? ExpectedLength { get; set; }

        [Display(Name = "Måltidspaus beräknas ingå", Description = "Kryssa i rutan om det är ett längre uppdrag över 5 timmar och det beräknas ingå en måltidspaus.")]
        public bool MealBreakIncluded { get; set; }

        [Display(Name = "Boka flera tillfällen med samma tolk", Description = "Om rutan kryssas i så går det att lägga till flera tillfällen. Det är tvingande för förmedlingen att tillsätta samma tolk för alla tillfällen. Detta innebär att arvode och förmedlingsavgift utgår för varje tillfälle. Fyll i ett fullständigt tillfälle för att kunna lägga till fler tillfällen.")]
        public bool SeveralOccasions { get; set; }

        [Display(Name = "Datum och tid", Description = "Sluttid kan anges för nästa dag vid dygnspassering, t ex 01:00.")]
        [ClientRequired(ErrorMessage = "Ange datum")]
        public virtual SplitTimeRange SplitTimeRange { get; set; }

        [Display(Name = "Sista svarstid", Description = "Eftersom att uppdraget sker i närtid måste sista svarstid anges. Observera dock att det i vissa, och särskilt akuta fall, kan vara bättre att ringa förmedlingarna för att få svar direkt.")]
        [ClientRequired(ErrorMessage = "Ange sista svarstid")]
        public DateTimeOffset? LatestAnswerBy { get; set; }

        [Display(Name = "Uppdragstyp", Description = "Avistatolkning sker genom en kombination av tal och skrift, till exempel uppläsning av dokument.")]
        [Required]
        public RadioButtonGroup AssignmentType { get; set; }

        [Display(Name = "Övrigt (annat) språk", Description = "Ange annat språk. Dialekt läggs till i fältet bredvid.")]
        [ClientRequired]
        [StringLength(255)]
        public string OtherLanguage { get; set; }

        [Display(Name = "Är tolkanvändare samma person som bokar", Description = "Ange om du som bokar är den som ska använda tolken, dvs hålla i mötet. Om du bokar åt annan person på myndigheten så har du möjlighet att ange kontaktuppgifter till denna i fältet för ”Övrig information om uppdraget”. (Obs. avser ej namn på klienten)")]
        [ClientRequired]
        public RadioButtonGroup CreatorIsInterpreterUser { get; set; }

        public List<OrderOccasionModel> Occasions { get; set; }

        public bool DisplayForBroker { get; set; } = false;

        public string WarningOrderTimeInfo { get; set; } = string.Empty;

        public string WarningOrderRequiredCompetenceInfo { get; set; } = string.Empty;

        public string WarningOrderGroupCloseInTime { get; set; } = string.Empty;

        public PriceInformation PriceInformation { get; set; }

        public List<FileModel> Files { get; set; }

        public Guid? FileGroupKey { get; set; }

        public long? CombinedMaxSizeAttachments { get; set; }

        #region details


        public string ColorClassName { get => CssClassHelper.GetColorClassNameForOrderStatus(Status); }

        [Display(Name = "BokningsID")]
        public string OrderNumber { get; set; }

        public int? ReplacedByOrderId { get; set; }

        [Display(Name = "Ersatt av BokningsID")]
        public string ReplacedByOrderNumber { get; set; }

        [Display(Name = "Ersätter BokningsID")]
        public string ReplacingOrderNumber { get; set; }

        public int CreatedById { get; set; }

        public PriceInformationModel ActiveRequestPriceInformationModel { get; set; }


        [Display(Name = "Status på aktiv förfrågan")]
        public RequestStatus? RequestStatus { get; set; }

        public int? RequestId { get; set; }

        public RequestModel ActiveRequest { get; set; }

        public IEnumerable<BrokerListModel> PreviousRequests { get; set; }

        [Display(Name = "Anledning till att bokningen avbokas")]
        [DataType(DataType.MultilineText)]
        [ClientRequired]
        [StringLength(1000)]
        [Placeholder("Beskriv anledning till avbokning.")]
        public string CancelMessage { get; set; }

        #endregion

        public bool AllowDenial => AllowExceedingTravelCost != null && EnumHelper.Parse<AllowExceedingTravelCost>(AllowExceedingTravelCost.SelectedItem.Value) == BusinessLogic.Enums.AllowExceedingTravelCost.YesShouldBeApproved;

        public bool AllowEditContactPerson { get; set; } = false;

        public bool AllowUpdate { get; set; } = false;

        [Display(Name = "Skapa ersättningsuppdrag")]
        public bool AddReplacementOrder { get; set; } = false;

        public bool AllowComplaintCreation { get; set; } = false;

        public bool AllowRequestPrint { get; set; } = false;

        public bool AllowNoAnswerConfirmation { get; set; } = false;

        public bool AllowResponseNotAnsweredConfirmation { get; set; } = false;

        public bool AllowUpdateExpiry { get; set; } = false;

        public bool AllowConfirmCancellation { get; set; } = false;

        public string InfoMessage { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public bool ActiveRequestIsAnswered { get; set; }

        public bool IsReplacement => ReplacingOrderId.HasValue;

        public bool IsInOrderGroup => OrderGroupId.HasValue;

        public bool HasOnsiteLocation => RankedInterpreterLocationFirst == InterpreterLocation.OnSite || RankedInterpreterLocationFirst == InterpreterLocation.OffSiteDesignatedLocation
        || RankedInterpreterLocationSecond == InterpreterLocation.OnSite || RankedInterpreterLocationSecond == InterpreterLocation.OffSiteDesignatedLocation
        || RankedInterpreterLocationThird == InterpreterLocation.OnSite || RankedInterpreterLocationThird == InterpreterLocation.OffSiteDesignatedLocation;

        public EventLogModel EventLog { get; set; }

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
            get => (!SeveralOccasions && ExtraInterpreter) || (SeveralOccasions && (Occasions.Count > 1 || Occasions.Single().ExtraInterpreter));
        }

        public OrderOccasionModel FirstOccasion => UniqueOrdersFromOccasions.OrderBy(o => o.OccasionStartDateTime).FirstOrDefault();

        public IEnumerable<OrderOccasionDisplayModel> UniqueOrdersFromOccasions
        {
            get
            {
                int id = 0;
                if (SeveralOccasions)
                {
                    foreach (var occasion in Occasions)
                    {
                        yield return new OrderOccasionDisplayModel(occasion) { ExtraInterpreter = false, MealBreakIncluded = occasion.MealBreakIncluded, OrderOccasionId = id++ };
                        if (occasion.ExtraInterpreter)
                        {
                            yield return new OrderOccasionDisplayModel(occasion) { ExtraInterpreterFor = id - 1, MealBreakIncluded = occasion.MealBreakIncluded, OrderOccasionId = id++ };
                        }
                    }
                }
                else
                {
                    if (SplitTimeRange != null)
                    {
                        yield return new OrderOccasionDisplayModel
                        {
                            OccasionStartDateTime = SplitTimeRange.StartAt.Value.DateTime,
                            OccasionEndDateTime = SplitTimeRange.EndAt.Value.DateTime,
                            ExpectedLength = ExpectedLength,
                            ExtraInterpreter = false,
                            MealBreakIncluded = MealBreakIncluded,
                            OrderOccasionId = id
                        };
                        if (ExtraInterpreter)
                        {
                            yield return new OrderOccasionDisplayModel
                            {
                                OccasionStartDateTime = SplitTimeRange.StartAt.Value.DateTime,
                                OccasionEndDateTime = SplitTimeRange.EndAt.Value.DateTime,
                                ExpectedLength = ExpectedLength,
                                ExtraInterpreter = true,
                                ExtraInterpreterFor = id,
                                MealBreakIncluded = MealBreakIncluded,
                                OrderOccasionId = ++id
                            };
                        }
                    }
                }
            }
        }

        public DefaultSettingsModel UserDefaultSettings { get; set; }

        public bool EnableOrderGroups { get; set; }
        public FlexibleOrderSettings FlexibleOrderSettings { get; set; }

        #region methods

        internal void UpdateOrderGroup(OrderGroup orderGroup, bool useAttachments)
        {
            orderGroup.Attachments = useAttachments ? Files?.Select(f => new OrderGroupAttachment { AttachmentId = f.Id }).ToList() : null;
            var location = RankedInterpreterLocationFirst.Value;
            orderGroup.InterpreterLocations.Add(new OrderGroupInterpreterLocation { Rank = 1, InterpreterLocation = location });
            if (RankedInterpreterLocationSecond.HasValue)
            {
                orderGroup.InterpreterLocations.Add(new OrderGroupInterpreterLocation { Rank = 2, InterpreterLocation = RankedInterpreterLocationSecond.Value });
                if (RankedInterpreterLocationThird.HasValue)
                {
                    orderGroup.InterpreterLocations.Add(new OrderGroupInterpreterLocation { Rank = 3, InterpreterLocation = RankedInterpreterLocationThird.Value });
                }
            }
            else
            {
                RankedInterpreterLocationThird = null;
            }
            orderGroup.LanguageId = LanguageId;
            orderGroup.OtherLanguage = OtherLanguageId == LanguageId ? OtherLanguage : null;
            orderGroup.LanguageHasAuthorizedInterpreter = LanguageHasAuthorizedInterpreter ?? false;
            orderGroup.RegionId = RegionId.Value;
            orderGroup.AssignmentType = EnumHelper.Parse<AssignmentType>(AssignmentType.SelectedItem.Value);
            orderGroup.CreatorIsInterpreterUser = EnumHelper.Parse<TrueFalse>(CreatorIsInterpreterUser.SelectedItem.Value) == TrueFalse.Yes;
            orderGroup.CustomerUnitId = (CustomerUnitId.HasValue && CustomerUnitId > 0) ? CustomerUnitId : null;
            if (HasOnsiteLocation && AllowExceedingTravelCost != null)
            {
                orderGroup.AllowExceedingTravelCost = EnumHelper.Parse<AllowExceedingTravelCost>(AllowExceedingTravelCost.SelectedItem.Value);
            }
            orderGroup.SpecificCompetenceLevelRequired = SpecificCompetenceLevelRequired;
            if (Dialect != null)
            {
                var requirement = new OrderGroupRequirement
                {
                    RequirementType = RequirementType.Dialect,
                    IsRequired = DialectIsRequired,
                    Description = Dialect
                };
                orderGroup.Requirements.Add(requirement);
                foreach (Order order in orderGroup.Orders)
                {
                    order.Requirements.Add(new OrderRequirement
                    {
                        OrderGroupRequirement = requirement,
                        RequirementType = RequirementType.Dialect,
                        IsRequired = DialectIsRequired,
                        Description = Dialect
                    });
                }
            }
            if (OrderRequirements != null)
            {
                // add all extra requirements
                foreach (var req in OrderRequirements)
                {
                    OrderGroupRequirement requirement = new OrderGroupRequirement
                    {
                        RequirementType = req.RequirementType.Value,
                        IsRequired = true,
                        Description = req.RequirementDescription
                    };
                    orderGroup.Requirements.Add(requirement);
                    foreach (Order order in orderGroup.Orders)
                    {
                        order.Requirements.Add(new OrderRequirement
                        {
                            OrderGroupRequirement = requirement,
                            RequirementType = req.RequirementType.Value,
                            IsRequired = true,
                            Description = req.RequirementDescription
                        });
                    }
                }
            }
            if (OrderDesiredRequirements != null)
            {
                // add all extra desired requirements
                foreach (var req in OrderDesiredRequirements)
                {
                    OrderGroupRequirement requirement = new OrderGroupRequirement
                    {
                        RequirementType = req.DesiredRequirementType.Value,
                        IsRequired = false,
                        Description = req.DesiredRequirementDescription
                    };
                    orderGroup.Requirements.Add(requirement);
                    foreach (Order order in orderGroup.Orders)
                    {
                        order.Requirements.Add(new OrderRequirement
                        {
                            OrderGroupRequirement = requirement,
                            RequirementType = req.DesiredRequirementType.Value,
                            IsRequired = false,
                            Description = req.DesiredRequirementDescription
                        });
                    }
                }
            }
            // OrderCompetenceRequirements
            //set OtherInterpreter as a requirement for languages that lacks authorized interpreters
            if (LanguageHasAuthorizedInterpreter.HasValue && !LanguageHasAuthorizedInterpreter.Value)
            {
                orderGroup.SpecificCompetenceLevelRequired = true;
                orderGroup.CompetenceRequirements.Add(new OrderGroupCompetenceRequirement
                {
                    CompetenceLevel = CompetenceAndSpecialistLevel.OtherInterpreter
                });
            }
            else
            {
                if (RequestedCompetenceLevels.Any())
                {
                    // Counting rank for cases where e.g. first option is undefined, but second is defined
                    int rank = 0;
                    if (RequestedCompetenceLevelFirst.HasValue)
                    {
                        orderGroup.CompetenceRequirements.Add(new OrderGroupCompetenceRequirement
                        {
                            CompetenceLevel = RequestedCompetenceLevelFirst.Value,
                            Rank = ++rank
                        });
                    }
                    if (RequestedCompetenceLevelSecond.HasValue)
                    {
                        orderGroup.CompetenceRequirements.Add(new OrderGroupCompetenceRequirement
                        {
                            CompetenceLevel = RequestedCompetenceLevelSecond.Value,
                            Rank = ++rank
                        });
                    }
                }
            }
        }

        internal void UpdateOrder(Order order, OrderOccasionModel occasion, bool isReplace = false, bool isGroupOrder = false, bool useAttachments = false)
        {
            order.CustomerReferenceNumber = CustomerReferenceNumber;
            order.StartAt = occasion.OccasionStartDateTime;
            order.EndAt = occasion.OccasionEndDateTime;
            order.ExpectedLength = occasion.ExpectedLength;
            order.Description = Description;
            order.UnitName = UnitName;
            order.ContactPersonId = ContactPersonId;
            if (!isGroupOrder && useAttachments)
            {
                order.Attachments = Files?.Select(f => new OrderAttachment { AttachmentId = f.Id }).ToList();
            }
            order.InvoiceReference = InvoiceReference;

            if (isReplace)
            {
                // need to be able to change the locations after getting the replaced order´s information copied...
                order.InterpreterLocations.Clear();
            }
            var location = RankedInterpreterLocationFirst.Value;
            order.InterpreterLocations.Add(RankedInterpreterLocationFirstAddressModel.GetInterpreterLocation(location, 1));
            if (RankedInterpreterLocationSecond.HasValue)
            {
                order.InterpreterLocations.Add(RankedInterpreterLocationSecondAddressModel.GetInterpreterLocation(RankedInterpreterLocationSecond.Value, 2));
                if (RankedInterpreterLocationThird.HasValue)
                {
                    order.InterpreterLocations.Add(RankedInterpreterLocationThirdAddressModel.GetInterpreterLocation(RankedInterpreterLocationThird.Value, 3));
                }
            }
            else
            {
                RankedInterpreterLocationThird = null;
            }
            if (isReplace)
            {
                order.ReplacingOrderId = ReplacingOrderId;
            }
            else
            {
                order.MealBreakIncluded = MealBreakIncluded && ((int)(order.EndAt.DateTime - order.StartAt.DateTime).TotalMinutes > 300);
                order.LanguageId = LanguageId;
                order.OtherLanguage = OtherLanguageId == LanguageId ? OtherLanguage : null;
                order.LanguageHasAuthorizedInterpreter = LanguageHasAuthorizedInterpreter ?? false;
                order.RegionId = RegionId.Value;
                order.AssignmentType = EnumHelper.Parse<AssignmentType>(AssignmentType.SelectedItem.Value);
                order.CustomerUnitId = (CustomerUnitId.HasValue && CustomerUnitId > 0) ? CustomerUnitId : null;
                if (HasOnsiteLocation && AllowExceedingTravelCost != null)
                {
                    order.AllowExceedingTravelCost = EnumHelper.Parse<AllowExceedingTravelCost>(AllowExceedingTravelCost.SelectedItem.Value);
                }
                order.SpecificCompetenceLevelRequired = SpecificCompetenceLevelRequired;
                order.CreatorIsInterpreterUser = EnumHelper.Parse<TrueFalse>(CreatorIsInterpreterUser.SelectedItem.Value) == TrueFalse.Yes;
                if (!isGroupOrder)
                {
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
                            OrderRequirement requirement = new OrderRequirement
                            {
                                RequirementType = req.RequirementType.Value,
                                IsRequired = true,
                                Description = req.RequirementDescription
                            };
                            order.Requirements.Add(requirement);
                        }
                    }
                    if (OrderDesiredRequirements != null)
                    {
                        // add all extra desired requirements
                        foreach (var req in OrderDesiredRequirements)
                        {
                            OrderRequirement requirement = new OrderRequirement
                            {
                                RequirementType = req.DesiredRequirementType.Value,
                                IsRequired = false,
                                Description = req.DesiredRequirementDescription
                            };
                            order.Requirements.Add(requirement);
                        }
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
                if (RequestedCompetenceLevels.Any())
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

        internal void UpdateModelWithDefaultSettings(List<int> units, List<int> regions)
        {
            if (UserDefaultSettings != null)
            {
                RegionId = UserDefaultSettings.RegionId.HasValue && regions.Contains(UserDefaultSettings.RegionId.Value) ? UserDefaultSettings.RegionId : null;
                InvoiceReference = UserDefaultSettings.InvoiceReference;
                CustomerUnitId = (UserDefaultSettings.CustomerUnitId.HasValue && (UserDefaultSettings.CustomerUnitId == 0 || units.Contains(UserDefaultSettings.CustomerUnitId.Value))) ? UserDefaultSettings.CustomerUnitId : null;
                AllowExceedingTravelCost = UserDefaultSettings.AllowExceedingTravelCost != null ?
                    new RadioButtonGroup { SelectedItem = SelectListService.AllowExceedingTravelCost.Single(e => e.Value == UserDefaultSettings.AllowExceedingTravelCost.ToString()) } : null;
                RankedInterpreterLocationFirst = UserDefaultSettings.RankedInterpreterLocationFirst;
                RankedInterpreterLocationSecond = UserDefaultSettings.RankedInterpreterLocationSecond;
                RankedInterpreterLocationThird = UserDefaultSettings.RankedInterpreterLocationThird;
            }
        }

        #endregion
    }
}
