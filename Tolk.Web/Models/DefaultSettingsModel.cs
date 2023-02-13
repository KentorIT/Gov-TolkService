using Microsoft.Build.ObjectModelRemoting;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class DefaultSettingsModel : IModel
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

        [Display(Name = "Gatuadress", Description = "Ange tydlig gatuadress så att tolken hittar. Mer information kan anges i fältet för övrig information om uppdraget i samband med att bokning görs")]
        [StringLength(100)]
        [SubItem]
        [Placeholder("Er gatuadress...")]
        public string OnSiteLocationStreet { get; set; }

        [Display(Name = "Ort")]
        [StringLength(100)]
        [SubItem]
        [Placeholder("Er ort...")]
        public string OnSiteLocationCity { get; set; }

        [Display(Name = "Gatuadress", Description = "Ange tydlig gatuadress så att tolken hittar. Mer information kan anges i fältet för övrig information om uppdraget i samband med att bokning görs")]
        [StringLength(100)]
        [SubItem]
        [Placeholder("Lokalens gatuadress...")]
        public string OffSiteDesignatedLocationStreet { get; set; }

        [Display(Name = "Ort")]
        [StringLength(100)]
        [SubItem]
        [Placeholder("Lokalens ort...")]
        public string OffSiteDesignatedLocationCity { get; set; }

        [Display(Name = "Telefon", Description = "Ange vilket telefonnummer tolken ska ringa upp på eller om ni istället själva vill ringa upp tolken")]
        [StringLength(255)]
        [SubItem]
        [Placeholder("Information om hur man når er...")]
        public string OffSitePhoneContactInformation { get; set; }

        [Display(Name = "Video", Description = "Ange vilket system som ska användas för videomötet. Mer information om anslutning etc. kan anges i fältet för övrig information om uppdraget i samband med att bokning görs")]
        [StringLength(255)]
        [SubItem]
        [Placeholder("Information om hur man når er...")]
        public string OffSiteVideoContactInformation { get; set; }

        [Display(Name = "Accepterar restid eller resväg som överskrider gränsvärden", Description = "Vid tolkning med inställelsesätt På plats eller Distans i anvisad lokal per video har förmedlingen rätt att debitera kostnader för tolkens resor upp till ramavtalets gränsvärden på 2 timmars restid eller 100 km resväg. Resekostnader som överskrider gränsvärdena måste godkännas av myndighet i förväg. Genom att du markerat denna ruta måste förmedlingen ange bedömd resekostnad för tillsatt tolk i sin bekräftelse. Du får ett e-postmeddelande när bekräftelsen kommit. Om du underkänner bedömd resekostnad går förfrågan vidare till nästa förmedling enligt rangordningen.")]
        public AllowExceedingTravelCost? AllowExceedingTravelCost { get; set; }

        [Display(Name = "Är tolkanvändare samma person som bokar", Description = "Om du är den som brukar använda tolken, dvs hålla i mötet, så välj Ja. Om du brukar boka tolk åt en annan person på myndigheten så välj Nej")]
        public TrueFalse? CreatorIsInterpreterUser { get; set; }

        [Display(Name = "Fakturareferens", Description = "Här anger du den beställarreferens enligt era interna instruktioner som krävs för att fakturan för tolkuppdraget ska komma till rätt mottagare i er myndighet.")]
        [StringLength(100)]
        [Placeholder("Er fakturareferens...")]
        public string InvoiceReference { get; set; }

        public List<OrderRequirementModel> OrderRequirements { get; set; }

        public List<OrderDesiredRequirementModel> OrderDesiredRequirements { get; set; }

        public List<OrderRequirementModel> SavedOrderRequirements { get; set; }

        public List<OrderDesiredRequirementModel> SavedOrderDesiredRequirements { get; set; }

        public bool IsFirstTimeUser { get; set; }

        public string TravelConditionText { get; set; }

        internal static DefaultSettingsModel GetModel(AspNetUser user, FrameworkAgreementResponseRuleset frameWorkAgreementVersion, bool isFirstTimeUser = false)
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
                CreatorIsInterpreterUser = !string.IsNullOrWhiteSpace(creatorIsInterpreterUser) ? (creatorIsInterpreterUser == "Yes") ? TrueFalse.Yes : TrueFalse.No : null,
                IsFirstTimeUser = isFirstTimeUser,
                TravelConditionText = GetTravelConditionText(frameWorkAgreementVersion),
                SavedOrderRequirements = user.DefaultSettingOrderRequirements.Where(r => r.IsRequired).Select(n => new OrderRequirementModel
                {
                    UserDefaultSettingOrderRequirementId = n.UserDefaultSettingOrderRequirementId,
                    RequirementDescription = n.Description,
                    RequirementType = n.RequirementType
                }).ToList(),
                SavedOrderDesiredRequirements = user.DefaultSettingOrderRequirements.Where(r => !r.IsRequired).Select(n => new OrderDesiredRequirementModel
                {
                    UserDefaultSettingOrderRequirementId = n.UserDefaultSettingOrderRequirementId,
                    DesiredRequirementDescription = n.Description,
                    DesiredRequirementType = n.RequirementType
                }).ToList()
            };
        }

        private static string GetTravelConditionText(FrameworkAgreementResponseRuleset r) => $"Vid tolkning med inställelsesätt På plats eller Distans i anvisad lokal per video har förmedlingen rätt att debitera kostnader för tolkens resor upp till ramavtalets gränsvärden på {EnumHelper.GetContractDefinition(r).TravelConditionHours} timmars restid eller {EnumHelper.GetContractDefinition(r).TravelConditionKilometers} km resväg. Resekostnader som överskrider gränsvärdena måste godkännas av myndighet i förväg. Genom att du markerat denna ruta måste förmedlingen ange bedömd resekostnad för tillsatt tolk i sin bekräftelse. Du får ett e-postmeddelande när bekräftelsen kommit. Om du underkänner bedömd resekostnad går förfrågan vidare till nästa förmedling enligt rangordningen.";

    }

}
