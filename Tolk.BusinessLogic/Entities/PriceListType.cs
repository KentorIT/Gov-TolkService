using System.ComponentModel;

namespace Tolk.BusinessLogic.Entities
{
    public enum PriceListType
    {
        [Description("Domstolsverket")]
        Court = 1,
        [Description("Övriga myndigheter")]
        Other = 2
    }
}
