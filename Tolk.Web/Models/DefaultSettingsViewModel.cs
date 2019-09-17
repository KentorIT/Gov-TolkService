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
using Tolk.BusinessLogic.Helpers;

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

        [Display(Name = "Fakturareferens")]
        public string InvoiceReference { get; set; }

        public string Message { get; set; }
        public bool ShowUnitSelection { get; set; }
        public bool AllowChange { get; set; }

        public UserPageMode UserPageMode { get; set; }
    }
}
