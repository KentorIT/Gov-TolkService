using System;
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

        public void CreateReplacingEmail(string recipient, string subject, string plainBody, string htmlBody, int replacingEmailId) { }

        public void FlushNotificationSettings() { }

        public void NotifyOnFailure(int callId) { }

        public void OrderCancelledByCustomer(Request request, bool createFullCompensationRequisition) { }

        public void OrderContactPersonChanged(Order order) { }

        public void OrderGroupNoBrokerAccepted(OrderGroup terminatedOrderGroup) { }

        public void OrderNoBrokerAccepted(Order order) { }

        public void OrderReplacementCreated(Order order) { }

        public void RemindUnhandledRequest(Request request) { }

        public void RequestAccepted(Request request) { }

        public void RequestAnswerApproved(Request request) { }

        public void RequestAnswerAutomaticallyAccepted(Request request) { }

        public void RequestAnswerDenied(Request request) { }

        public void RequestCancelledByBroker(Request request) { }

        public void RequestChangedInterpreter(Request request) { }

        public void RequestChangedInterpreterAccepted(Request request, InterpereterChangeAcceptOrigin changeOrigin = InterpereterChangeAcceptOrigin.User) { }

        public void RequestCreated(Request request) { }

        public void RequestCreatedWithoutExpiry(Request request) { }

        public void RequestDeclinedByBroker(Request request) { }

        public void RequestExpired(Request request) { }

        public void RequestGroupCreated(RequestGroup requestGroup) { }

        public void RequestGroupCreatedWithoutExpiry(RequestGroup newRequestGroup) { }

        public void RequestGroupExpired(RequestGroup expiredRequestGroup) { }

        public void RequestReplamentOrderAccepted(Request request) { }

        public void RequestReplamentOrderDeclinedByBroker(Request request) { }

        public void RequisitionCommented(Requisition requisition) { }

        public void RequisitionCreated(Requisition requisition) { }

        public void RequisitionReviewed(Requisition requisition) { }

        public bool ResendWebHook(OutboundWebHookCall failedCall) { return true; }
    }
}
