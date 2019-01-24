using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Services
{
    public class ComplaintService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly NotificationService _notificationService;

        public ComplaintService(
            TolkDbContext dbContext,
            ISwedishClock clock,
            NotificationService notificationService
            )
        {
            _dbContext = dbContext;
            _clock = clock;
            _notificationService = notificationService;
        }

        public Complaint Create(Request request, int userId, int? impersonatorId, string message, ComplaintType type)
        {
            var complaint = new Complaint
            {
                RequestId = request.RequestId,
                ComplaintType = type,
                ComplaintMessage = message,
                Status = ComplaintStatus.Created,
                CreatedAt = _clock.SwedenNow,
                CreatedBy = userId,
                ImpersonatingCreatedBy = impersonatorId
            };
            request.CreateComplaint(complaint);
            _dbContext.SaveChanges();
            _notificationService.ComplaintCreated(complaint);
            return complaint;
        }

        public void Accept(Complaint complaint, int userId, int? impersonatorId, string message)
        {
            complaint.Answer(_clock.SwedenNow, userId, impersonatorId, message, ComplaintStatus.Confirmed);
            _dbContext.SaveChanges();
        }

        public void Dispute(Complaint complaint, int userId, int? impersonatorId, string message)
        {
            complaint.Answer(_clock.SwedenNow, userId, impersonatorId, message, ComplaintStatus.Disputed);
            _dbContext.SaveChanges();
            _notificationService.ComplaintDisputed(complaint);
        }

        public void AcceptDispute(Complaint complaint, int userId, int? impersonatorId, string message)
        {
            complaint.AnswerDispute(_clock.SwedenNow, userId, impersonatorId, message, ComplaintStatus.TerminatedAsDisputeAccepted);
            _dbContext.SaveChanges();
            _notificationService.ComplaintTerminatedAsDisputeAccepted(complaint);
        }

        public void Refute(Complaint complaint, int userId, int? impersonatorId, string message)
        {
            complaint.AnswerDispute(_clock.SwedenNow, userId, impersonatorId, message, ComplaintStatus.DisputePendingTrial);
            _dbContext.SaveChanges();
            _notificationService.ComplaintDisputePendingTrial(complaint);
        }
    }
}
