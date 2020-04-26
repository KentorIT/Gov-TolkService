using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Enums
{
    public enum PriceInformationType
    {
        [Description("Beräknat pris enligt ursprunglig bokningsförfrågan")]
        Order = 1,
        [Description("Beräknat pris enligt bokningsbekräftelse")]
        Request = 2,
        [Description("Pris enligt tidigare rekvisition")]
        Requisition = 3
    }
}
