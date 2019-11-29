using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Helpers
{
    public static class CssClassHelper
    {
        public static string GetColorClassNameForOrderStatus(OrderStatus status)
        {
            return (status == OrderStatus.NoBrokerAcceptedOrder || status == OrderStatus.CancelledByCreator || status == OrderStatus.CancelledByBroker || status == OrderStatus.ResponseNotAnsweredByCreator || status == OrderStatus.NoDeadlineFromCustomer) ? "red-border-left" 
                : (status == OrderStatus.Delivered || status == OrderStatus.ResponseAccepted) ? "green-border-left" 
                : (status == OrderStatus.RequestResponded || status == OrderStatus.RequestRespondedNewInterpreter || status == OrderStatus.AwaitingDeadlineFromCustomer) ? "yellow-border-left" 
                : "blue-border-left";
        }

        internal static string GetColorClassNameForSystemMessageType(SystemMessageType systemMessageType)
        {
            return systemMessageType == SystemMessageType.Information ? "blue-border-left" : "yellow-border-left";
        }

        public static string GetColorClassNameForRequestStatus(RequestStatus status)
        {
            return (status == RequestStatus.CancelledByBroker || status == RequestStatus.CancelledByCreator || status == RequestStatus.CancelledByCreatorWhenApproved || status == RequestStatus.DeniedByCreator || status == RequestStatus.DeniedByTimeLimit || status == RequestStatus.ResponseNotAnsweredByCreator || status == RequestStatus.DeclinedByBroker || status == RequestStatus.NoDeadlineFromCustomer) ? "red-border-left" 
                : (status == RequestStatus.Approved) ? "green-border-left" 
                : (status == RequestStatus.Accepted || status == RequestStatus.AcceptedNewInterpreterAppointed || status == RequestStatus.AwaitingDeadlineFromCustomer) ? "yellow-border-left" 
                : "blue-border-left";
        }

        public static string GetColorClassNameForRequisitionStatus(RequisitionStatus status)
        {
            return (status == RequisitionStatus.Commented) ? "red-border-left" 
                : (status == RequisitionStatus.Reviewed || status == RequisitionStatus.AutomaticGeneratedFromCancelledOrder) ? "green-border-left" 
                : "blue-border-left";
        }

        public static string GetColorClassNameForComplaintStatus(ComplaintStatus status)
        {
            return (status == ComplaintStatus.Disputed || status == ComplaintStatus.DisputePendingTrial) ? "red-border-left" 
                : (status == ComplaintStatus.Confirmed || status == ComplaintStatus.TerminatedAsDisputeAccepted) ? "green-border-left" 
                : "blue-border-left";
        }

        public static string GetColorClassNameForItemStatus(bool isActive)
        {
            return isActive ? "green-border-left" : "gray-border-left";
        }

        public static string GetColorClassNameForStartListItem(StartListItemStatus status)
        {
            return (status == StartListItemStatus.ComplaintEvent || status == StartListItemStatus.RequestArrived || status == StartListItemStatus.RequestReceived || status == StartListItemStatus.RequisitonArrived || status == StartListItemStatus.ReplacementOrderRequestReceived || status == StartListItemStatus.ReplacementOrderRequestArrived || status == StartListItemStatus.RequestGroupReceived || status == StartListItemStatus.RequestGroupArrived) ? "blue-border-left" 
                : (status == StartListItemStatus.RequisitionCommented || status == StartListItemStatus.OrderCancelled || status == StartListItemStatus.OrderNotAnswered || status == StartListItemStatus.RequestDenied || status == StartListItemStatus.ReplacementOrderNotAnswered || status == StartListItemStatus.OrderGroupNotAnswered || status == StartListItemStatus.RequestGroupDenied) ? "red-border-left" 
                : (status == StartListItemStatus.OrderApproved || status == StartListItemStatus.RequisitionToBeCreated) ? "green-border-left" 
                : (status == StartListItemStatus.RequisitionAwaited || status == StartListItemStatus.OrderCreated || status == StartListItemStatus.ReplacementOrderCreated || status == StartListItemStatus.RequisitionCreated || status == StartListItemStatus.OrderGroupCreated) ? "gray-border-left" 
                : "yellow-border-left";
        }

        public static string GetClassNamesForStatisticsChangeType(StatisticsChangeType changeType)
        {
            return changeType == StatisticsChangeType.Increasing ? "color-green glyphicon glyphicon-arrow-up" : changeType == StatisticsChangeType.Decreasing ? "color-red glyphicon glyphicon-arrow-down" : string.Empty;
        }
    }
}
