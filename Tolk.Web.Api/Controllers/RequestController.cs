using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Enums;
using Tolk.Api.Payloads.Responses;
using Tolk.Api.Payloads.WebHookPayloads;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Exceptions;
using Tolk.Web.Api.Helpers;
using Tolk.Web.Api.Services;

namespace Tolk.Web.Api.Controllers
{
    public class RequestController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly RequestService _requestService;
        private readonly ApiUserService _apiUserService;
        private readonly ISwedishClock _timeService;
        private readonly ApiOrderService _apiOrderService;
        private readonly ILogger _logger;

        public RequestController(
            TolkDbContext tolkDbContext,
            RequestService requestService,
            ApiUserService apiUserService,
            ISwedishClock timeService,
            ApiOrderService apiOrderService,
            ILogger<RequestController> logger)
        {
            _dbContext = tolkDbContext;
            _apiUserService = apiUserService;
            _timeService = timeService;
            _requestService = requestService;
            _apiOrderService = apiOrderService;
            _logger = logger;
        }

        #region Updating Methods

        [HttpPost]
        public async Task<JsonResult> Answer([FromBody] RequestAnswerModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var apiUser = await GetApiUser();
                var order = await _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(o => o.Requests).ThenInclude(r => r.RequirementAnswers)
                .Include(o => o.Requests).ThenInclude(r => r.PriceRows)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.CustomerUnit)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ContactPersonUser)
                .Include(o => o.Requirements)
                .Include(o => o.InterpreterLocations)
                .Include(o => o.CompetenceRequirements)
                .Include(o => o.Language)
                .SingleOrDefaultAsync(o => o.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId));
                if (order == null)
                {
                    return ReturnError(ErrorCodes.OrderNotFound);
                }
                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
                var request = order.Requests.SingleOrDefault(r =>
                apiUser.BrokerId == r.Ranking.BrokerId &&
                //Possibly other statuses, but this code is only temporary. Should be coalesced with the controller code.
                (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                InterpreterBroker interpreter;
                try
                {
                    interpreter = _apiUserService.GetInterpreter(new InterpreterDetailsModel(model.Interpreter), apiUser.BrokerId.Value);
                }
                catch (InvalidOperationException)
                {
                    return ReturnError(ErrorCodes.InterpreterOfficialIdAlreadySaved);
                }

                //Does not handle Kammarkollegiets tolknummer
                if (interpreter == null)
                {
                    //Possibly the interpreter should be added, if not found?? 
                    return ReturnError(ErrorCodes.InterpreterNotFound);
                }
                if (model.Location == null)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, "Location was missing");
                }
                if (model.CompetenceLevel == null)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, "CompetenceLevel was missing");
                }
                var now = _timeService.SwedenNow;
                if (request.Status == RequestStatus.Created)
                {
                    _requestService.Acknowledge(request, now, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null));
                }
                try
                {
                    await _requestService.Accept(
                        request,
                        now,
                        user?.Id ?? apiUser.Id,
                        (user != null ? (int?)apiUser.Id : null),
                        interpreter,
                        EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.Location).Value,
                        EnumHelper.GetEnumByCustomName<CompetenceAndSpecialistLevel>(model.CompetenceLevel).Value,
                        model.RequirementAnswers == null ? new List<OrderRequirementRequestAnswer>() :
                        model.RequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                        {
                            Answer = ra.Answer,
                            CanSatisfyRequirement = ra.CanMeetRequirement,
                            OrderRequirementId = ra.RequirementId,
                        }).ToList(),
                        //Does not handle attachments yet.
                        new List<RequestAttachment>(),
                        model.ExpectedTravelCosts,
                        model.ExpectedTravelCostInfo
                    );
                    await _dbContext.SaveChangesAsync();
                    //End of service
                    return Json(new AnswerResponse { InterpreterId = interpreter.InterpreterBrokerId });
                }
                catch (InvalidOperationException ex)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, ex.Message);
                }
                catch (ArgumentNullException ex)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, ex.Message);
                }
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<JsonResult> Acknowledge([FromBody] RequestAcknowledgeModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var apiUser = await GetApiUser();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, apiUser.BrokerId.Value);
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
                var request = await _dbContext.Requests
                    .SingleOrDefaultAsync(r => r.Order.OrderNumber == model.OrderNumber && apiUser.BrokerId == r.Ranking.BrokerId && r.Status == RequestStatus.Created);
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                _requestService.Acknowledge(request, _timeService.SwedenNow, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null));
                await _dbContext.SaveChangesAsync();
                //End of service
                return Json(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<JsonResult> Decline([FromBody] RequestDeclineModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var apiUser = await GetApiUser();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, apiUser.BrokerId.Value);
                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
                var request = await _dbContext.Requests
                    .Include(r => r.Order).ThenInclude(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.Order.CreatedByUser)
                    .Include(r => r.Order.CustomerUnit)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests)
                    .SingleOrDefaultAsync(r => r.Order.OrderNumber == model.OrderNumber &&
                        //Must have a request connected to the order for the broker, any status...
                        r.Ranking.BrokerId == apiUser.BrokerId &&
                        //Possibly other statuses, but this code is only temporary. Should be coalesced with the controller code.
                        (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                await _requestService.Decline(request, _timeService.SwedenNow, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null), model.Message);
                await _dbContext.SaveChangesAsync();
                //End of service
                return Json(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<JsonResult> Cancel([FromBody] RequestCancelModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var apiUser = await GetApiUser();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, apiUser.BrokerId.Value);
                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);

                var request = await _dbContext.Requests
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                    .Include(r => r.Order.CreatedByUser)
                    .Include(r => r.Order.ContactPersonUser)
                    .Include(r => r.Interpreter)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .SingleOrDefaultAsync(r => r.Order.OrderNumber == model.OrderNumber &&
                //Must have a request connected to the order for the broker, any status...
                r.Ranking.BrokerId == apiUser.BrokerId &&
                //TODO: Possibly other statuses, but this code is only temporary. Should be coalesced with the controller code.
                (r.Status == RequestStatus.Approved));
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                try
                {
                    _requestService.CancelByBroker(request, _timeService.SwedenNow, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null), model.Message);
                    await _dbContext.SaveChangesAsync();

                }
                catch (InvalidOperationException)
                {
                    //TODO: Should log the acctual exception here!!
                    return ReturnError(ErrorCodes.RequestNotInCorrectState);
                }

                //End of service
                return Json(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<JsonResult> ChangeInterpreter([FromBody] RequestAnswerModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var apiUser = await GetApiUser();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, apiUser.BrokerId.Value);
                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
                var request = await _dbContext.Requests
                    .Include(r => r.RequestGroup)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                    .Include(r => r.Order).ThenInclude(o => o.Requests).ThenInclude(r => r.PriceRows)
                    .Include(r => r.Order).ThenInclude(o => o.Requirements)
                    .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                    .Include(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                    .Include(r => r.Order.CreatedByUser)
                    .Include(r => r.Order.ContactPersonUser)
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .SingleOrDefaultAsync(r => r.Order.OrderNumber == model.OrderNumber &&
                        r.Ranking.BrokerId == apiUser.BrokerId &&
                        (r.Status == RequestStatus.Approved ||
                        r.Status == RequestStatus.Created ||
                        r.Status == RequestStatus.Received ||
                        r.Status == RequestStatus.InterpreterReplaced ||
                        r.Status == RequestStatus.Accepted));
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                InterpreterBroker interpreter;
                try
                {
                    interpreter = _apiUserService.GetInterpreter(new InterpreterDetailsModel(model.Interpreter), apiUser.BrokerId.Value);
                }
                catch (InvalidOperationException)
                {
                    return ReturnError(ErrorCodes.InterpreterOfficialIdAlreadySaved);
                }
                if (interpreter == null)
                {
                    //Possibly the interpreter should be added, if not found?? 
                    return ReturnError(ErrorCodes.InterpreterNotFound);
                }
                if (model.Location == null)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, "Location was missing");
                }
                if (model.CompetenceLevel == null)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, "CompetenceLevel was missing");
                }
                try
                {
                    await _requestService.ChangeInterpreter(
                        request,
                        _timeService.SwedenNow,
                        user?.Id ?? apiUser.Id,
                        (user != null ? (int?)apiUser.Id : null),
                        interpreter,
                        EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.Location).Value,
                        EnumHelper.GetEnumByCustomName<CompetenceAndSpecialistLevel>(model.CompetenceLevel).Value,
                        model.RequirementAnswers == null ? new List<OrderRequirementRequestAnswer>() :
                        model.RequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                        {
                            Answer = ra.Answer,
                            CanSatisfyRequirement = ra.CanMeetRequirement,
                            OrderRequirementId = ra.RequirementId,
                        }).ToList(),
                        //Does not handle attachments yet.
                        new List<RequestAttachment>(),
                        model.ExpectedTravelCosts,
                        model.ExpectedTravelCostInfo);
                    await _dbContext.SaveChangesAsync();
                }
                catch (InvalidOperationException)
                {
                    //TODO: Should log the acctual exception here!!
                    return ReturnError(ErrorCodes.RequestNotInCorrectState);
                }
                return Json(new ChangeInterpreterResponse { InterpreterId = interpreter.InterpreterBrokerId });
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<JsonResult> AcceptReplacement([FromBody] RequestAcceptReplacementModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var apiUser = await GetApiUser();

                var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(o => o.Requests).ThenInclude(r => r.PriceRows)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.CustomerUnit)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ContactPersonUser)
                .SingleOrDefault(o => o.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId));
                if (order == null)
                {
                    return ReturnError(ErrorCodes.OrderNotFound);
                }
                //Possibly the user should be added, if not found?? 
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
                var request = order.Requests.SingleOrDefault(r =>
                    apiUser.BrokerId == r.Ranking.BrokerId &&
                    r.Order.ReplacingOrderId != null &&
                    //Possibly other statuses
                    (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
                if (request == null)
                {
                    return ReturnError(ErrorCodes.RequestNotFound);
                }
                if (model.Location == null)
                {
                    return ReturnError(ErrorCodes.RequestNotCorrectlyAnswered, "Location was missing");
                }
                var now = _timeService.SwedenNow;
                //Add transaction here!!!
                if (request.Status == RequestStatus.Created)
                {
                    request.Received(now, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null));
                }
                _requestService.AcceptReplacement(
                    request,
                    now,
                    user?.Id ?? apiUser.Id,
                    (user != null ? (int?)apiUser.Id : null),
                    EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.Location).Value,
                    model.ExpectedTravelCosts,
                    model.ExpectedTravelCostInfo
                );
                _dbContext.SaveChanges();
                //End of service
                return Json(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<JsonResult> ConfirmDenial([FromBody] ConfirmDenialModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var apiUser = await GetApiUser();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, apiUser.BrokerId.Value);
                //Get User, if any...
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
                Request request = await GetConfirmedRequest(model.OrderNumber, apiUser.BrokerId.Value, new[] { RequestStatus.DeniedByCreator });
                await _requestService.ConfirmDenial(
                    request,
                    _timeService.SwedenNow,
                    user?.Id ?? apiUser.Id,
                    (user != null ? (int?)apiUser.Id : null)
                );
                //Do The magic
                return Json(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpPost]
        public async Task<JsonResult> ConfirmCancellation([FromBody] ConfirmCancellationModel model)
        {
            if (model == null)
            {
                return ReturnError(ErrorCodes.IncomingPayloadIsMissing);
            }
            try
            {
                var apiUser = await GetApiUser();
                var order = await _apiOrderService.GetOrderAsync(model.OrderNumber, apiUser.BrokerId.Value);
                //Get User, if any...
                var user = await _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
                Request request = await GetConfirmedRequest(model.OrderNumber, apiUser.BrokerId.Value, new[] { RequestStatus.CancelledByCreator, RequestStatus.CancelledByCreatorWhenApproved });
                await _requestService.ConfirmCancellation(
                    request,
                    _timeService.SwedenNow,
                    user?.Id ?? apiUser.Id,
                    (user != null ? (int?)apiUser.Id : null)
                );
                return Json(new ResponseBase());
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        #endregion

        #region getting methods

        [HttpGet]
        public async Task<JsonResult> File(string orderNumber, int attachmentId, string callingUser)
        {
            _logger.LogInformation($"{callingUser} called {nameof(File)} to get the attachment {attachmentId} on order {orderNumber}");

            try
            {
                var apiUser = await GetApiUser();
                var order = _dbContext.Orders
                    .Include(o => o.Requests).ThenInclude(r => r.Ranking)
                    .Include(o => o.Attachments).ThenInclude(a => a.Attachment)
                    .SingleOrDefault(o => o.OrderNumber == orderNumber &&
                        //Must have a request connected to the order for the broker, any status...
                        o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId));
                if (order == null)
                {
                    return ReturnError(ErrorCodes.OrderNotFound);
                }

                var attachment = order.Attachments.Where(a => a.AttachmentId == attachmentId).SingleOrDefault()?.Attachment;
                if (attachment == null)
                {
                    return ReturnError(ErrorCodes.AttachmentNotFound);
                }

                return Json(new FileResponse
                {
                    FileBase64 = Convert.ToBase64String(attachment.Blob)
                });
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpGet]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is a public api, do not return 500")]
        public async Task<JsonResult> View(string orderNumber, string callingUser)
        {
            _logger.LogInformation($"'{callingUser ?? "Unspecified user"}' called {nameof(View)} for the active request for the order {orderNumber}");
            try
            {
                var apiUser = await GetApiUser();

                //GET THE MOST CURRENT REQUEST, IE THE REQUEST WITHOUT ReplacedBy....
                var request = await _dbContext.Requests
                    .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                    .Include(r => r.RequirementAnswers)
                    .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                    .Include(r => r.Interpreter)
                    .Include(r => r.Order).ThenInclude(o => o.CreatedByUser)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerUnit)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.Region)
                    .Include(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Order).ThenInclude(o => o.Requirements)
                    .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                    .Include(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                    .Include(r => r.Order).ThenInclude(o => o.Attachments).ThenInclude(a => a.Attachment)
                    .Include(r => r.Order).ThenInclude(o => o.PriceRows).ThenInclude(p => p.PriceListRow)
                    .SingleOrDefaultAsync(r => r.Order.OrderNumber == orderNumber &&
                        //Must have a request connected to the order for the broker, any status...
                        r.Ranking.BrokerId == apiUser.BrokerId &&
                        r.ReplacingRequest == null);
                if (request == null)
                {
                    return ReturnError(ErrorCodes.OrderNotFound);
                }
                //End of service
                return Json(ApiOrderService.GetResponseFromRequest(request));
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Unexpected error occured when client called Request/{nameof(View)}");
                return ReturnError(ErrorCodes.UnspecifiedProblem);
            }
        }

        #endregion

        #region private methods

        private async Task<Request> GetConfirmedRequest(string orderNumber, int brokerId, IEnumerable<RequestStatus> expectedStatuses)
        {
            var request = await _dbContext.Requests
                .Include(r => r.Ranking)
                .Include(r => r.Order)
                .Include(r => r.RequestStatusConfirmations)
                .SingleOrDefaultAsync(r => r.Order.OrderNumber == orderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    r.Ranking.BrokerId == brokerId && expectedStatuses.Contains(r.Status));
            if (request == null)
            {
                throw new InvalidApiCallException(ErrorCodes.RequestNotFound);
            }

            return request;
        }

        //Break out to error generator service...
        private JsonResult ReturnError(string errorCode, string specifiedErrorMessage = null)
        {
            //TODO: Add to log, information...
            var message = TolkApiOptions.ErrorResponses.Single(e => e.ErrorCode == errorCode).Copy();
            Response.StatusCode = message.StatusCode;
            if (!string.IsNullOrEmpty(specifiedErrorMessage))
            {
                message.ErrorMessage = specifiedErrorMessage;
            }
            return Json(message);
        }

        //Break out to a auth pipline
        private async Task<AspNetUser> GetApiUser()
        {
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-UserName", out var userName);
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-ApiKey", out var key);
            return await _apiUserService.GetApiUser(Request.HttpContext.Connection.ClientCertificate, userName, key);
        }

        private static RequestDetailsResponse GetResponseFromRequest(Request request)
        {
            var attach = request.Attachments;
            return new RequestDetailsResponse
            {
                Status = request.Status.GetCustomName(),
                StatusMessage = request.DenyMessage ?? request.CancelMessage,
                CreatedAt = request.CreatedAt,
                OrderNumber = request.Order.OrderNumber,
                CustomerInformation = new CustomerInformationModel
                {
                    Name = request.Order.CustomerOrganisation.Name,
                    Key = request.Order.CustomerOrganisation.OrganisationPrefix,
                    OrganisationNumber = request.Order.CustomerOrganisation.OrganisationNumber,
                    ContactName = request.Order.CreatedByUser.FullName,
                    ContactPhone = request.Order.ContactPhone,
                    ContactEmail = request.Order.ContactEmail,
                    InvoiceReference = request.Order.InvoiceReference,
                    PriceListType = request.Order.CustomerOrganisation.PriceListType.GetCustomName(),
                    TravelCostAgreementType = request.Order.CustomerOrganisation.TravelCostAgreementType.GetCustomName(),
                    ReferenceNumber = request.Order.CustomerReferenceNumber,
                    UnitName = request.Order.CustomerUnit?.Name,
                    DepartmentName = request.Order.UnitName
                },
                Region = request.Order.Region.Name,
                ExpiresAt = request.ExpiresAt,
                Language = new LanguageModel
                {
                    Key = request.Order.Language?.ISO_639_Code,
                    Description = request.Order.OtherLanguage ?? request.Order.Language.Name,
                },
                StartAt = request.Order.StartAt,
                EndAt = request.Order.EndAt,
                Locations = request.Order.InterpreterLocations.Select(l => new LocationModel
                {
                    OffsiteContactInformation = l.OffSiteContactInformation,
                    Street = l.Street,
                    City = l.City,
                    Rank = l.Rank,
                    Key = l.InterpreterLocation.GetCustomName()
                }),
                CompetenceLevels = request.Order.CompetenceRequirements.Select(c => new CompetenceModel
                {
                    Key = c.CompetenceLevel.GetCustomName(),
                    Rank = c.Rank ?? 0
                }),
                CompetenceLevelsAreRequired = request.Order.SpecificCompetenceLevelRequired,
                AllowMoreThanTwoHoursTravelTime = request.Order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved || request.Order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldNotBeApproved,
                Description = request.Order.Description,
                AssignentType = request.Order.AssignmentType.GetCustomName(),
                //Should the attachemts from the broker be applied too? Yes I suppose...
                Attachments = request.Order.Attachments.Select(a => new AttachmentInformationModel
                {
                    AttachmentId = a.AttachmentId,
                    FileName = a.Attachment.FileName
                }),
                Requirements = request.Order.Requirements.Select(r => new RequirementModel
                {
                    Description = r.Description,
                    IsRequired = r.IsRequired,
                    RequirementId = r.OrderRequirementId,
                    RequirementType = r.RequirementType.GetCustomName()
                }),
                CalculatedPriceInformationFromRequest = request.Order.PriceRows.GetPriceInformationModel(request.Order.PriceCalculatedFromCompetenceLevel.GetCustomName(), request.Ranking.BrokerFee),
                CalculatedPriceInformationFromAnswer = request.PriceRows.Any() ?
                    request.PriceRows.GetPriceInformationModel(((CompetenceAndSpecialistLevel)request.CompetenceLevel).GetCustomName(), request.Ranking.BrokerFee)
                    : null,
                Interpreter = request.Interpreter != null ? new InterpreterModel
                {
                    InterpreterId = request.Interpreter.InterpreterBrokerId,
                    Email = request.Interpreter.Email,
                    FirstName = request.Interpreter.FirstName,
                    LastName = request.Interpreter.LastName,
                    OfficialInterpreterId = request.Interpreter.OfficialInterpreterId,
                    PhoneNumber = request.Interpreter.PhoneNumber,
                    InterpreterInformationType = EnumHelper.GetCustomName(InterpreterInformationType.ExistingInterpreter)
                } : null,
                InterpreterLocation = request.InterpreterLocation.HasValue ? EnumHelper.GetCustomName((InterpreterLocation)request.InterpreterLocation) : null,
                InterpreterCompetenceLevel = request.CompetenceLevel.HasValue ? EnumHelper.GetCustomName((CompetenceAndSpecialistLevel)request.CompetenceLevel) : null,
                ExpectedTravelCosts = request.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0,
                ExpectedTravelCostInfo = request.ExpectedTravelCostInfo,
                RequirementAnswers = request.RequirementAnswers
                    .Select(r => new RequirementAnswerModel
                    {
                        Answer = r.Answer,
                        CanMeetRequirement = r.CanSatisfyRequirement,
                        RequirementId = r.OrderRequirementId
                    }),
            };
        }

        #endregion
    }
}
