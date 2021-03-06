﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;


namespace Tolk.BusinessLogic.Services
{
    public class ComplaintService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ComplaintService> _logger;

        public ComplaintService(
            TolkDbContext dbContext,
            ISwedishClock clock,
            INotificationService notificationService,
            ILogger<ComplaintService> logger
            )
        {
            _dbContext = dbContext;
            _clock = clock;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Create(Request request, int userId, int? impersonatorId, string message, ComplaintType type)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(Create), nameof(ComplaintService));
            request.Complaints = await _dbContext.Complaints.GetComplaintsForRequest(request.RequestId).ToListAsync();
            var complaint = new Complaint
            {
                Request = request,
                ComplaintType = type,
                ComplaintMessage = message,
                Status = ComplaintStatus.Created,
                CreatedAt = _clock.SwedenNow,
                CreatedBy = userId,
                ImpersonatingCreatedBy = impersonatorId
            };
            request.CreateComplaint(complaint, _clock.SwedenNow);
            _notificationService.ComplaintCreated(complaint);
            _logger.LogDebug($"Created complaint for request {request.RequestId}");
        }

        public void Accept(Complaint complaint, int userId, int? impersonatorId, string message = null)
        {
            NullCheckHelper.ArgumentCheckNull(complaint, nameof(Accept), nameof(ComplaintService));
            complaint.Answer(_clock.SwedenNow, userId, impersonatorId, message, ComplaintStatus.Confirmed);
            _logger.LogDebug($"Accepted complaint {complaint.ComplaintId}");
            _notificationService.ComplaintConfirmed(complaint);
        }

        public void Dispute(Complaint complaint, int userId, int? impersonatorId, string message)
        {
            NullCheckHelper.ArgumentCheckNull(complaint, nameof(Dispute), nameof(ComplaintService));
            complaint.Answer(_clock.SwedenNow, userId, impersonatorId, message, ComplaintStatus.Disputed);
            _logger.LogDebug($"Disputed complaint {complaint.ComplaintId}");
            _notificationService.ComplaintDisputed(complaint);
        }

        public void AcceptDispute(Complaint complaint, int userId, int? impersonatorId, string message)
        {
            NullCheckHelper.ArgumentCheckNull(complaint, nameof(AcceptDispute), nameof(ComplaintService));
            complaint.AnswerDispute(_clock.SwedenNow, userId, impersonatorId, message, ComplaintStatus.TerminatedAsDisputeAccepted);
            _logger.LogDebug($"Accepted dispute on complaint {complaint.ComplaintId}");
            _notificationService.ComplaintTerminatedAsDisputeAccepted(complaint);
        }

        public void Refute(Complaint complaint, int userId, int? impersonatorId, string message)
        {
            NullCheckHelper.ArgumentCheckNull(complaint, nameof(Refute), nameof(ComplaintService));
            complaint.AnswerDispute(_clock.SwedenNow, userId, impersonatorId, message, ComplaintStatus.DisputePendingTrial);
            _logger.LogDebug($"Refuted complaint {complaint.ComplaintId}, pending trial");
            _notificationService.ComplaintDisputePendingTrial(complaint);
        }
    }
}
