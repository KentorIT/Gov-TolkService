using System.ComponentModel;


namespace Tolk.BusinessLogic.Enums
{
    public enum BrokerFeeCalculationType
    {
        [Description("Förmedlingsavgiften beräknas från region och förmedling")]
        ByRegionAndBroker = 1,
        [Description("Förmedlingsavgiften beräknas från region och vald tolktjänst")]
        ByRegionAndServiceType = 2
    }
}
