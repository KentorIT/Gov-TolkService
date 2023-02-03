using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Enums;
using Tolk.Web.Models;

namespace Tolk.Web.Services
{
    public class ListToModelService
    {
        private readonly TolkDbContext _dbContext;

        public ListToModelService(TolkDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        internal async Task<OrderViewModel> AddInformationFromListsToModel(OrderViewModel model)
        {
            int id = model.OrderId.Value;
            //Lists
            var orderStatusConfirmations = await _dbContext.OrderStatusConfirmation.GetStatusConfirmationsForOrder(id).ToListAsync();
            model.HasNoBrokerAcceptedConfirmation = orderStatusConfirmations.Any(os => os.OrderStatus == OrderStatus.NoBrokerAcceptedOrder);
            model.HasResponseNotAnsweredByCreatorConfirmation = orderStatusConfirmations.Any(os => os.OrderStatus == OrderStatus.ResponseNotAnsweredByCreator);

            //Locations
            var interpreterLocations = await _dbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(id).ToListAsync();
            await GetOrderBaseLists(model, interpreterLocations, id);
            if (model.UseAttachments)
            {
                model.AttachmentListModel = await AttachmentListModel.GetReadOnlyModelFromList(_dbContext.Attachments.GetAttachmentsForOrderAndGroup(id, model.OrderGroupId), "Bifogade filer från myndighet");
            }
            model.PreviousRequests = await BrokerListModel.GetFromList(_dbContext.Requests.GetLostRequestsForOrder(id));
            model.OrderCalculatedPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplay(await _dbContext.OrderPriceRows.GetPriceRowsForOrder(id).ToListAsync(), PriceInformationType.Order, mealBreakIncluded: model.MealbreakIncluded);

            model.Dialect = model.OrderRequirements.SingleOrDefault(r => r.RequirementType == RequirementType.Dialect)?.RequirementDescription;
            if (model.RequestId.HasValue)
            {
                var requestStatusConfirmations = await _dbContext.RequestStatusConfirmation.GetStatusConfirmationsForRequest(model.RequestId.Value).ToListAsync();
                model.HasCancelledByBrokerConfirmation = requestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.CancelledByBroker);
                model.HasNoRequisitionConfirmation = requestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.Approved);
                model.HasDeniedByCreatorConfirmation = requestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.DeniedByCreator);
                model.HasResponseNotAnsweredByCreatorBrokerConfirmation = requestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.ResponseNotAnsweredByCreator);
                model.HasCancelledByCreatorWhenApprovedConfirmation = requestStatusConfirmations.Any(rs => rs.RequestStatus == RequestStatus.CancelledByCreatorWhenApprovedOrAccepted);

