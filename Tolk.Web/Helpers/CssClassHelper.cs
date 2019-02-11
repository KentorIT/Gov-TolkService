using System;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Helpers
{
    public class CssClassHelper
    {
        public static string GetColorClassNameForOrderStatus(OrderStatus status)
        {
            return (status == OrderStatus.NoBrokerAcceptedOrder || status == OrderStatus.CancelledByCreator || status == OrderStatus.CancelledByBroker || status == OrderStatus.ResponseNotAnsweredByCreator) ? "red-border-left" :
            (status == OrderStatus.Delivered || status == OrderStatus.DeliveryAccepted || status == OrderStatus.ResponseAccepted) ? "green-border-left" :
            (status == OrderStatus.RequestResponded || status == OrderStatus.RequestRespondedNewInterpreter) ? "yellow-border-left" : "blue-border-left";
        }

        internal static string GetColorClassNameForSystemMessageType(SystemMessageType systemMessageType)
        {
            return systemMessageType == SystemMessageType.Information ? "blue-border-left" : "yellow-border-left";
        }

        public static string GetColorClassNameForRequestStatus(RequestStatus status)
        {
            return (status == RequestStatus.CancelledByBroker || status == RequestStatus.CancelledByCreator || status == RequestStatus.CancelledByCreatorWhenApproved || status == RequestStatus.DeniedByCreator || status == RequestStatus.DeniedByTimeLimit || status == RequestStatus.ResponseNotAnsweredByCreator || status == RequestStatus.DeclinedByBroker) ? "red-border-left" :
            (status == RequestStatus.Approved) ? "green-border-left" : (status == RequestStatus.Accepted || status == RequestStatus.AcceptedNewInterpreterAppointed) ? "yellow-border-left" : "blue-border-left";
        }

        public static string GetColorClassNameForRequisitionStatus(RequisitionStatus status)
        {
            return (status == RequisitionStatus.DeniedByCustomer) ? "red-border-left" :
            (status == RequisitionStatus.Approved || status == RequisitionStatus.AutomaticApprovalFromCancelledOrder) ? "green-border-left" : "blue-border-left";
        }

        public static string GetColorClassNameForComplaintStatus(ComplaintStatus status)
        {
            return (status == ComplaintStatus.Disputed || status == ComplaintStatus.DisputePendingTrial) ? "red-border-left" :
            (status == ComplaintStatus.Confirmed || status == ComplaintStatus.TerminatedAsDisputeAccepted) ? "green-border-left" : "blue-border-left";
        }

        public static string GetColorClassNameForUserStatus(bool isActive)
        {
            return isActive ? "green-border-left" : "gray-border-left";
        }

        public static string GetColorClassNameForStartListItem(StartListItemStatus status)
        {
            return (status == StartListItemStatus.ComplaintEvent || status == StartListItemStatus.RequestArrived || status == StartListItemStatus.RequestReceived || status == StartListItemStatus.RequisitonArrived || status == StartListItemStatus.ReplacementOrderRequestReceived || status == StartListItemStatus.ReplacementOrderRequestArrived) ? "blue-border-left" :
            (status == StartListItemStatus.RequisitionDenied || status == StartListItemStatus.OrderCancelled || status == StartListItemStatus.OrderNotAnswered || status == StartListItemStatus.RequestDenied || status == StartListItemStatus.ReplacementOrderNotAnswered) ? "red-border-left" :
            (status == StartListItemStatus.OrderApproved || status == StartListItemStatus.RequisitionToBeCreated) ? "green-border-left" :
            (status == StartListItemStatus.RequisitionAwaited || status == StartListItemStatus.OrderCreated || status == StartListItemStatus.RequisitionCreated) ? "gray-border-left" : "yellow-border-left";
        }
    }
}
