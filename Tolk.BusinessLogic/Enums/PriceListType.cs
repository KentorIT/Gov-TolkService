using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum PriceListType
    {
        [CustomName("domstolsverket")]
        [Description("Domstolsverket")]
        Court = 1,
        [CustomName("övriga_myndigheter")]
        [Description("Övriga myndigheter")]
        Other = 2
    }
}
