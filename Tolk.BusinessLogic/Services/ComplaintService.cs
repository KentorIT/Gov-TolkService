using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ComplaintService> _logger;

        public ComplaintService(
            TolkDbContext dbContext,
            ISwedishClock clock,
            NotificationService notificationService,
            ILogger<ComplaintService> logger
            )
        {
            _dbContext = dbContext;
            _clock = clock;
            _notificationService = notificationService;
            _logger = logger;
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
            _logger.LogDebug($"Created complaint {complaint.ComplaintId} for request {request.RequestId}");
            _notificationService.ComplaintCreated(complaint);
            return complaint;
        }

        public void Accept(Complaint complaint, int userId, int? impersonatorId, string message)
        {
            complaint.Answer(_clock.SwedenNow, userId, impersonatorId, message, ComplaintStatus.Confirmed);
            _dbContext.SaveChanges();
            _logger.LogDebug($"Accepted complaint {complaint.ComplaintId}");
        }

        public void Dispute(Complaint complaint, int userId, int? impersonatorId, string message)
        {
            complaint.Answer(_clock.SwedenNow, userId, impersonatorId, message, ComplaintStatus.Disputed);
            _dbContext.SaveChanges();
            _logger.LogDebug($"Disputed complaint {complaint.ComplaintId}");
            _notificationService.ComplaintDisputed(complaint);
        }

        public void AcceptDispute(Complaint complaint, int userId, int? impersonatorId, string message)
        {
            complaint.AnswerDispute(_clock.SwedenNow, userId, impersonatorId, message, ComplaintStatus.TerminatedAsDisputeAccepted);
            _dbContext.SaveChanges();
            _logger.LogDebug($"Accepted dispute on complaint {complaint.ComplaintId}");
            _notificationService.ComplaintTerminatedAsDisputeAccepted(complaint);
        }

        public void Refute(Complaint complaint, int userId, int? impersonatorId, string message)
        {
            complaint.AnswerDispute(_clock.SwedenNow, userId, impersonatorId, message, ComplaintStatus.DisputePendingTrial);
            _dbContext.SaveChanges();
            _logger.LogDebug($"Refuted complaint {complaint.ComplaintId}, pending trial");
            _notificationService.ComplaintDisputePendingTrial(complaint);
        }
    }
}
