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

namespace Tolk.Web.Models
{
    public class OrderBaseModel
    {
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

        [Display(Name = "Bokning skapad av")]
        public string CreatedByName { get; set; }

        [Display(Name = "Fakturareferens", Description = "Här anger du den beställarreferens enligt era interna instruktioner som krävs för att fakturan för tolkuppdraget ska komma till rätt mottagare i er myndighet.")]
        [StringLength(100)]
        [Placeholder("Referens för korrekt fakturering...")]
        [Required]
        public string InvoiceReference { get; set; }

        [Display(Name = "Bokning besvarad av")]
        [DataType(DataType.MultilineText)]
        public string AnsweredBy { get; set; }

        public DateTimeOffset? AnsweredAt { get; set; }

        [Display(Name = "Person med rätt att granska rekvisition", Description = "Person som har rätt att granska rekvisition")]
        [DataType(DataType.MultilineText)]
        public string ContactPerson { get; set; }

        [Display(Name = "Myndighet")]
        public string CustomerName { get; set; }

        [Display(Name = "Myndighetens organisationsnummer")]
        public string CustomerOrganisationNumber { get; set; }

        [Display(Name = "Tolken fakturerar själv tolkarvode")]
        public bool CustomerUseSelfInvoicingInterpreter { get; set; }

        [Display(Name = "Myndighetens avdelning")]
        [StringLength(100)]
        public string UnitName { get; set; }

        [Display(Name = "Förmedling")]
        public string BrokerName { get; set; }

        [Display(Name = "Förmedlingens organisationsnummer")]
        public string BrokerOrganizationNumber { get; set; }

        [Display(Name = "Status på bokningen")]
        public OrderStatus Status { get; set; }

        public PriceInformationModel OrderCalculatedPriceInformationModel { get; set; }

        [Display(Name = "Angiven bedömd resekostnad (exkl. moms)")]
        [DataType(DataType.Currency)]
        public virtual decimal ExpectedTravelCosts { get; set; }

        [Display(Name = "Kommentar till bedömd resekostnad")]
        [DataType(DataType.MultilineText)]
        public string ExpectedTravelCostInfo { get; set; }

        public bool? LanguageHasAuthorizedInterpreter { get; set; }

        [Display(Name = "Tolkens kompetensnivå", Description = "Kompetensnivå kan anges som krav eller önskemål. Maximalt två alternativ kan anges. Om kompetensnivå anges som krav ska förmedlingen tillsätta tolk med någon av angivna alternativ. Om kompetensnivå anges som önskemål kan förmedlingen tillsätta tolk enligt något av alternativen. Om inget krav eller önskemål om kompetensnivå har angetts, eller om förmedlingen inte kan tillgodose angivna önskemål, måste förmedlingen tillsätta tolk med högsta möjliga kompetensnivå enligt principen om kompetensprioritering.")]
        [ClientRequired]
        public RadioButtonGroup CompetenceLevelDesireType { get; set; }

