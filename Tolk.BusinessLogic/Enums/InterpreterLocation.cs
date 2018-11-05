using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{
    public enum InterpreterLocation
    {
        [Description("På plats")]
        OnSite = 1,
        [Description("Distans per telefon")]
        OffSitePhone = 2,
        [Description("Distans per video")]
        OffSiteVideo = 3,
        [Description("Distans i anvisad lokal")]
        OffSiteDesignatedLocation = 4
    }
}
