using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Enums;

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

        public bool MealBreakIsNotDetucted { get; set; }

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
            return GetPriceinformationToDisplay(order.PriceRows.OfType<PriceRowBase>().ToList(), PriceInformationType.Order, initialCollapse);
        }

        internal static PriceInformationModel GetPriceinformationToDisplay(IEnumerable<PriceRowBase> priceRows, PriceInformationType type, bool initialCollapse = true)
        {
            return new PriceInformationModel
            {
                Header = type.GetDescription(),
				MealBreakIsNotDetucted = order.MealBreakIncluded ?? false,
				PriceInformationToDisplay = PriceCalculationService.GetPriceInformationToDisplay(priceRows),
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
            };
FELFELFEL saknar meal break!
return GetPriceinformationToDisplay(request.PriceRows.OfType<PriceRowBase>().ToList(), PriceInformationType.Request, initialCollapse);
            return GetPriceinformationToDisplay(request.PriceRows.OfType<PriceRowBase>().ToList(), PriceInformationType.Request, initialCollapse);
            {
                MealBreakIsNotDetucted = request.Order.MealBreakIncluded ?? false,
                PriceInformationToDisplay = PriceCalculationService.GetPriceInformationToDisplay(request.PriceRows.OfType<PriceRowBase>().ToList()),
                Header = "Beräknat pris enligt bokningsbekräftelse",
                UseDisplayHideInfo = true,
                InitialCollapse = initialCollapse
            };
        }

    }
}
