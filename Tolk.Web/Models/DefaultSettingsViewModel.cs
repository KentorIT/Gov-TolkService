﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Models.CustomerSpecificProperties;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class DefaultSettingsViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Län")]
        public string Region { get; set; }

        [Display(Name = "Enhet")]
        public string CustomerUnit { get; set; }

        [Display(Name = "Inställelsesätt - första hand")]
        public InterpreterLocation? RankedInterpreterLocationFirst { get; set; }

        [Display(Name = "Inställelsesätt - andra hand")]
        public InterpreterLocation? RankedInterpreterLocationSecond { get; set; }

        [Display(Name = "Inställelsesätt - tredje hand")]
        public InterpreterLocation? RankedInterpreterLocationThird { get; set; }

        [Display(Name = "Gatuadress")]
        [SubItem]
        public string OnSiteLocationStreet { get; set; }

        [Display(Name = "Ort")]
        [SubItem]
        public string OnSiteLocationCity { get; set; }

        [Display(Name = "Gatuadress")]
        [SubItem]
        public string OffSiteDesignatedLocationStreet { get; set; }

        [Display(Name = "Ort")]
        [SubItem]
        public string OffSiteDesignatedLocationCity { get; set; }

        [Display(Name = "Telefon")]
        [SubItem]
        public string OffSitePhoneContactInformation { get; set; }

        [Display(Name = "Video")]
        [SubItem]
        public string OffSiteVideoContactInformation { get; set; }

        [Display(Name = "Accepterar restid eller resväg som överskrider gränsvärden")]
        public AllowExceedingTravelCost? AllowExceedingTravelCost { get; set; }

        [Display(Name = "Är tolkanvändare samma person som bokar")]
        public bool? CreatorIsInterpreterUser { get; set; }

        [Display(Name = "Fakturareferens")]
        public string InvoiceReference { get; set; }
        public CustomerSpecificPropertyModel CustomerSpecificInvoiceReference { get; set; }

        [Display(Name = "Tillkommande krav")]
        public List<OrderRequirementModel> OrderRequirements { get; set; }

        [Display(Name = "Tillkommande önskemål")]
        public List<OrderDesiredRequirementModel> OrderDesiredRequirements { get; set; }

        public string Message { get; set; }
        public bool ShowUnitSelection { get; set; }

        public UserPageMode UserPageMode { get; set; }

        internal static DefaultSettingsViewModel GetModel(AspNetUser user, Region[] regions, string message = null)
        {
            int? customerUnit = user.GetIntValue(DefaultSettingsType.CustomerUnit);
            var creatorIsInterpreterUser = user.GetValue(DefaultSettingsType.CreatorIsInterpreterUser);
            return new DefaultSettingsViewModel
            {
                Message = message,
                ShowUnitSelection = user.CustomerUnits.Any(),
                Region = regions.SingleOrDefault(r => r.RegionId == user.GetIntValue(DefaultSettingsType.Region))?.Name,
                CustomerUnit = customerUnit == 0 ? Constants.SelectNoUnit : user.CustomerUnits.SingleOrDefault(c => c.CustomerUnitId == customerUnit)?.CustomerUnit.Name,
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
                CreatorIsInterpreterUser = !string.IsNullOrWhiteSpace(creatorIsInterpreterUser) ? (bool?)(creatorIsInterpreterUser == "Yes") : null,
                OrderRequirements = user.DefaultSettingOrderRequirements.Where(r => r.IsRequired).Select(n => new OrderRequirementModel
                {
                    UserDefaultSettingOrderRequirementId = n.UserDefaultSettingOrderRequirementId,
                    RequirementDescription = n.Description,
                    //RequirementIsRequired = true,
                    RequirementType = n.RequirementType
                }).ToList(),
                OrderDesiredRequirements = user.DefaultSettingOrderRequirements.Where(r => !r.IsRequired).Select(n => new OrderDesiredRequirementModel
                {
                    UserDefaultSettingOrderRequirementId = n.UserDefaultSettingOrderRequirementId,
                    DesiredRequirementDescription = n.Description,
                    DesiredRequirementType = n.RequirementType
                }).ToList()
            };
        }
    }
}
