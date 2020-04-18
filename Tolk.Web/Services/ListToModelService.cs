using Microsoft.AspNetCore.Html;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Models;
using System.Collections.Generic;
using Tolk.Web.Enums;

namespace Tolk.Web.Services
{
    public class ListToModelService
    {
        private readonly TolkDbContext _dbContext;

        public ListToModelService(TolkDbContext dbContext)
        {
            _dbContext = dbContext;
        }

#warning man borde skicka med vilken roll man har, för alla delar av rättighetskollarna skall bara göras per roll, det finns ju sys och app-admins också, och de får ju inte göra någonting...
        internal async Task<OrderViewModel> AddInformationFromListsToModel(OrderViewModel model)
        {
            int id = model.OrderId.Value;
            //LISTS
            var orderStatusConfirmations = await _dbContext.OrderStatusConfirmation.GetStatusConfirmationsForOrder(id).ToListAsync();
            model.HasNoBrokerAcceptedConfirmation = orderStatusConfirmations.Any(os => os.OrderStatus == OrderStatus.NoBrokerAcceptedOrder);
            model.HasResponseNotAnsweredByCreatorConfirmation = orderStatusConfirmations.Any(os => os.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator);

            //Locations
            var interpreterLocations = await _dbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(id).ToListAsync();

            model.RankedInterpreterLocationFirst = interpreterLocations.Single(l => l.Rank == 1)?.InterpreterLocation;
            model.RankedInterpreterLocationSecond = interpreterLocations.SingleOrDefault(l => l.Rank == 2)?.InterpreterLocation;
            model.RankedInterpreterLocationThird = interpreterLocations.SingleOrDefault(l => l.Rank == 3)?.InterpreterLocation;
            model.RankedInterpreterLocationFirstAddressModel = OrderBaseModel.GetInterpreterLocation(interpreterLocations.Single(l => l.Rank == 1));
            model.RankedInterpreterLocationSecondAddressModel = OrderBaseModel.GetInterpreterLocation(interpreterLocations.SingleOrDefault(l => l.Rank == 2));
            model.RankedInterpreterLocationThirdAddressModel = OrderBaseModel.GetInterpreterLocation(interpreterLocations.SingleOrDefault(l => l.Rank == 3));

            //Competences
            List<CompetenceAndSpecialistLevel> competenceRequirements = await _dbContext.OrderCompetenceRequirements
                .GetOrderedCompetenceRequirementsForOrder(id)
                .Select(r => r.CompetenceLevel)
                .ToListAsync();

            model.RequestedCompetenceLevelFirst = competenceRequirements.FirstOrDefault();
            model.RequestedCompetenceLevelSecond = competenceRequirements.Count > 1 ? (CompetenceAndSpecialistLevel?)competenceRequirements[1] : null;

            model.AttachmentListModel = await AttachmentListModel.GetReadOnlyModelFromList(_dbContext.Attachments.GetAttachmentsForOrderAndGroup(id, model.OrderGroupId), "Bifogade filer från myndighet");
            model.PreviousRequests = await BrokerListModel.GetFromList(_dbContext.Requests.GetLostRequestsForOrder(id));
            model.OrderCalculatedPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplay(await _dbContext.OrderPriceRows.GetPriceRowsForOrder(id).ToListAsync(), PriceInformationType.Order, model.MealbreakIncluded);

            model.OrderRequirements = await OrderRequirementModel.GetFromList(_dbContext.OrderRequirements.GetRequirementsForOrder(id));
            model.Dialect = model.OrderRequirements.SingleOrDefault(r => r.RequirementType == RequirementType.Dialect)?.RequirementDescription;
            if (model.RequestId.HasValue)
            {
                var requestStatusConfirmations = await _dbContext.RequestStatusConfirmation.GetStatusConfirmationsForRequest(model.RequestId.Value).ToListAsync();
                model.HasCancelledByBrokerConfirmation = requestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.CancelledByBroker);
                model.HasNoRequisitionConfirmation = requestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.Approved);
                model.HasDeniedByCreatorConfirmation = requestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.DeniedByCreator);
                model.HasResponseNotAnsweredByCreatorBrokerConfirmation = requestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.ResponseNotAnsweredByCreator);
                model.HasCancelledByCreatorWhenApprovedConfirmation = requestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.CancelledByCreatorWhenApproved);

                model.ActiveRequestPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplay(await _dbContext.RequestPriceRows.GetPriceRowsForRequest(model.RequestId.Value).ToListAsync(), PriceInformationType.Request, model.MealbreakIncluded);
                model.RequestAttachmentListModel = await AttachmentListModel.GetReadOnlyModelFromList(_dbContext.Attachments.GetAttachmentsForRequest(model.RequestId.Value, model.RequestGroupId), "Bifogade filer från förmedling");
                var requestChecks = await _dbContext.Requests
                    .Where(r => r.RequestId == model.RequestId.Value)
                    .Select(r => new
                    {
                        LatestComplaintId = r.Complaints.Max(c => (int?)c.ComplaintId),
                        LatestRequisitionId = r.Requisitions.Max(req => (int?)req.RequisitionId),
                        HasActiveRequests = r.Requisitions.Any(req => req.Status == RequisitionStatus.Reviewed || req.Status == RequisitionStatus.Created),
                        HasComplaints = r.Complaints.Any(),
                    }).SingleAsync();
                model.HasComplaints = requestChecks.HasComplaints;
                model.ComplaintId = requestChecks.LatestComplaintId;
                model.RequisitionId = requestChecks.LatestRequisitionId;
                model.HasActiveRequests = requestChecks.HasActiveRequests;
                model.ActiveRequest.RequirementAnswers = await RequestRequirementAnswerModel.GetFromList(_dbContext.OrderRequirementRequestAnswer.GetRequirementAnswersForRequest(model.RequestId.Value));
                model.ConfirmedOrderChangeLogEntries = await _dbContext.OrderChangeLogEntries.GetOrderChangeLogEntiesForOrder(id).Where(oc => oc.BrokerId == model.ActiveRequest.BrokerId && oc.OrderChangeLogType != OrderChangeLogType.ContactPerson && oc.OrderChangeConfirmation == null)
                    .Select(oc => oc.OrderChangeLogEntryId).ToListAsync();
            }
            return model;
        }

        internal async Task<string> GetOtherViewer(int requestId, int currentUserId)
        {
            var otherViewer = await _dbContext.RequestViews.GetActiveViewsForRequest(requestId).FirstOrDefaultAsync(rv => rv.ViewedBy != currentUserId);
            return otherViewer != null ?
                $"{otherViewer.ViewedByUser.FullName} håller också på med denna förfrågan" :
                string.Empty;
        }
    }
}

