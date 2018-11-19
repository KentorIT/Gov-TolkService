using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum InterpreterLocation
    {
        [Description("På plats")]
        [CustomName("on_site")]
        OnSite = 1,
        [Description("Distans per telefon")]
        [CustomName("off_site_phone")]
        OffSitePhone = 2,
        [Description("Distans per video")]
        [CustomName("off_site_video")]
        OffSiteVideo = 3,
        [Description("Distans i anvisad lokal")]
        [CustomName("off_site_designated_location")]
        OffSiteDesignatedLocation = 4
    }
}
