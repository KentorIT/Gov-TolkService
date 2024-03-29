﻿using System.Threading.Tasks;
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

        public void CreateEmail(string recipient, string subject, string plainBody, string htmlBody, NotificationType notificationType, string frameworkAgreementNumber = null, bool isBrokerMail = false, bool addContractInfo = true) { }

        public void CreateReplacingEmail(string recipient, string subject, string plainBody, string htmlBody, NotificationType notificationType, int replacingEmailId, int resentByUserId) { }

        public void CustomerCreated(CustomerOrganisation customer) { }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task NotifyOnFailedWebHook(int callId) { }

        public async Task NotifyOnFailedPeppolMessage(int messageId) { }

        public async Task OrderReplacementCreated(int replacedRequestId, int newRequestId) { }

        public async Task RequestNeedsFullAnswerCreated(Request request) { }

        public async Task RequestNeedsAcceptanceCreated(Request request) { }

        public async Task FlexibleRequestCreated(Request request) { }

        public async Task RequestGroupCreated(RequestGroup requestGroup) { }

        public async Task RequisitionReviewed(Requisition requisition) { }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        public void OrderCancelledByCustomer(Request request, bool createFullCompensationRequisition) { }

        public void OrderContactPersonChanged(Order order, AspNetUser user) { }

        public void OrderGroupTerminated(OrderGroup terminatedOrderGroup) { }

        public void OrderGroupCancelledByCustomer(RequestGroup requestGroup) { }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task OrderTerminated(Order order) {  }

        public void OrderUpdated(Order order, bool attachmentChanged, bool orderFieldsUpdated) { }

        public void PartialRequestGroupAnswerAccepted(RequestGroup requestGroup) { }

        public void PartialRequestGroupAnswerAutomaticallyApproved(RequestGroup requestGroup) { }

        public void RemindUnhandledRequest(Request request) { }

        public void RemindUnhandledRequestGroup(RequestGroup requestGrpoup) { }

        public void RequestAnsweredAwaitingApproval(Request request) { }

        public void RequestAnswerApproved(Request request) { }

        public void RequestAnswerAutomaticallyApproved(Request request) { }

        public void RequestAnswerDenied(Request request) { }

        public void RequestCancelledByBroker(Request request) { }

        public void RequestChangedInterpreter(Request request) { }

        public void RequestChangedInterpreterAccepted(Request request, InterpereterChangeAcceptOrigin changeOrigin = InterpereterChangeAcceptOrigin.User) { }

        public void RequestCompleted(Request request) { }

        public void RequestCreatedWithoutExpiry(Request request) { }

        public void RequestDeclinedByBroker(Request request) { }

        public void RequestExpiredDueToInactivity(Request request) { }
        
        public void RequestExpiredDueToNotFullyAnswered(Request request) { }

        public void RequestExpiredDueToNoAnswerFromCustomer(Request request) { }

        public void RequestGroupAnsweredAwaitingApproval(RequestGroup requestGroup) { }

        public void RequestGroupAnswerApproved(RequestGroup requestGroup) { }

        public void RequestGroupAnswerAutomaticallyApproved(RequestGroup requestGroup) { }

        public void RequestGroupAnswerDenied(RequestGroup requestGroup) { }

        public void RequestGroupCreatedWithoutExpiry(RequestGroup newRequestGroup) { }

        public void RequestGroupDeclinedByBroker(RequestGroup requestGroup) { }

        public void RequestGroupExpiredDueToInactivity(RequestGroup expiredRequestGroup) { }

        public void RequestGroupExpiredDueToNotFullyAnswered(RequestGroup expiredRequestGroup) { }

        public void RequestGroupExpiredDueToNoAnswerFromCustomer(RequestGroup expiredRequestGroup) { }

        public void RequestReplamentOrderAccepted(Request request) { }

        public void RequestReplamentOrderDeclinedByBroker(Request request) { }

        public void RequisitionCommented(Requisition requisition) { }

        public void RequisitionCreated(Requisition requisition) { }

        public bool ResendWebHook(OutboundWebHookCall failedCall, int? resentUserId = null, int? resentImpersonatorUserId = null) { return true; }

        public bool ResendPeppolMessage(OutboundPeppolMessage failedMessage, int? resentUserId = null, int? resentImpersonatorUserId = null) { return true; }

        public void RequestTerminatedDueToTerminatedFrameworkAgreement(Request request) { }

        public void RequestGroupTerminatedDueToTerminatedFrameworkAgreement(RequestGroup requestGroup) { }

        public void RequestAccepted(Request request) { }

        public void RequestGroupAccepted(RequestGroup requestGroup) { }
        public void ExpiresAtChanged(Request request) { }
    }
}