        [Prefix(PrefixPosition = PrefixAttribute.Position.Value, Text = "<span class=\"competence-ranking-num\">1. </span>")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelFirst { get; set; }

        [NoDisplayName]
        [Prefix(PrefixPosition = PrefixAttribute.Position.Value, Text = "<span class=\"competence-ranking-num\">2. </span>")]
        public CompetenceAndSpecialistLevel? RequestedCompetenceLevelSecond { get; set; }

        [ClientRequired]
        [Display(Name = "Accepterar restid eller resväg som överskrider gränsvärden", Description = "Vid tolkning med inställelsesätt På plats eller Distans i anvisad lokal per video har förmedlingen rätt att debitera kostnader för tolkens resor upp till ramavtalets gränsvärden på 2 timmars restid eller 100 km resväg. Resekostnader som överskrider gränsvärdena måste godkännas av myndighet i förväg. Genom att du markerat denna ruta måste förmedlingen ange bedömd resekostnad för tillsatt tolk i sin bekräftelse. Du får ett e-postmeddelande när bekräftelsen kommit. Om du underkänner bedömd resekostnad går förfrågan vidare till nästa förmedling enligt rangordningen.")]
        public RadioButtonGroup AllowExceedingTravelCost { get; set; }

        [Display(Name = "Är tolkanvändare samma person som bokar")]
        public bool? IsCreatorInterpreterUser { get; set; }

        public IEnumerable<OrderOccasionDisplayModel> OrderOccasionDisplayModels { get; set; }

        public AttachmentListModel AttachmentListModel { get; set; }

        [Display(Name = "Kompetensnivå är ett krav")]
        public virtual bool SpecificCompetenceLevelRequired => CompetenceLevelDesireType == null ? false : EnumHelper.Parse<DesireType>(CompetenceLevelDesireType.SelectedItem.Value) == DesireType.Requirement;

        public decimal TotalPrice => OrderOccasionDisplayModels?.Sum(o => o.PriceInformationModel.TotalPriceToDisplay) ?? 0;

        [Display(Name = "Myndighetens ärendenummer", Description = "Fält för att koppla till ett ärendenummer i er verksamhet.")]
        [StringLength(100)]
        public string CustomerReferenceNumber { get; set; }

        [Display(Name = "Anledning till att svaret inte godtas")]
        [DataType(DataType.MultilineText)]
        [ClientRequired]
        [StringLength(1000)]
        [Placeholder("Beskriv anledning till varför du inte godtar svaret.")]
        public string DenyMessage { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Övrig information om uppdraget", Description = "Eventuell annan information som är viktig eller relevant för förmedling eller tolk, t ex vägbeskrivning, ärendeinformation eller förutsättningar i övrigt för tolkuppdragets genomförande. Här kan du även ange kontaktuppgifter till person som tolken skall kontakta. Beakta eventuell sekretess avseende informationen.")]
        [Placeholder("T ex vägbeskrivning, ärendeinformation eller övriga förutsättningar för tolkuppdraget. Beakta eventuell sekretess avseende informationen.")]
        [StringLength(1000)]
        public string Description { get; set; }

        [Display(Name = "Språk och dialekt")]
        [DataType(DataType.MultilineText)]
        public string LanguageAndDialect => $"{LanguageName}\n{DialectDescription}";

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
        public List<CompetenceAndSpecialistLevel> RequestedCompetenceLevels
        {
            get
            {
                List<CompetenceAndSpecialistLevel> list = new List<CompetenceAndSpecialistLevel>();
                if (RequestedCompetenceLevelFirst.HasValue)
                {
                    list.Add(RequestedCompetenceLevelFirst.Value);
                }
                if (RequestedCompetenceLevelSecond.HasValue)
                {
                    list.Add(RequestedCompetenceLevelSecond.Value);
                }
                return list;
            }
        }

        [Display(Name = "Tillsatt tolk")]
        [DataType(DataType.MultilineText)]
        public string InterpreterName { get; set; }

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

        [Display(Name = "Tolkens kompetensnivå", Description = "Kompetensnivå kan anges som krav eller önskemål. Maximalt två alternativ kan anges. Om kompetensnivå anges som krav ska förmedlingen tillsätta tolk med någon av angivna alternativ. Om kompetensnivå anges som önskemål kan förmedlingen tillsätta tolk enligt något av alternativen. Om inget krav eller önskemål om kompetensnivå har angetts, eller om förmedlingen inte kan tillgodose angivna önskemål, måste förmedlingen tillsätta tolk med högsta möjliga kompetensnivå enligt principen om kompetensprioritering.")]
        public CompetenceAndSpecialistLevel? InterpreterCompetenceLevel { get; set; }

        [Display(Name = "Inställelsesätt enl. svar")]
        public InterpreterLocation InterpreterLocationAnswer { get; set; }

        public string InterpreterLocationInfoAnswer { get; set; }

        #region extra requirements

        [Display(Name = "Tillkommande krav och/eller önskemål", Description = "Klicka på +-ikonen för att lägga till andra krav såsom tolkens kön, specifik tolk eller andra krav. Förmedlingen behöver inte uppfylla önskemål.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<OrderRequirementModel> OrderRequirements { get; set; }


        [Display(Name = "Tillkommande önskemål", Description = "Klicka på +-ikonen för att lägga till andra önskemål såsom tolkens kön, specifik tolk eller andra önskemål. Önskemål är inte tvingande för förmedlingen")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<OrderDesiredRequirementModel> OrderDesiredRequirements { get; set; }

        #endregion

        public virtual DateTimeOffset? StartAt 
        {
            get;
        }

        public virtual bool AllowProcessing { get; set; } = false;

        public bool TerminateOnDenial { get; set; } = false;

        public bool UseAttachments { get; set; }

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

        internal static InterpreterLocationAddressModel GetInterpreterLocation(OrderInterpreterLocation location)
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

    }
}
