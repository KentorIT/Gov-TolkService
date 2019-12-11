using System.Linq;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Utilities;
using System.Collections.Generic;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Models
{
    public class PriceInformationModel
    {

        public DisplayPriceInformation PriceInformationToDisplay { get; set; }

        public string Header { get; set; }

        [DataType(DataType.Currency)]
        public decimal TotalPriceToDisplay { get => PriceInformationToDisplay.TotalPrice; }

        public bool UseDisplayHideInfo { get; set; }

        public bool CenterHeader { get; set; } = false;

        public bool InitialCollapse { get; set; } = true;

        public string Description { get; set; }

        internal static PriceInformationModel GetPriceinformationToDisplay(Order order, bool initialCollapse = true, bool alwaysUseOrderPriceRows = true)
        {
            if (order.PriceRows == null)
            {
                return null;
            }
            else if (!alwaysUseOrderPriceRows && order.Requests != null && order.Requests.OrderBy(r => r.RequestId).Last().PriceRows != null && order.Requests.OrderBy(r => r.RequestId).Last().PriceRows.Any())
            {
                return GetPriceinformationToDisplay(order.Requests.OrderBy(r => r.RequestId).Last(), initialCollapse);
            }
            return new PriceInformationModel
            {
                PriceInformationToDisplay = PriceCalculationService.GetPriceInformationToDisplay(order.PriceRows.OfType<PriceRowBase>().ToList()),
                Header = "Beräknat pris enligt ursprunglig bokningsförfrågan",
                UseDisplayHideInfo = true,
                InitialCollapse = initialCollapse
            };
        }

        internal static PriceInformationModel GetPriceinformationToDisplay(Request request, bool initialCollapse = true)
        {
            if (request.PriceRows == null || !request.PriceRows.Any())
            {
                return null;
            }
            return new PriceInformationModel
            {
                PriceInformationToDisplay = PriceCalculationService.GetPriceInformationToDisplay(request.PriceRows.OfType<PriceRowBase>().ToList()),
                Header = "Beräknat pris enligt bokningsbekräftelse",
                UseDisplayHideInfo = true,
                InitialCollapse = initialCollapse
            };
        }

    }
}
