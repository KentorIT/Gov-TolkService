using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class DefaultSettingsModel
    {

        [Display(Name = "Län", Description = "Län för den plats där tolkbehovet finns. I det fall tolkning sker på distans anges länet där myndigheten som använder den aktuella tolktjänsten är placerad. Om tolkning ska genomföras vid en myndighets lokalkontor anges det län där lokalkontoret är placerat.")]
        public int? RegionId { get; set; }

        [Display(Name = "Enhet")]
        public int? CustomerUnitId { get; set; }

        [Display(Name = "Inställelsesätt - första hand")]
        public InterpreterLocation? RankedInterpreterLocationFirst { get; set; }

        [Display(Name = "Inställelsesätt - andra hand")]
        public InterpreterLocation? RankedInterpreterLocationSecond { get; set; }

        [Display(Name = "Inställelsesätt - tredje hand")]
        public InterpreterLocation? RankedInterpreterLocationThird { get; set; }

        [Display(Name = "Gatuadress")]
        [StringLength(100)]
        [SubItem]
        [Placeholder("Er gatuadress...")]
        public string OnSiteLocationStreet { get; set; }

        [Display(Name = "Ort")]
        [StringLength(100)]
        [SubItem]
        [Placeholder("Er ort...")]
        public string OnSiteLocationCity { get; set; }

        [Display(Name = "Gatuadress")]
        [StringLength(100)]
        [SubItem]
        [Placeholder("Lokalens gatuadress...")]
        public string OffSiteDesignatedLocationStreet { get; set; }

        [Display(Name = "Ort")]
        [StringLength(100)]
        [SubItem]
        [Placeholder("Lokalens ort...")]
        public string OffSiteDesignatedLocationCity { get; set; }

        [Display(Name = "Telefon")]
        [StringLength(255)]
        [SubItem]
        [Placeholder("Information om hur man når er...")]
        public string OffSitePhoneContactInformation { get; set; }

        [Display(Name = "Video")]
        [StringLength(255)]
        [SubItem]
        [Placeholder("Information om hur man når er...")]
        public string OffSiteVideoContactInformation { get; set; }

        [Display(Name = "Accepterar restid eller resväg som överskrider gränsvärden", Description = "Vid tolkning med inställelsesätt På plats eller Distans i anvisad lokal har förmedlingen rätt att debitera kostnader för tolkens resor upp till ramavtalets gränsvärden på 2 timmars restid eller 100 km resväg. Resekostnader som överskrider gränsvärdena måste godkännas av myndighet i förväg. Genom att du markerat denna ruta måste förmedlingen ange bedömd resekostnad för tillsatt tolk i sin bekräftelse. Du får ett e-postmeddelande när bekräftelsen kommit. Om du underkänner bedömd resekostnad går förfrågan vidare till nästa förmedling enligt rangordningen.")]
        public AllowExceedingTravelCost? AllowExceedingTravelCost { get; set; }

        [Display(Name = "Är tolkanvändare samma person som bokar", Description = "Om du brukar boka tolk åt en annan person så kryssa i ja, annars nej")]
        public TrueFalse? CreatorIsInterpreterUser { get; set; }

        [Display(Name = "Fakturareferens", Description = "Här anger du den beställarreferens enligt era interna instruktioner som krävs för att fakturan för tolkuppdraget ska komma till rätt mottagare i er myndighet.")]
        [StringLength(100)]
        [Placeholder("Er fakturareferens...")]
        public string InvoiceReference { get; set; }

        public UserPageMode UserPageMode { get; set; }

        public bool IsFirstTimeUser { get; set; }

        internal static DefaultSettingsModel GetModel(AspNetUser user, bool isFirstTimeUser = false)
        {
            var creatorIsInterpreterUser = user.GetValue(DefaultSettingsType.CreatorIsInterpreterUser);
            return new DefaultSettingsModel
            {
                RegionId = user.GetIntValue(DefaultSettingsType.Region),
                CustomerUnitId = user.GetIntValue(DefaultSettingsType.CustomerUnit),
                RankedInterpreterLocationFirst = user.TryGetEnumValue<InterpreterLocation>(DefaultSettingsType.InterpreterLocationPrimary),
                RankedInterpreterLocationSecond = user.TryGetEnumValue<InterpreterLocation>(DefaultSettingsType.InterpreterLocationSecondary),
                RankedInterpreterLocationThird = user.TryGetEnumValue<InterpreterLocation>(DefaultSettingsType.InterpreterLocationThird),
                OnSiteLocationStreet = user.GetValue(DefaultSettingsType.OnSiteStreet),
                OnSiteLocationCity = user.GetValue(DefaultSettingsType.OnSiteCity),
                OffSiteDesignatedLocationStreet = user.GetValue(DefaultSettingsType.OffSiteDesignatedLocationStreet),
                OffSiteDesignatedLocationCity = user.GetValue(DefaultSettingsType.OffSiteDesignatedLocationCity),
                OffSitePhoneContactInformation = user.GetValue(DefaultSettingsType.OffSitePhoneContactInformation),
                OffSiteVideoContactInformation = user.GetValue(DefaultSettingsType.OffSiteVideoContactInformation),
                AllowExceedingTravelCost = user.TryGetEnumValue<AllowExceedingTravelCost>(DefaultSettingsType.AllowExceedingTravelCost),
                InvoiceReference = user.GetValue(DefaultSettingsType.InvoiceReference),
                CreatorIsInterpreterUser = !string.IsNullOrWhiteSpace(creatorIsInterpreterUser) ? (creatorIsInterpreterUser == "Yes") ? (TrueFalse?)TrueFalse.Yes : (TrueFalse?)TrueFalse.No : null,
                IsFirstTimeUser = isFirstTimeUser
            };
        }

    }

}
