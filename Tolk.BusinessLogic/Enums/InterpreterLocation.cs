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
        [Description("Distans")]
        OffSite = 2,
        [Description("Distans i anvisad lokal")]
        OffSiteDesignatedLocation = 3,
    }
}
