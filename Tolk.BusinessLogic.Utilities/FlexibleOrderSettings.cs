using System;

namespace Tolk.BusinessLogic.Utilities
{
    public class FlexibleOrderSettings
    {
        public bool UseFlexibleOrders { get; set; }
        public bool AllowOnNonWorkdays { get; set; }
        public int EarliestStartAtHour { get; set; }
        public int LatestEndAtHour { get; set; }
        public string Description => $"Använd: {(UseFlexibleOrders.ToSwedishString())}\nTillåt bokning på helgdagar: {AllowOnNonWorkdays.ToSwedishString()}\nTidigaste flexibel starttid: {EarliestStartAtHour}\nSenaste flexibel sluttid: {LatestEndAtHour}";
    }
}
