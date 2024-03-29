﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class RequisitionService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly INotificationService _notificationService;
        private readonly ILogger<RequisitionService> _logger;
        private readonly PriceCalculationService _priceCalculationService;

        public RequisitionService(
            TolkDbContext dbContext,
            ISwedishClock clock,
            INotificationService notificationService,
            ILogger<RequisitionService> logger,
            PriceCalculationService priceCalculationService
            )
        {
            _dbContext = dbContext;
            _clock = clock;
            _notificationService = notificationService;
            _logger = logger;
            _priceCalculationService = priceCalculationService;
        }

        public async Task<Requisition> Create(Request request, int userId, int? impersonatorId, string message, decimal? outlay,
            DateTimeOffset sessionStartedAt, DateTimeOffset sessionEndedAt, int? timeWasteNormalTime, int? timeWasteIWHTime, TaxCardType interpreterTaxCard,
            List<RequisitionAttachment> attachments, Guid fileGroupKey, List<MealBreak> mealbreaks, int? carCompensation, string perDiem)
        {
            NullCheckHelper.ArgumentCheckNull(request, nameof(Create), nameof(RequisitionService));
            request.Requisitions = await _dbContext.Requisitions.GetRequisitionsForRequest(request.RequestId).ToListAsync();
            if (!request.CanCreateRequisition)
            {
                throw new InvalidOperationException($"Cannot create requisition on order {request.OrderId}");
            }
            request.PriceRows = await _dbContext.RequestPriceRows.GetPriceRowsForRequest(request.RequestId).ToListAsync();
            TimeSpan sessionDuration = sessionEndedAt - sessionStartedAt;
            var priceInformation = _priceCalculationService.GetPricesRequisition(
                sessionStartedAt,
                sessionDuration,
                request.CalculatedStartAt,
                request.Order.Duration,
                EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>((CompetenceAndSpecialistLevel)request.CompetenceLevel),
                request.Order.CustomerOrganisation.PriceListType,
                out bool useRequestRows,
                timeWasteNormalTime,
                timeWasteIWHTime,
                request.PriceRows.OfType<PriceRowBase>(),
                outlay,
                request.Order.ReplacingOrderId.HasValue ? request.Order.ReplacingOrder : null,
                request.Order.CreatedAt,
                mealbreaks
            );

            using var transaction = _dbContext.Database.BeginTransaction();
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
                InterpretersTaxCard = interpreterTaxCard,
                PriceRows = priceInformation.PriceRows.Select(row => DerivedClassConstructor.Construct<PriceRowBase, RequisitionPriceRow>(row)).ToList(),
                Attachments = attachments,
                MealBreaks = mealbreaks,
                CarCompensation = carCompensation,
                PerDiem = perDiem,
                RequestOrReplacingOrderPeriodUsed = useRequestRows,
            };

            foreach (var tag in _dbContext.TemporaryAttachmentGroups.Where(t => t.TemporaryAttachmentGroupKey == fileGroupKey))
            {
                _dbContext.TemporaryAttachmentGroups.Remove(tag);
            }
            request.CreateRequisition(requisition);
            await _dbContext.SaveChangesAsync();
            var replacingRequisition = request.Requisitions.SingleOrDefault(r => r.Status == RequisitionStatus.Commented &&
                !r.ReplacedByRequisitionId.HasValue);
            if (replacingRequisition != null)
            {
                replacingRequisition.ReplacedByRequisitionId = requisition.RequisitionId;
                await _dbContext.SaveChangesAsync();
            }
            transaction.Commit();
            _logger.LogDebug($"Created requisition {requisition.RequisitionId} for request {request.RequestId}");
            _notificationService.RequisitionCreated(requisition);
            return requisition;
        }

        public async Task Review(Requisition requisition, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(requisition, nameof(Review), nameof(RequisitionService));
            requisition.Review(_clock.SwedenNow, userId, impersonatorId);
            await _dbContext.SaveChangesAsync();
            _logger.LogDebug($"Requisition reviewed {requisition.RequisitionId}");
            await _notificationService.RequisitionReviewed(requisition);
        }

        public async Task ConfirmNoReview(Requisition requisition, int userId, int? impersonatorId)
        {
            NullCheckHelper.ArgumentCheckNull(requisition, nameof(ConfirmNoReview), nameof(RequisitionService));
            requisition.RequisitionStatusConfirmations = await _dbContext.RequisitionStatusConfirmations.GetRequisitionStatusConfirmationsForRequisition(requisition.RequisitionId).ToListAsync();
            requisition.CofirmNoReview(_clock.SwedenNow, userId, impersonatorId);
            await _dbContext.SaveChangesAsync();
            _logger.LogDebug($"Requisition confirmed with no review {requisition.RequisitionId}");
        }

        public async Task Comment(Requisition requisition, int userId, int? impersonatorId, string message)
        {
            NullCheckHelper.ArgumentCheckNull(requisition, nameof(Comment), nameof(RequisitionService));
            requisition.Comment(_clock.SwedenNow, userId, impersonatorId, message);
            await _dbContext.SaveChangesAsync();
            _logger.LogDebug($"Requisition commented {requisition.RequisitionId}");
            _notificationService.RequisitionCommented(requisition);
        }
    }
}
