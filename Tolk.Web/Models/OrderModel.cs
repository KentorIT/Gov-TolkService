using System;
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

        [Display(Name = "Extra tolk", Description = "Om denna kryssruta kryssas i så betyder det att man vill ha två tolkar till samma tillfälle. Det innebär arvode och förmedlingsavgift för båda tolkarna för hela tilfället.")]
        public bool ExtraInterpreter { get; set; }

        [Display(Name = "Boka flera tillfällen", Description = "Om denna kryssruta kryssas i så kan man lägga till flera tillfällen. Det är tvingande för förmedlingen att tillsätta samma tolk för alla tillfällen. Detta innebär arvode och förmedlingsavgift för varje tillfälle. Fyll i ett fullständigt tillfälle för att kunna lägga till fler.")]
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<OrderOccasionModel> Occasions { get; set; }

        public bool DisplayForBroker { get; set; } = false;

        public string WarningOrderTimeInfo { get; set; } = string.Empty;

        public string WarningOrderRequiredCompetenceInfo { get; set; } = string.Empty;

        public string WarningOrderGroupCloseInTime { get; set; } = string.Empty;

        public PriceInformation PriceInformation { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
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

        [Display(Name = "Skapa ersättningsuppdrag")]
        public bool AddReplacementOrder { get; set; } = false;

        public bool AllowComplaintCreation { get; set; } = false;

        public bool AllowRequestPrint { get; set; } = false;

        public bool AllowNoAnswerConfirmation { get; set; } = false;

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

        public OrderOccasionDisplayModel FirstOccasion
        {
            get => UniqueOrdersFromOccasions.OrderBy(o => o.OccasionStartDateTime).FirstOrDefault();
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
                            yield return new OrderOccasionDisplayModel(occasion) { ExtraInterpreterFor = id - 1, OrderOccasionId = id++ };
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
                            ExtraInterpreter = false,
                            OrderOccasionId = id
                        };
                        if (ExtraInterpreter)
                        {
                            yield return new OrderOccasionDisplayModel
                            {
                                OccasionStartDateTime = SplitTimeRange.StartAt.Value.DateTime,
                                OccasionEndDateTime = SplitTimeRange.EndAt.Value.DateTime,
                                ExtraInterpreter = true,
                                ExtraInterpreterFor = id,
                                OrderOccasionId = ++id
                            };
                        }
                    }
                }
            }
        }

        public DefaultSettingsModel UserDefaultSettings { get; set; }

        #region methods

        internal void UpdateOrderGroup(OrderGroup orderGroup)
        {
            orderGroup.Attachments = Files?.Select(f => new OrderGroupAttachment { AttachmentId = f.Id }).ToList();
            var location = RankedInterpreterLocationFirst.Value;
            orderGroup.InterpreterLocations.Add(new OrderGroupInterpreterLocation { Rank = 1, InterpreterLocation = location});
            if (RankedInterpreterLocationSecond.HasValue)
            {
                orderGroup.InterpreterLocations.Add(new OrderGroupInterpreterLocation { Rank = 2, InterpreterLocation = RankedInterpreterLocationSecond.Value });
                if (RankedInterpreterLocationThird.HasValue)
                {
                    orderGroup.InterpreterLocations.Add(new OrderGroupInterpreterLocation { Rank = 3, InterpreterLocation = RankedInterpreterLocationThird.Value });
                }
            }
            orderGroup.LanguageId = LanguageId;
            orderGroup.OtherLanguage = OtherLanguageId == LanguageId ? OtherLanguage : null;
            orderGroup.LanguageHasAuthorizedInterpreter = LanguageHasAuthorizedInterpreter ?? false;
            orderGroup.RegionId = RegionId.Value;
            orderGroup.AssignmentType = EnumHelper.Parse<AssignmentType>(AssignmentType.SelectedItem.Value);
            orderGroup.CustomerUnitId = (CustomerUnitId.HasValue && CustomerUnitId > 0) ? CustomerUnitId : null;
            if (HasOnsiteLocation && AllowExceedingTravelCost != null)
            {
                orderGroup.AllowExceedingTravelCost = EnumHelper.Parse<AllowExceedingTravelCost>(AllowExceedingTravelCost.SelectedItem.Value);
            }
            orderGroup.SpecificCompetenceLevelRequired = SpecificCompetenceLevelRequired;
                if (Dialect != null)
                {
                    orderGroup.Requirements.Add(new OrderGroupRequirement
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
                        OrderGroupRequirement requirement = new OrderGroupRequirement
                        {
                            RequirementType = req.RequirementType.Value,
                            IsRequired = true,
                            Description = req.RequirementDescription
                        };
                        orderGroup.Requirements.Add(requirement);
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

        internal void UpdateOrder(Order order, DateTimeOffset startAt, DateTimeOffset endAt, bool isReplace = false, bool isGroupOrder = false)
        {
            order.CustomerReferenceNumber = CustomerReferenceNumber;
            order.StartAt = startAt;
            order.EndAt = endAt;
            order.Description = Description;
            order.UnitName = UnitName;
            order.ContactPersonId = ContactPersonId;
            if (!isGroupOrder)
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
                order.AssignmentType = EnumHelper.Parse<AssignmentType>(AssignmentType.SelectedItem.Value);
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
       


        internal static OrderModel GetModelFromOrder(Order order, int? activeRequestId = null, bool displayForBroker = false)
        {
            bool useRankedInterpreterLocation = order.InterpreterLocations.Count > 1;

            OrderCompetenceRequirement competenceFirst = null;
            OrderCompetenceRequirement competenceSecond = null;
            var competenceRequirements = order.CompetenceRequirements.Select(r => new OrderCompetenceRequirement
            {
                CompetenceLevel = r.CompetenceLevel,
                Rank = r.Rank,
            }).ToList();

            competenceRequirements = competenceRequirements.OrderBy(r => r.Rank).ToList();
            competenceFirst = competenceRequirements.Count > 0 ? competenceRequirements[0] : null;
            competenceSecond = competenceRequirements.Count > 1 ? competenceRequirements[1] : null;

            return new OrderModel
            {
                DisplayForBroker = displayForBroker,
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                ReplacingOrderNumber = order?.ReplacingOrder?.OrderNumber,
                ReplacedByOrderNumber = order?.ReplacedByOrder?.OrderNumber,
                ReplacedByOrderId = order?.ReplacedByOrder?.OrderId,
                ReplacingOrderId = order.ReplacingOrderId,
                OrderGroupId = order.OrderGroupId,
                OrderGroupNumber = order.OrderGroupId.HasValue ? order.Group.OrderGroupNumber : string.Empty,
                CreatedBy = order.ContactInformation,
                CreatedById = order.CreatedBy,
                ContactPerson = order.ContactPersonUser?.CompleteContactInformation,
                ChangeContactPersonId = order.ContactPersonId,
                CreatedAt = order.CreatedAt,
                InvoiceReference = order.InvoiceReference,
                CustomerName = order.CustomerOrganisation.Name,
                CustomerOrganisationNumber = order.CustomerOrganisation.OrganisationNumber,
                LanguageName = order.OtherLanguage ?? order.Language?.Name ?? "-",
                CustomerUnitName = order.CustomerUnit?.Name ?? string.Empty,
                Dialect = order.Requirements.Any(r => r.RequirementType == RequirementType.Dialect) ? order.Requirements.Single(r => r.RequirementType == RequirementType.Dialect)?.Description : string.Empty,
                RegionName = order.Region.Name,
                LanguageId = order.LanguageId,
                LanguageHasAuthorizedInterpreter = order.LanguageHasAuthorizedInterpreter,
                AllowExceedingTravelCost = displayForBroker ? new RadioButtonGroup { SelectedItem = order.AllowExceedingTravelCost == null ? null : SelectListService.BoolList.Single(e => e.Value == EnumHelper.Parent<AllowExceedingTravelCost, TrueFalse>(order.AllowExceedingTravelCost.Value).ToString()) } : new RadioButtonGroup { SelectedItem = order.AllowExceedingTravelCost == null ? null : SelectListService.AllowExceedingTravelCost.Single(e => e.Value == order.AllowExceedingTravelCost.ToString()) },
                AssignmentType = new RadioButtonGroup { SelectedItem = SelectListService.AssignmentTypes.Single(e => e.Value == order.AssignmentType.ToString()) },
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
                RequestedCompetenceLevelFirst = competenceFirst?.CompetenceLevel,
                RequestedCompetenceLevelSecond = competenceSecond?.CompetenceLevel,
                Status = order.Status,
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
                       r.Status == BusinessLogic.Enums.RequestStatus.DeniedByCreator ||
                       r.Status == BusinessLogic.Enums.RequestStatus.LostDueToQuarantine
                ).Select(r => new BrokerListModel
                {
                    Status = r.Status,
                    BrokerName = r.Ranking.Broker.Name,
                    DenyMessage = r.DenyMessage,
                }).ToList(),
            };
        }

        internal void UpdateModelWithDefaultSettings(List<int> units)
        {
            if (UserDefaultSettings != null)
            {
                RegionId = Region.Regions.Any(r => r.RegionId == UserDefaultSettings.RegionId) ? UserDefaultSettings.RegionId : null;
                InvoiceReference = UserDefaultSettings.InvoiceReference;
                CustomerUnitId = (UserDefaultSettings.CustomerUnitId.HasValue && (UserDefaultSettings.CustomerUnitId == 0  || units.Contains(UserDefaultSettings.CustomerUnitId.Value))) ? UserDefaultSettings.CustomerUnitId : null;
                AllowExceedingTravelCost = UserDefaultSettings.AllowExceedingTravelCost != null ?
                    new RadioButtonGroup { SelectedItem = SelectListService.AllowExceedingTravelCost.Single(e => e.Value == UserDefaultSettings.AllowExceedingTravelCost.ToString()) } : null;
                RankedInterpreterLocationFirst = UserDefaultSettings.RankedInterpreterLocationFirst;
                RankedInterpreterLocationSecond = UserDefaultSettings.RankedInterpreterLocationSecond;
                RankedInterpreterLocationThird = UserDefaultSettings.RankedInterpreterLocationThird;
            }
        }

        internal static OrderModel GetModelFromOrderForConfirmation(Order order)
        {
            bool useRankedInterpreterLocation = order.InterpreterLocations.Count > 1;

            OrderCompetenceRequirement competenceFirst = null;
            OrderCompetenceRequirement competenceSecond = null;
            var competenceRequirements = order.CompetenceRequirements.Select(r => new OrderCompetenceRequirement
            {
                CompetenceLevel = r.CompetenceLevel,
                Rank = r.Rank,
            }).ToList();

            competenceRequirements = competenceRequirements.OrderBy(r => r.Rank).ToList();
            competenceFirst = competenceRequirements.Count > 0 ? competenceRequirements[0] : null;
            competenceSecond = competenceRequirements.Count > 1 ? competenceRequirements[1] : null;

            return new OrderModel
            {
                AllowExceedingTravelCost = new RadioButtonGroup { SelectedItem = order.AllowExceedingTravelCost == null ? null : SelectListService.AllowExceedingTravelCost.Single(e => e.Value == order.AllowExceedingTravelCost.ToString()) },
                AssignmentType = new RadioButtonGroup { SelectedItem = SelectListService.AssignmentTypes.Single(e => e.Value == order.AssignmentType.ToString()) },
                RegionId = order.RegionId,
                CustomerReferenceNumber = order.CustomerReferenceNumber,
                CustomerUnitId = order.CustomerUnitId,
                InvoiceReference = order.InvoiceReference,
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

        private static OrderInterpreterLocation GetInterpreterLocation(InterpreterLocation location, int rank, InterpreterLocationAddressModel addressModel)
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
