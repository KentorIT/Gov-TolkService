using System;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class StartListItemModel
    {
        public string DefaulListAction { get; set; }

        public string DefaulListController { get; set; }

        public int DefaultItemId { get; set; }

        public string ButtonAction { get; set; }

        public string ButtonController { get; set; }

        public int ButtonItemId { get; set; }

        public StartListItemStatus Status { get; set; }

        public string CustomerName { get; set; }

        public string OrderNumber { get; set; }

        [NoDisplayName]
        public TimeRange Orderdate { get; set; }

        public CompetenceAndSpecialistLevel? CompetenceLevel { get; set; }

        public string Language { get; set; }

        public DateTime InfoDate { get; set; }

        public DateTime? LatestDate { get; set; }

        public string InfoDateDescription { get; set; } = "Inkommen: ";

        public string ColorClassName
        {
            get => (Status == StartListItemStatus.ComplaintEvent || Status == StartListItemStatus.RequestArrived || Status == StartListItemStatus.RequestReceived || Status == StartListItemStatus.RequisitonArrived) ? "blue-border-left" :
            (Status == StartListItemStatus.RequisitionDenied || Status == StartListItemStatus.OrderCancelled || Status == StartListItemStatus.OrderNotAnswered || Status == StartListItemStatus.RequestDenied) ? "red-border-left" :
            (Status == StartListItemStatus.OrderApproved || Status == StartListItemStatus.RequisitionToBeCreated) ? "green-border-left" :
            (Status == StartListItemStatus.RequisitionAwaited || Status == StartListItemStatus.OrderCreated || Status == StartListItemStatus.RequisitionCreated) ? "gray-border-left" : "yellow-border-left";
        }
    }

}
