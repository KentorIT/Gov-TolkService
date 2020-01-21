using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;

namespace Tolk.BusinessLogic.Tests.TestHelpers
{
    public class StubNotificationService : INotificationService
    {
        public void ComplaintConfirmed(Complaint complaint) { }

        public void ComplaintCreated(Complaint complaint) { }

        public void ComplaintDisputed(Complaint complaint) { }

        public void ComplaintDisputePendingTrial(Complaint complaint) { }

        public void ComplaintTerminatedAsDisputeAccepted(Complaint complaint) { }

        public void CreateEmail(string recipient, string subject, string plainBody, string htmlBody = null, bool isBrokerMail = false, bool addContractInfo = true) { }

        public void CreateReplacingEmail(string recipient, string subject, string plainBody, string htmlBody, int replacingEmailId, int resentByUserId) { }

        public void CustomerCreated(CustomerOrganisation customer) { }

        public void NotifyOnFailure(int callId) { }

        public void OrderCancelledByCustomer(Request request, bool createFullCompensationRequisition) { }

        public void OrderContactPersonChanged(Order order, AspNetUser user) { }

        public void OrderGroupNoBrokerAccepted(OrderGroup terminatedOrderGroup) { }

        public void OrderNoBrokerAccepted(Order order) { }

        public void OrderReplacementCreated(Order order) { }

        public void OrderUpdated(Order order, bool attachmentChanged, bool orderFieldsUpdated) { }

        public void PartialRequestGroupAnswerAccepted(RequestGroup requestGroup) { }

        public void PartialRequestGroupAnswerAutomaticallyApproved(RequestGroup requestGroup) { }

        public void RemindUnhandledRequest(Request request) { }

        public void RequestAccepted(Request request) { }

        public void RequestAnswerApproved(Request request) { }

        public void RequestAnswerAutomaticallyApproved(Request request) { }

        public void RequestAnswerDenied(Request request) { }

        public void RequestCancelledByBroker(Request request) { }

        public void RequestChangedInterpreter(Request request) { }

        public void RequestChangedInterpreterAccepted(Request request, InterpereterChangeAcceptOrigin changeOrigin = InterpereterChangeAcceptOrigin.User) { }

        public void RequestCreated(Request request) { }

        public void RequestCreatedWithoutExpiry(Request request) { }

        public void RequestDeclinedByBroker(Request request) { }

        public void RequestExpiredDueToInactivity(Request request) { }
        public void RequestExpiredDueToNoAnswerFromCustomer(Request request) { }

        public void RequestGroupAccepted(RequestGroup requestGroup) { }

        public void RequestGroupAnswerApproved(RequestGroup requestGroup) { }

        public void RequestGroupAnswerAutomaticallyApproved(RequestGroup requestGroup) { }

        public void RequestGroupAnswerDenied(RequestGroup requestGroup) { }

        public void RequestGroupCreated(RequestGroup requestGroup) { }

        public void RequestGroupCreatedWithoutExpiry(RequestGroup newRequestGroup) { }

        public void RequestGroupDeclinedByBroker(RequestGroup requestGroup) { }

        public void RequestGroupExpiredDueToInactivity(RequestGroup expiredRequestGroup) { }

        public void RequestGroupExpiredDueToNoAnswerFromCustomer(RequestGroup expiredRequestGroup) { }

        public void RequestReplamentOrderAccepted(Request request) { }

        public void RequestReplamentOrderDeclinedByBroker(Request request) { }

        public void RequisitionCommented(Requisition requisition) { }

        public void RequisitionCreated(Requisition requisition) { }

        public void RequisitionReviewed(Requisition requisition) { }

        public bool ResendWebHook(OutboundWebHookCall failedCall, int? resentUserId = null, int? resentImpersonatorUserId = null) { return true; }
    }
}
