using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class RequisitionService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly NotificationService _notificationService;
        private readonly ILogger<RequisitionService> _logger;

        public RequisitionService(
            TolkDbContext dbContext,
            ISwedishClock clock,
            NotificationService notificationService,
            ILogger<RequisitionService> logger
            )
        {
            _dbContext = dbContext;
            _clock = clock;
            _notificationService = notificationService;
            _logger = logger;
        }

        public Requisition Create(Request request, int userId, int? impersonatorId, string message, PriceInformation priceInformation, bool useRequestRows, 
            DateTimeOffset sessionStartedAt, DateTimeOffset sessionEndedAt, int? timeWasteNormalTime, int? timeWasteIWHTime, TaxCard? interpreterTaxCard, 
            List<RequisitionAttachment> attachments, Guid fileGroupKey, List<MealBreak> mealbreaks)
        {
            using (var transaction = _dbContext.Database.BeginTransaction())
            {
                var requisition = new Requisition
                {
                    Status = RequisitionStatus.Created,
                    CreatedBy = userId,
                    CreatedAt = _clock.SwedenNow,
                    ImpersonatingCreatedBy = impersonatorId,
                    Message = message,
                    SessionStartedAt = sessionStartedAt,
                    SessionEndedAt = sessionEndedAt,
                    TimeWasteNormalTime = timeWasteNormalTime,
                    TimeWasteIWHTime = timeWasteIWHTime,
                    InterpretersTaxCard = interpreterTaxCard.Value,
                    PriceRows = new List<RequisitionPriceRow>(),
                    Attachments = attachments,
                    MealBreaks = mealbreaks
                };

                requisition.RequestOrReplacingOrderPeriodUsed = useRequestRows;
                requisition.PriceRows.AddRange(priceInformation.PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, RequisitionPriceRow>(row)));
                foreach (var tag in _dbContext.TemporaryAttachmentGroups.Where(t => t.TemporaryAttachmentGroupKey == fileGroupKey))
                {
                    _dbContext.TemporaryAttachmentGroups.Remove(tag);
                }
                request.CreateRequisition(requisition);
                _dbContext.SaveChanges();
                var replacingRequisition = request.Requisitions.SingleOrDefault(r => r.Status == RequisitionStatus.Commented &&
                    !r.ReplacedByRequisitionId.HasValue);
                if (replacingRequisition != null)
                {
                    replacingRequisition.ReplacedByRequisitionId = requisition.RequisitionId;
                    _dbContext.SaveChanges();
                }
                transaction.Commit();
                _logger.LogDebug($"Created requisition {requisition.RequisitionId} for request {request.RequestId}");
                _notificationService.RequisitionCreated(requisition);
                return requisition;
            }
        }

        public void Review(Requisition requisition, int userId, int? impersonatorId)
        {
            requisition.Review(_clock.SwedenNow, userId, impersonatorId);
            _dbContext.SaveChanges();
            _logger.LogDebug($"Requisition reviewed {requisition.RequisitionId}");
            _notificationService.RequisitionReviewed(requisition);
        }

        public void Comment(Requisition requisition, int userId, int? impersonatorId, string message)
        {
            requisition.Comment(_clock.SwedenNow, userId, impersonatorId, message);
            _dbContext.SaveChanges();
            _logger.LogDebug($"Requisition commented {requisition.RequisitionId}");
            _notificationService.RequisitionCommented(requisition);
        }
    }
}
