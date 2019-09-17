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
    public class DefaultSettingsModel
    {
        public int Id { get; set; }
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

        [Display(Name = "Fakturareferens")]
        [StringLength(100)]
        [Placeholder("Er fakturareferens...")]
        public string InvoiceReference { get; set; }

        public UserPageMode UserPageMode { get; set; }
    }
}