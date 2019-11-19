using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Services
{
    public interface INotificationService
    {
        void ComplaintCreated(Complaint complaint);
        void ComplaintConfirmed(Complaint complaint);
        void ComplaintDisputed(Complaint complaint);
        void ComplaintDisputePendingTrial(Complaint complaint);
        void ComplaintTerminatedAsDisputeAccepted(Complaint complaint);
        void CreateEmail(string recipient, string subject, string plainBody, string htmlBody = null, bool isBrokerMail = false, bool addContractInfo = true);
        void CreateReplacingEmail(string recipient, string subject, string plainBody, string htmlBody, int replacingEmailId, int resentByUserId);
        void CustomerCreated(CustomerOrganisation customer);
        void FlushNotificationSettings();
        void OrderCancelledByCustomer(Request request, bool createFullCompensationRequisition);
        void OrderContactPersonChanged(Order order);
        void OrderNoBrokerAccepted(Order order);
        void OrderReplacementCreated(Order order);
        void PartialRequestGroupAnswerAccepted(RequestGroup requestGroup);
        void PartialRequestGroupAnswerAutomaticallyApproved(RequestGroup requestGroup);
        void RemindUnhandledRequest(Request request);
        void RequestAccepted(Request request);
        void RequestAnswerApproved(Request request);
        void RequestAnswerAutomaticallyApproved(Request request);
        void RequestAnswerDenied(Request request);
        void RequestCancelledByBroker(Request request);
        void RequestChangedInterpreter(Request request);
        void RequestChangedInterpreterAccepted(Request request, InterpereterChangeAcceptOrigin changeOrigin = InterpereterChangeAcceptOrigin.User);
        void RequestCreated(Request request);
        void RequestCreatedWithoutExpiry(Request request);
        void RequestDeclinedByBroker(Request request);
        void RequestGroupAccepted(RequestGroup requestGroup);
        void RequestGroupAnswerAutomaticallyApproved(RequestGroup requestGroup);
        void RequestGroupAnswerApproved(RequestGroup requestGroup);
        void RequestGroupDeclinedByBroker(RequestGroup requestGroup);
        void RequestGroupAnswerDenied(RequestGroup requestGroup);
        void RequestExpired(Request request);
        void RequestReplamentOrderAccepted(Request request);
        void RequestReplamentOrderDeclinedByBroker(Request request);
        void RequisitionCommented(Requisition requisition);
        void RequisitionCreated(Requisition requisition);
        void RequisitionReviewed(Requisition requisition);
        bool ResendWebHook(OutboundWebHookCall failedCall, int? resentUserId = null, int? resentImpersonatorUserId = null);
        void RequestGroupCreated(RequestGroup requestGroup);
        void RequestGroupCreatedWithoutExpiry(RequestGroup newRequestGroup);
        void OrderGroupNoBrokerAccepted(OrderGroup terminatedOrderGroup);
        void RequestGroupExpired(RequestGroup expiredRequestGroup);
        void NotifyOnFailure(int callId);
    }
}