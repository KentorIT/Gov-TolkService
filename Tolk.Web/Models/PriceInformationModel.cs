using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
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

        public decimal ExpectedTravelCosts { get; set; }
        public bool MealBreakIsNotDetucted { get; set; }

#warning Detta är inte ok, tror jag...
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
            return GetPriceinformationToDisplay(order.PriceRows.OfType<PriceRowBase>().ToList(), PriceInformationType.Order, initialCollapse, order.MealBreakIncluded ?? false);
        }

        internal static PriceInformationModel GetPriceinformationToDisplay(IEnumerable<PriceRowBase> priceRows, PriceInformationType type, bool initialCollapse = true, bool mealBreakIncluded = false)
        {
            return new PriceInformationModel
            {
                Header = type.GetDescription(),
				MealBreakIsNotDetucted = mealBreakIncluded,
				PriceInformationToDisplay = PriceCalculationService.GetPriceInformationToDisplay(priceRows),
                UseDisplayHideInfo = true,
                InitialCollapse = initialCollapse,
                ExpectedTravelCosts = priceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0
            };
        }

        internal static PriceInformationModel GetPriceinformationToDisplay(Request request, bool initialCollapse = true)
        {
            if (request.PriceRows == null || !request.PriceRows.Any())
            {
                return null;
            }
            return GetPriceinformationToDisplay(request.PriceRows.OfType<PriceRowBase>().ToList(), PriceInformationType.Request, initialCollapse, request.Order.MealBreakIncluded ?? false);
        }

    }
}