                model.ActiveRequestPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplay(await _dbContext.RequestPriceRows.GetPriceRowsForRequest(model.RequestId.Value).ToListAsync(), PriceInformationType.Request, mealBreakIncluded: model.MealbreakIncluded);
                model.RequestAttachmentListModel = await AttachmentListModel.GetReadOnlyModelFromList(_dbContext.Attachments.GetAttachmentsForRequest(model.RequestId.Value, model.RequestGroupId), "Bifogade filer från förmedling");
                var requestChecks = await _dbContext.Requests
                    .Where(r => r.RequestId == model.RequestId.Value)
                    .Select(r => new
                    {
                        LatestComplaintId = r.Complaints.Max(c => (int?)c.ComplaintId),
                        LatestRequisitionId = r.Requisitions.Max(req => (int?)req.RequisitionId),
                        HasActiveRequisitions = r.Requisitions.Any(req => req.Status == RequisitionStatus.Reviewed || req.Status == RequisitionStatus.Created),
                        HasComplaints = r.Complaints.Any(),
                    }).SingleAsync();
                model.HasComplaints = requestChecks.HasComplaints;
                model.ComplaintId = requestChecks.LatestComplaintId;
                model.RequisitionId = requestChecks.LatestRequisitionId;
                model.HasActiveRequisitions = requestChecks.HasActiveRequisitions;
                model.ActiveRequest.RequirementAnswers = await RequestRequirementAnswerModel.GetFromList(_dbContext.OrderRequirementRequestAnswer.GetRequirementAnswersForRequest(model.RequestId.Value));
                //just for broker 
                var orderChanges = await _dbContext.OrderChangeLogEntries.GetOrderChangeLogEntitesForOrder(id).Where(oc => oc.BrokerId == model.ActiveRequest.BrokerId && oc.OrderChangeLogType != OrderChangeLogType.ContactPerson && oc.OrderChangeConfirmation == null).ToListAsync();
                model.ConfirmedOrderChangeLogEntries = orderChanges.Select(oc => oc.OrderChangeLogEntryId).ToList();
                if (orderChanges.Any() && (model.ActiveRequest.Status == RequestStatus.Approved || model.ActiveRequest.Status == RequestStatus.AcceptedNewInterpreterAppointed) && model.StartAtIsInFuture)
                {
                    model.DisplayOrderChangeText = await GetOrderChangeTextToDisplay(model.ActiveRequest.BrokerId, interpreterLocations, model.InterpreterLocationAnswer, orderChanges, model.Description, model.UnitName, model.InvoiceReference, model.CustomerReferenceNumber);
                }
                if (model.ReplacedByOrderId.HasValue)
                {
                    var request = await _dbContext.Requests.GetLastRequestWithRankingForOrder(model.ReplacedByOrderId.Value);
                    if (request.Ranking.BrokerId == model.ActiveRequest.BrokerId)
                    {
                        model.ActiveRequest.ReplacedByOrderRequestId = request.RequestId;
                    }
                }
                if (model.ReplacingOrderId.HasValue)
                {
                    model.ActiveRequest.ReplacingOrderRequestId = (await _dbContext.Requests.GetLastRequestForOrder(model.ReplacingOrderId.Value)).RequestId;
                }
                //end just for broker
            }
            return model;
        }

        internal async Task<RequisitionModel> AddInformationFromListsToModel(RequisitionModel model)
        {
            model.RequestPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplay(await _dbContext.RequestPriceRows.GetPriceRowsForRequest(model.RequestId).ToListAsync(), PriceInformationType.Request, mealBreakIncluded: model.MealBreakIncluded ?? false, description: "Om rekvisitionen innehåller ersättning för bilersättning och traktamente kan förmedlingen komma att debitera påslag för sociala avgifter för de tolkar som inte är registrerade för F-skatt");
            model.ExpectedTravelCosts = model.RequestPriceInformationModel.ExpectedTravelCosts;
            var previousRequisition = await _dbContext.Requisitions.GetPreviosRequisitionByRequestId(model.RequestId);
            if (previousRequisition != null)
            {
                previousRequisition.PriceRows = await _dbContext.RequisitionPriceRows.GetPriceRowsForRequisition(previousRequisition.RequisitionId).ToListAsync();
                previousRequisition.Attachments = await _dbContext.RequisitionAttachments.GetRequisitionAttachmentsForRequisition(previousRequisition.RequisitionId).ToListAsync();
                previousRequisition.MealBreaks = await _dbContext.MealBreaks.GetMealBreksForRequisition(previousRequisition.RequisitionId).ToListAsync();

                model.PreviousRequisition = PreviousRequisitionViewModel.GetViewModelFromPreviousRequisition(previousRequisition);
                model.PreviousRequisition.Outlay = previousRequisition.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.Outlay)?.Price;
                model.PreviousRequisition.ResultPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplayForRequisition(previousRequisition, true);

                model.SessionStartedAt = previousRequisition.SessionStartedAt;
                model.SessionEndedAt = previousRequisition.SessionEndedAt;
                model.MealBreaks = previousRequisition.MealBreaks.Any() ? previousRequisition.MealBreaks : null;
                if (model.MealBreaks != null && model.MealBreaks.Any())
                {
                    model.MealBreaks.ForEach(mb => SetMealbreakTimes(mb));
                }
                var files = previousRequisition.Attachments.Select(a => new FileModel
                {
                    Id = a.Attachment.AttachmentId,
                    FileName = a.Attachment.FileName,
                    Size = a.Attachment.Blob.Length
                }).ToList();
                model.Files = files.Any() ? files : null;
            }
            return model;
        }

        internal async Task<OrderGroupModel> AddInformationFromListsToModel(OrderGroupModel model)
        {
            int orderGroupId = model.OrderGroupId.Value;
            await AddInformationFromListsToModel(model, orderGroupId);
            model.PreviousRequestGroups = _dbContext.RequestGroups.GetPreviousRequestGroupsForOrderGroup(orderGroupId)
                .Select(r => new BrokerListModel
                {
                    Status = r.Status,
                    BrokerName = r.Ranking.Broker.Name,
                    DenyMessage = r.DenyMessage,
                }).ToList();
            return model;
        }

        internal async Task<OrderBaseModel> AddInformationFromListsToModel(OrderBaseModel model, int orderGroupId)
        {
            model.AttachmentListModel = await AttachmentListModel.GetReadOnlyModelFromList(_dbContext.Attachments.GetAttachmentsForOrderGroup(orderGroupId), "Bifogade filer");

            OrderCompetenceRequirement competenceFirst = null;
            OrderCompetenceRequirement competenceSecond = null;
            var competenceRequirements = _dbContext.OrderGroupCompetenceRequirements.GetOrderedCompetenceRequirementsForOrderGroup(orderGroupId).Select(r => new OrderCompetenceRequirement
            {
                CompetenceLevel = r.CompetenceLevel,
                Rank = r.Rank,
            }).ToList();

            competenceRequirements = competenceRequirements.OrderBy(r => r.Rank).ToList();
            competenceFirst = competenceRequirements.Count > 0 ? competenceRequirements[0] : null;
            competenceSecond = competenceRequirements.Count > 1 ? competenceRequirements[1] : null;
            model.RequestedCompetenceLevelFirst = competenceFirst?.CompetenceLevel;
            model.RequestedCompetenceLevelSecond = competenceSecond?.CompetenceLevel;

            model.OrderRequirements = _dbContext.OrderGroupRequirements.GetRequirementsForOrderGroup(orderGroupId).Select(r => new OrderRequirementModel
            {
                OrderRequirementId = r.OrderGroupRequirementId,
                RequirementDescription = r.Description,
                RequirementIsRequired = r.IsRequired,
                RequirementType = r.RequirementType
            }).ToList();
            return model;
        }

        internal async Task<RequestGroupViewModel> AddInformationFromListsToModel(RequestGroupViewModel model)
        {
            model.AttachmentListModel = await AttachmentListModel.GetReadOnlyModelFromList(_dbContext.Attachments.GetAttachmentsForRequestGroup(model.RequestGroupId), "Bifogade filer");
            return model;
        }

        internal async Task<RequestGroupProcessModel> AddInformationFromListsToModel(RequestGroupProcessModel model, int userId)
        {
            int requestGroupId = model.RequestGroupId;

            var orderGroup = await _dbContext.OrderGroups.GetOrderGroupById(model.OrderGroupId);
            orderGroup.Requirements = await _dbContext.OrderGroupRequirements.GetRequirementsForOrderGroup(orderGroup.OrderGroupId).ToListAsync();
            orderGroup.CompetenceRequirements = await _dbContext.OrderGroupCompetenceRequirements.GetOrderedCompetenceRequirementsForOrderGroup(orderGroup.OrderGroupId).ToListAsync();
            model.RequestedCompetenceLevelFirst = orderGroup.CompetenceRequirements.SingleOrDefault(l => l.Rank == 1 || l.Rank == null)?.CompetenceLevel;
            model.RequestedCompetenceLevelSecond = orderGroup.CompetenceRequirements.SingleOrDefault(l => l.Rank == 2)?.CompetenceLevel;
            model.InterpreterAnswerModel = new InterpreterAnswerModel
            {
                RequiredRequirementAnswers = orderGroup.Requirements.Where(r => r.IsRequired).Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderGroupRequirementId,
                    IsRequired = true,
                    Description = r.Description,
                    RequirementType = r.RequirementType,
                }).ToList(),
                DesiredRequirementAnswers = orderGroup.Requirements.Where(r => !r.IsRequired).Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderGroupRequirementId,
                    IsRequired = false,
                    Description = r.Description,
                    RequirementType = r.RequirementType,
                }).ToList(),
            };
            model.ExtraInterpreterAnswerModel = model.HasExtraInterpreter ? new InterpreterAnswerModel
            {
                RequiredRequirementAnswers = orderGroup.Requirements.Where(r => r.IsRequired).Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderGroupRequirementId,
                    IsRequired = true,
                    Description = r.Description,
                    RequirementType = r.RequirementType,
                }).ToList(),
                DesiredRequirementAnswers = orderGroup.Requirements.Where(r => !r.IsRequired).Select(r => new RequestRequirementAnswerModel
                {
                    OrderRequirementId = r.OrderGroupRequirementId,
                    IsRequired = false,
                    Description = r.Description,
                    RequirementType = r.RequirementType,
                }).ToList(),
            } : null;
            if (model.AllowAccept)
            {
                model.InterpreterAcceptModel = new InterpreterAcceptModel
                {
                    RequiredRequirementAnswers = orderGroup.Requirements.Where(r => r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderGroupRequirementId,
                        IsRequired = true,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                    }).ToList(),
                    DesiredRequirementAnswers = orderGroup.Requirements.Where(r => !r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderGroupRequirementId,
                        IsRequired = false,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                    }).ToList(),
                };
                model.ExtraInterpreterAcceptModel = model.HasExtraInterpreter ? new InterpreterAcceptModel
                {
                    RequiredRequirementAnswers = orderGroup.Requirements.Where(r => r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderGroupRequirementId,
                        IsRequired = true,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                    }).ToList(),
                    DesiredRequirementAnswers = orderGroup.Requirements.Where(r => !r.IsRequired).Select(r => new RequestRequirementAnswerModel
                    {
                        OrderRequirementId = r.OrderGroupRequirementId,
                        IsRequired = false,
                        Description = r.Description,
                        RequirementType = r.RequirementType,
                    }).ToList(),
                } : null;
            }
            model.Dialect = orderGroup.Requirements.Any(r => r.RequirementType == RequirementType.Dialect) ? orderGroup.Requirements.Single(r => r.RequirementType == RequirementType.Dialect)?.Description : string.Empty;

            var views = await _dbContext.RequestGroupViews.GetActiveViewsForRequestGroup(requestGroupId).ToListAsync();
            model.ViewedByUser = views.Any(rv => rv.ViewedBy != userId) ?
                views.First(rv => rv.ViewedBy != userId).ViewedByUser.FullName + " håller också på med denna förfrågan"
                : string.Empty;
            model.AttachmentListModel = await AttachmentListModel.GetReadOnlyModelFromList(_dbContext.Attachments.GetAttachmentsForOrderGroup(orderGroup.OrderGroupId), "Bifogade filer");
            return model;
        }

        private static void SetMealbreakTimes(MealBreak mb)
        {
            mb.StartAtTemp = mb.StartAt.DateTime;
            mb.EndAtTemp = mb.EndAt.DateTime;
        }

        internal async Task<RequisitionViewModel> AddInformationFromListsToModel(RequisitionViewModel model)
        {
            var requisitions = await _dbContext.Requisitions.GetRequisitionsForRequest(model.RequestId).ToListAsync();
            var viewedRequisition = requisitions.SingleOrDefault(r => r.RequisitionId == model.RequisitionId);
            viewedRequisition.RequisitionStatusConfirmations = await _dbContext.RequisitionStatusConfirmations.GetRequisitionStatusConfirmationsForRequisition(viewedRequisition.RequisitionId).ToListAsync();
            model.AttachmentListModel = await AttachmentListModel.GetReadOnlyModelFromList(_dbContext.Attachments.GetAttachmentsForRequisition(viewedRequisition.RequisitionId), "Bifogade filer");

            model.CanProcess = viewedRequisition.ProcessAllowed;
            model.CanConfirmNoReview = viewedRequisition.CofirmNoReviewAllowed;
            model.CanReplaceRequisition = requisitions.All(r => r.Status == RequisitionStatus.Commented) && requisitions.OrderBy(r => r.CreatedAt).Last().RequisitionId == viewedRequisition.RequisitionId;
            viewedRequisition.PriceRows = await _dbContext.RequisitionPriceRows.GetPriceRowsForRequisition(viewedRequisition.RequisitionId).ToListAsync();
            viewedRequisition.MealBreaks = await _dbContext.MealBreaks.GetMealBreksForRequisition(viewedRequisition.RequisitionId).ToListAsync();
            model.ResultPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplayForRequisition(viewedRequisition, false);
            model.RequestPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplay(await _dbContext.RequestPriceRows.GetPriceRowsForRequest(model.RequestId).ToListAsync(), PriceInformationType.Request, mealBreakIncluded: model.MealBreakIncluded ?? false, description: "Om rekvisitionen innehåller ersättning för bilersättning och traktamente kan förmedlingen komma att debitera påslag för sociala avgifter för de tolkar som inte är registrerade för F-skatt");
            if (requisitions.Count < 2)
            {
                return null;
            }
            var previousRequisition = requisitions
                .Where(r => r.Status == RequisitionStatus.Commented || r.Status == RequisitionStatus.DeniedByCustomer)
                .OrderByDescending(r => r.CreatedAt)
                .First();

            model.PreviousRequisitionView = RequisitionViewModel.GetPreviousRequisitionView(previousRequisition.Request);
            model.PreviousRequisitionView.AttachmentListModel = await AttachmentListModel.GetReadOnlyModelFromList(_dbContext.Attachments.GetAttachmentsForRequisition(previousRequisition.RequisitionId), "Bifogade filer");
            previousRequisition.PriceRows = await _dbContext.RequisitionPriceRows.GetPriceRowsForRequisition(previousRequisition.RequisitionId).ToListAsync();
            previousRequisition.MealBreaks = await _dbContext.MealBreaks.GetMealBreksForRequisition(previousRequisition.RequisitionId).ToListAsync();
            model.PreviousRequisitionView.ResultPriceInformationModel = PriceInformationModel.GetPriceinformationToDisplayForRequisition(previousRequisition, false);
            model.PreviousRequisitionView.RequestPriceInformationModel = model.RequestPriceInformationModel;


            model.RelatedRequisitions = requisitions
                .OrderBy(r => r.CreatedAt)
                .Select(r => r.RequisitionId)
                .ToList();
            return model;
        }

        internal async Task<ReplaceOrderModel> AddInformationFromListsToModel(ReplaceOrderModel model)
        {
            int id = model.ReplacingOrderId;
            //LISTS
            await GetOrderBaseLists(model, await _dbContext.OrderInterpreterLocation.GetOrderedInterpreterLocationsForOrder(id).ToListAsync(), id);

            model.AttachmentListModel = await AttachmentListModel.GetEditableModelFromList(_dbContext.Attachments.GetAttachmentsForOrder(model.ReplacingOrderId), string.Empty, "Möjlighet att bifoga filer som kan vara relevanta vid tillsättning av tolk");
            model.Files = model.AttachmentListModel.Files.Any() ? model.AttachmentListModel.Files : null;

            return model;
        }

        private async Task<OrderBaseModel> GetOrderBaseLists(OrderBaseModel model, IEnumerable<OrderInterpreterLocation> interpreterLocations, int orderId)
        {
            //Locations
            model.RankedInterpreterLocationFirst = interpreterLocations.Single(l => l.Rank == 1)?.InterpreterLocation;
            model.RankedInterpreterLocationSecond = interpreterLocations.SingleOrDefault(l => l.Rank == 2)?.InterpreterLocation;
            model.RankedInterpreterLocationThird = interpreterLocations.SingleOrDefault(l => l.Rank == 3)?.InterpreterLocation;
            model.RankedInterpreterLocationFirstAddressModel = OrderBaseModel.GetInterpreterLocation(interpreterLocations.Single(l => l.Rank == 1));
            model.RankedInterpreterLocationSecondAddressModel = OrderBaseModel.GetInterpreterLocation(interpreterLocations.SingleOrDefault(l => l.Rank == 2));
            model.RankedInterpreterLocationThirdAddressModel = OrderBaseModel.GetInterpreterLocation(interpreterLocations.SingleOrDefault(l => l.Rank == 3));

            //Competences
            List<CompetenceAndSpecialistLevel> competenceRequirements = await _dbContext.OrderCompetenceRequirements
                .GetOrderedCompetenceRequirementsForOrder(orderId)
                .Select(r => r.CompetenceLevel)
                .ToListAsync();

            model.RequestedCompetenceLevelFirst = competenceRequirements.Any() ? (CompetenceAndSpecialistLevel?)competenceRequirements.FirstOrDefault() : null;
            model.RequestedCompetenceLevelSecond = competenceRequirements.Count > 1 ? (CompetenceAndSpecialistLevel?)competenceRequirements[1] : null;
            model.OrderRequirements = await OrderRequirementModel.GetFromList(_dbContext.OrderRequirements.GetRequirementsForOrder(orderId));
            return model;
        }

        internal async Task<string> GetOtherViewer(int requestId, int currentUserId)
        {
            var otherViewer = await _dbContext.RequestViews.GetActiveViewsForRequest(requestId).FirstOrDefaultAsync(rv => rv.ViewedBy != currentUserId);
            return otherViewer != null ?
                $"{otherViewer.ViewedByUser.FullName} håller också på med denna förfrågan" :
                string.Empty;
        }

        private async Task<string> GetOrderChangeTextToDisplay(int brokerId, List<OrderInterpreterLocation> interpreterLocations, InterpreterLocation interpreterLocation, List<OrderChangeLogEntry> orderChanges, string description, string unitName, string invoiceRef, string custRefNo)
        {
            StringBuilder sb = new StringBuilder();
            var unconfirmedOrderChangeLogEntries = orderChanges
                .Where(oc => oc.OrderChangeConfirmation == null && oc.BrokerId == brokerId)
                .OrderBy(oc => oc.OrderChangeLogEntryId);

            var orderChangeLogEntries = unconfirmedOrderChangeLogEntries.Where(oc => oc.OrderChangeLogType == OrderChangeLogType.OrderInformationFields || oc.OrderChangeLogType == OrderChangeLogType.AttachmentAndOrderInformationFields).ToList();
            string interpreterLocationText = interpreterLocation == InterpreterLocation.OffSitePhone || interpreterLocation == InterpreterLocation.OffSiteVideo ?
               interpreterLocations.Where(il => il.InterpreterLocation == interpreterLocation).Single().OffSiteContactInformation :
               interpreterLocations.Where(il => il.InterpreterLocation == interpreterLocation).Single().Street;
            int i = 0;
            var orderHistories = await _dbContext.OrderHistoryEntries.GetOrderHistoriesForOrderChangeConfirmations(orderChangeLogEntries.Select(o => o.OrderChangeLogEntryId).ToList()).ToListAsync();
            foreach (OrderChangeLogEntry oce in orderChangeLogEntries)
            {
                i++;
                var nextToCompareTo = orderChangeLogEntries.Count > i ? orderChangeLogEntries[i] : null;
                var date = $"{oce.LoggedAt.ToSwedishString("yyyy-MM-dd HH:mm")} - ";
                foreach (OrderHistoryEntry oh in orderHistories.Where(oh => oh.OrderChangeLogEntryId == oce.OrderChangeLogEntryId))
                {
                    switch (oh.ChangeOrderType)
                    {
                        case ChangeOrderType.LocationStreet:
                            sb.Append(GetOrderFieldText(date, oh, nextToCompareTo == null ? interpreterLocationText : orderHistories.Where(ohe => ohe.OrderChangeLogEntryId == nextToCompareTo.OrderChangeLogEntryId).SingleOrDefault(o => o.ChangeOrderType == ChangeOrderType.LocationStreet).Value));
                            break;
                        case ChangeOrderType.OffSiteContactInformation:
                            sb.Append(GetOrderFieldText(date, oh, nextToCompareTo == null ? interpreterLocationText : orderHistories.Where(ohe => ohe.OrderChangeLogEntryId == nextToCompareTo.OrderChangeLogEntryId).SingleOrDefault(o => o.ChangeOrderType == ChangeOrderType.OffSiteContactInformation).Value));
                            break;
                        case ChangeOrderType.Description:
                            sb.Append(GetOrderFieldText(date, oh, nextToCompareTo == null ? description : orderHistories.Where(ohe => ohe.OrderChangeLogEntryId == nextToCompareTo.OrderChangeLogEntryId).SingleOrDefault(o => o.ChangeOrderType == ChangeOrderType.Description).Value));
                            break;
                        case ChangeOrderType.InvoiceReference:
                            sb.Append(GetOrderFieldText(date, oh, nextToCompareTo == null ? invoiceRef : orderHistories.Where(ohe => ohe.OrderChangeLogEntryId == nextToCompareTo.OrderChangeLogEntryId).SingleOrDefault(o => o.ChangeOrderType == ChangeOrderType.InvoiceReference).Value));
                            break;
                        case ChangeOrderType.CustomerReferenceNumber:
                            sb.Append(GetOrderFieldText(date, oh, nextToCompareTo == null ? custRefNo : orderHistories.Where(ohe => ohe.OrderChangeLogEntryId == nextToCompareTo.OrderChangeLogEntryId).SingleOrDefault(o => o.ChangeOrderType == ChangeOrderType.CustomerReferenceNumber).Value));
                            break;
                        case ChangeOrderType.CustomerDepartment:
                            sb.Append(GetOrderFieldText(date, oh, nextToCompareTo == null ? unitName : orderHistories.Where(ohe => ohe.OrderChangeLogEntryId == nextToCompareTo.OrderChangeLogEntryId).SingleOrDefault(o => o.ChangeOrderType == ChangeOrderType.CustomerDepartment).Value));
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
            var orderAttachmentChangeLogEntries = unconfirmedOrderChangeLogEntries.Where(oc => oc.OrderChangeLogType == OrderChangeLogType.Attachment || oc.OrderChangeLogType == OrderChangeLogType.AttachmentAndOrderInformationFields).ToList();
            if (orderAttachmentChangeLogEntries.Any())
            {
                sb.Append("\n");
                foreach (OrderChangeLogEntry oce in orderAttachmentChangeLogEntries)
                {
                    sb.Append($"{oce.LoggedAt.ToSwedishString("yyyy-MM-dd HH:mm")} - Bifogade bilagor ändrade\n");
                }
            }
            return sb.ToString();
        }

        private static string GetOrderFieldText(string date, OrderHistoryEntry oh, string newValue)
        {
            return (string.IsNullOrEmpty(newValue) && string.IsNullOrEmpty(oh.Value)) ? string.Empty :
                string.IsNullOrEmpty(newValue) ? $"{date}{oh.ChangeOrderType.GetDescription()} - Informationen togs bort\n" :
                newValue.Equals(oh.Value, StringComparison.OrdinalIgnoreCase) ? string.Empty :
                $"{date}{oh.ChangeOrderType.GetDescription()} - Nytt värde: {newValue}\n";
        }

    }
}

