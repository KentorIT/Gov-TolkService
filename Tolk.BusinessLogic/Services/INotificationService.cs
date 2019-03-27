using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Services
{
    public interface INotificationService
    {
        void ComplaintCreated(Complaint complaint);
        void ComplaintDisputed(Complaint complaint);
        void ComplaintDisputePendingTrial(Complaint complaint);
        void ComplaintTerminatedAsDisputeAccepted(Complaint complaint);
        void CreateEmail(string recipient, string subject, string plainBody, bool isBrokerMail = false);
        void CreateEmail(string recipient, string subject, string plainBody, string htmlBody, bool isBrokerMail = false);
        void OrderCancelledByCustomer(Request request, bool createFullCompensationRequisition);
        void OrderContactPersonChanged(Order order);
        void OrderNoBrokerAccepted(Order order);
        void OrderReplacementCreated(Order order);
        void RemindUnhandledRequest(Request request);
        void RequestAccepted(Request request);
        void RequestAnswerApproved(Request request);
        void RequestAnswerAutomaticallyAccepted(Request request);
        void RequestAnswerDenied(Request request);
        void RequestCancelledByBroker(Request request);
        void RequestChangedInterpreter(Request request);
        void RequestChangedInterpreterAccepted(Request request, InterpereterChangeAcceptOrigin changeOrigin = InterpereterChangeAcceptOrigin.User);
        void RequestCreated(Request request);
        void RequestCreatedWithoutExpiry(Request request);
        void RequestDeclinedByBroker(Request request);
        void RequestExpired(Request request);
        void RequestReplamentOrderAccepted(Request request);
        void RequestReplamentOrderDeclinedByBroker(Request request);
        void RequisitionCommented(Requisition requisition);
        void RequisitionCreated(Requisition requisition);
        void RequisitionReviewed(Requisition requisition);
    }
}