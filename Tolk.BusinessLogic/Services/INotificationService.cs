using System.Threading.Tasks;
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
        void CreateEmail(string recipient, string subject, string plainBody, string htmlBody, NotificationType notificationType, bool isBrokerMail = false, bool addContractInfo = true);
        void CreateReplacingEmail(string recipient, string subject, string plainBody, string htmlBody, NotificationType notificationType, int replacingEmailId, int resentByUserId);
        void CustomerCreated(CustomerOrganisation customer);
        void OrderCancelledByCustomer(Request request, bool createFullCompensationRequisition);
        void OrderContactPersonChanged(Order order, AspNetUser previousContactUser);
        void OrderGroupCancelledByCustomer(RequestGroup requestGroup);
        void OrderTerminated(Order order);
        Task OrderReplacementCreated(int replacedRequestId, int newRequestId);
        void PartialRequestGroupAnswerAccepted(RequestGroup requestGroup);
        void PartialRequestGroupAnswerAutomaticallyApproved(RequestGroup requestGroup);
        void RemindUnhandledRequest(Request request);
        void RemindUnhandledRequestGroup(RequestGroup requestGrpoup);
        void RequestCompleted(Request request);
        void RequestAnsweredAwaitingApproval(Request request);
        void RequestAccepted(Request request);
        void RequestAnswerApproved(Request request);
        void RequestAnswerAutomaticallyApproved(Request request);
        void RequestAnswerDenied(Request request);
        void RequestCancelledByBroker(Request request);
        void RequestChangedInterpreter(Request request);
        void RequestChangedInterpreterAccepted(Request request, InterpereterChangeAcceptOrigin changeOrigin = InterpereterChangeAcceptOrigin.User);
        Task RequestCreated(Request request);
        void RequestCreatedWithoutExpiry(Request request);
        void RequestDeclinedByBroker(Request request);
        void RequestGroupAccepted(RequestGroup requestGroup);
        void RequestGroupAnswerAutomaticallyApproved(RequestGroup requestGroup);
        void RequestGroupAnswerApproved(RequestGroup requestGroup);
        void RequestGroupDeclinedByBroker(RequestGroup requestGroup);
        void RequestGroupAnswerDenied(RequestGroup requestGroup);
        void RequestExpiredDueToInactivity(Request request);
        void RequestExpiredDueToNoAnswerFromCustomer(Request request);
        void RequestTerminatedDueToTerminatedFrameworkAgreement(Request request);
        void RequestGroupTerminatedDueToTerminatedFrameworkAgreement(RequestGroup requestGroup);
        void RequestReplamentOrderAccepted(Request request);
        void RequestReplamentOrderDeclinedByBroker(Request request);
        void RequisitionCommented(Requisition requisition);
        void RequisitionCreated(Requisition requisition);
        Task RequisitionReviewed(Requisition requisition);
        bool ResendWebHook(OutboundWebHookCall failedCall, int? resentUserId = null, int? resentImpersonatorUserId = null);
        bool ResendPeppolMessage(OutboundPeppolMessage failedMessage, int? resentUserId = null, int? resentImpersonatorUserId = null);
        Task RequestGroupCreated(RequestGroup requestGroup);
        void RequestGroupCreatedWithoutExpiry(RequestGroup newRequestGroup);
        void OrderGroupTerminated(OrderGroup terminatedOrderGroup);
        void OrderUpdated(Order order, bool attachmentChanged, bool orderFieldsUpdated);
        void RequestGroupExpiredDueToInactivity(RequestGroup expiredRequestGroup);
        void RequestGroupExpiredDueToNoAnswerFromCustomer(RequestGroup expiredRequestGroup);
        Task NotifyOnFailedWebHook(int callId);
        Task NotifyOnFailedPeppolMessage(int messageId);
    }
}