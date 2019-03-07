using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Helpers;
using Tolk.Web.Api.Services;

namespace Tolk.Web.Api.Controllers
{
    public class RequestController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly TolkApiOptions _options;
        private readonly RequestService _requestService;
        private readonly ApiUserService _apiUserService;
        private readonly ISwedishClock _timeService;

        public RequestController(
            TolkDbContext tolkDbContext,
            IOptions<TolkApiOptions> options,
            RequestService requestService,
            ApiUserService apiUserService,
            ISwedishClock timeService)
        {
            _dbContext = tolkDbContext;
            _options = options.Value;
            _apiUserService = apiUserService;
            _timeService = timeService;
            _requestService = requestService;
        }

        #region Updating Methods

        [HttpPost]
        public async Task<JsonResult> Answer([FromBody] RequestAnswerModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            var order = await _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(o => o.Requests).ThenInclude(r => r.RequirementAnswers)
                .Include(o => o.Requests).ThenInclude(r => r.PriceRows)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ContactPersonUser)
                .Include(o => o.Language)
                .SingleOrDefaultAsync(o => o.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId));
            if (order == null)
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
            var request = order.Requests.SingleOrDefault(r =>
                apiUser.BrokerId == r.Ranking.BrokerId &&
                //Possibly other statuses, but this code is only temporary. Should be coalesced with the controller code.
                (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
            if (request == null)
            {
                return ReturError("REQUEST_NOT_FOUND");
            }

            var interpreter = _apiUserService.GetInterpreter(model.Interpreter, apiUser.BrokerId.Value);
            //Does not handle Tolk-Id
            if (interpreter == null)
            {
                //Possibly the interpreter should be added, if not found?? 
                return ReturError("INTERPRETER_NOT_FOUND");
            }
            var now = _timeService.SwedenNow;
            if (request.Status == RequestStatus.Created)
            {
                request.Received(now, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null));
            }
            await _requestService.Accept(
                request,
                now,
                user?.Id ?? apiUser.Id,
                (user != null ? (int?)apiUser.Id : null),
                interpreter,
                EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.Location).Value,
                EnumHelper.GetEnumByCustomName<CompetenceAndSpecialistLevel>(model.CompetenceLevel).Value,
                model.RequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                {
                    Answer = ra.Answer,
                    CanSatisfyRequirement = ra.CanMeetRequirement,
                    OrderRequirementId = ra.RequirementId,
                }).ToList(),
                //Does not handle attachments yet.
                new List<RequestAttachment>(),
                model.ExpectedTravelCosts
            );
            await _dbContext.SaveChangesAsync();
            //End of service
            return Json(new ResponseBase());
        }

        [HttpPost]
        public JsonResult Acknowledge([FromBody] RequestAcknowledgeModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Ranking)
                .SingleOrDefault(o => o.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId));
            if (order == null)
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
            var request = order.Requests.SingleOrDefault(r =>
                apiUser.BrokerId == r.Ranking.BrokerId &&
                (r.Status == RequestStatus.Created));
            if (request == null)
            {
                return ReturError("REQUEST_NOT_FOUND");
            }
            //Add RequestService that does this, and additionally calls _notificationService
            request.Received(_timeService.SwedenNow, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null));
            _dbContext.SaveChanges();
            //End of service
            return Json(new ResponseBase());
        }

        [HttpPost]
        public async Task<JsonResult> Decline([FromBody] RequestDeclineModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Ranking)
                .SingleOrDefault(o => o.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId));
            if (order == null)
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
            var request = _dbContext.Requests
                .Include(r => r.Order).ThenInclude(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order.CreatedByUser)
                .Include(r => r.Order.ContactPersonUser)
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.Order).ThenInclude(o => o.ReplacingOrder).ThenInclude(r => r.Requests)
                .SingleOrDefault(r => r.Order.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    r.Ranking.BrokerId == apiUser.BrokerId &&
                    //Possibly other statuses, but this code is only temporary. Should be coalesced with the controller code.
                    (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
            if (request == null)
            {
                return ReturError("REQUEST_NOT_FOUND");
            }
            await _requestService.Decline(request, _timeService.SwedenNow, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null), model.Message);
            _dbContext.SaveChanges();
            //End of service
            return Json(new ResponseBase());
        }

        [HttpPost]
        public JsonResult Cancel([FromBody] RequestCancelModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            if (!_dbContext.Orders
                .Any(o => o.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId)))
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
            var request = _dbContext.Requests
            .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
            .Include(r => r.Order.CreatedByUser)
            .Include(r => r.Order.ContactPersonUser)
            .Include(r => r.Interpreter)
            .Include(r => r.Ranking).ThenInclude(r => r.Broker)
            .SingleOrDefault(r => r.Order.OrderNumber == model.OrderNumber &&
                //Must have a request connected to the order for the broker, any status...
                r.Ranking.BrokerId == apiUser.BrokerId &&
                //TODO: Possibly other statuses, but this code is only temporary. Should be coalesced with the controller code.
                (r.Status == RequestStatus.Approved));
            if (request == null)
            {
                return ReturError("REQUEST_NOT_FOUND");
            }
            try
            {
                _requestService.CancelByBroker(request, _timeService.SwedenNow, user?.Id ?? apiUser.Id, (user != null ? (int?)apiUser.Id : null), model.Message);
                _dbContext.SaveChanges();

            }
            catch (InvalidOperationException)
            {
                //TODO: Should log the acctual exception here!!
                return ReturError("REQUEST_NOT_IN_CORRECT_STATE");
            }

            //End of service
            return Json(new ResponseBase());
        }

        [HttpPost]
        public async Task<JsonResult> ChangeInterpreter([FromBody] RequestAnswerModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            if (!_dbContext.Orders
                .Any(o => o.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId)))
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
            var request = _dbContext.Requests
            .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
            .Include(r => r.Order.CreatedByUser)
            .Include(r => r.Order.ContactPersonUser)
            .Include(r => r.Ranking).ThenInclude(r => r.Broker)
            .SingleOrDefault(r => r.Order.OrderNumber == model.OrderNumber &&
                r.Ranking.BrokerId == apiUser.BrokerId &&
                (r.Status == RequestStatus.Approved ||
                r.Status == RequestStatus.Created ||
                r.Status == RequestStatus.Received ||
                r.Status == RequestStatus.InterpreterReplaced ||
                r.Status == RequestStatus.Accepted));
            if (request == null)
            {
                return ReturError("REQUEST_NOT_FOUND");
            }
            var interpreter = _apiUserService.GetInterpreter(model.Interpreter, apiUser.BrokerId.Value);
            if (interpreter == null)
            {
                //Possibly the interpreter should be added, if not found?? 
                return ReturError("INTERPRETER_NOT_FOUND");
            }
            var competenceLevel = EnumHelper.GetEnumByCustomName<CompetenceAndSpecialistLevel>(model.CompetenceLevel).Value;

            try
            {
                await _requestService.ChangeInterpreter(
                    request,
                    _timeService.SwedenNow,
                    user?.Id ?? apiUser.Id,
                    (user != null ? (int?)apiUser.Id : null),
                    interpreter,
                    EnumHelper.GetEnumByCustomName<InterpreterLocation>(model.Location).Value,
                    competenceLevel,
                    model.RequirementAnswers.Select(ra => new OrderRequirementRequestAnswer
                    {
                        Answer = ra.Answer,
                        CanSatisfyRequirement = ra.CanMeetRequirement,
                        OrderRequirementId = ra.RequirementId,
                    }).ToList(),
                    //Does not handle attachments yet.
                    new List<RequestAttachment>(),
                    model.ExpectedTravelCosts);
                await _dbContext.SaveChangesAsync();
            }
            catch (InvalidOperationException)
            {
                //TODO: Should log the acctual exception here!!
                return ReturError("REQUEST_NOT_IN_CORRECT_STATE");
            }
            return Json(new ResponseBase());
        }

        [HttpPost]
        public JsonResult AcceptReplacement([FromBody] RequestAcceptReplacementModel model)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(o => o.Requests).ThenInclude(r => r.PriceRows)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.CreatedByUser)
                .Include(o => o.ContactPersonUser)
                .SingleOrDefault(o => o.OrderNumber == model.OrderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId));
            if (order == null)
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(model.CallingUser, apiUser.BrokerId.Value);
            var request = order.Requests.SingleOrDefault(r =>
                apiUser.BrokerId == r.Ranking.BrokerId &&
                r.Order.ReplacingOrderId != null &&
                //Possibly other statuses
                (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received));
            if (request == null)
            {
                return ReturError("REQUEST_NOT_FOUND");
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
                model.ExpectedTravelCosts
            );
            _dbContext.SaveChanges();
            //End of service
            return Json(new ResponseBase());
        }

        #endregion

        #region getting methods

        [HttpGet]
        public JsonResult File(string orderNumber, int attachmentId, string callingUser)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }
            var order = _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Ranking)
                .Include(o => o.Attachments).ThenInclude(a => a.Attachment)
                .SingleOrDefault(o => o.OrderNumber == orderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    o.Requests.Any(r => r.Ranking.BrokerId == apiUser.BrokerId));
            if (order == null)
            {
                return ReturError("ORDER_NOT_FOUND");
            }

            var attachment = order.Attachments.Where(a => a.AttachmentId == attachmentId).SingleOrDefault()?.Attachment;
            if (attachment == null)
            {
                return ReturError("ATTACHMENT_NOT_FOUND");
            }

            return Json(new FileResponse
            {
                FileBase64 = Convert.ToBase64String(attachment.Blob)
            });
        }

        public JsonResult View(string orderNumber, string callingUser)
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }

            //GET THE MOST CURRENT REQUEST, IE THE REQUEST WITHOUT ReplacedBy....
            var request = _dbContext.Requests
                .Include(r => r.Ranking).ThenInclude(r => r.Broker)
                .Include(r => r.RequirementAnswers)
                .Include(r => r.PriceRows).ThenInclude(p => p.PriceListRow)
                .Include(r => r.Interpreter)
                .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                .Include(r => r.Order).ThenInclude(o => o.Region)
                .Include(r => r.Order).ThenInclude(o => o.Language)
                .Include(r => r.Order).ThenInclude(o => o.Requirements)
                .Include(r => r.Order).ThenInclude(o => o.InterpreterLocations)
                .Include(r => r.Order).ThenInclude(o => o.CompetenceRequirements)
                .Include(r => r.Order).ThenInclude(o => o.Attachments)
                .Include(r => r.Order).ThenInclude(o => o.PriceRows).ThenInclude(p => p.PriceListRow)
                .SingleOrDefault(r => r.Order.OrderNumber == orderNumber &&
                    //Must have a request connected to the order for the broker, any status...
                    r.Ranking.BrokerId == apiUser.BrokerId &&
                    r.ReplacingRequestId == null);
            if (request == null)
            {
                return ReturError("ORDER_NOT_FOUND");
            }
            //Possibly the user should be added, if not found?? 
            var user = _apiUserService.GetBrokerUser(callingUser, apiUser.BrokerId.Value);
            //End of service
            return Json(GetResponseFromRequest(request));
        }

        #endregion

        #region private methods

        //Break out to error generator service...
        private JsonResult ReturError(string errorCode)
        {
            //TODO: Add to log, information...
            var message = ErrorResponses.Single(e => e.ErrorCode == errorCode);
            Response.StatusCode = message.StatusCode;
            return Json(message);
        }

        //Break out to a auth pipline
        private AspNetUser GetApiUser()
        {
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-UserName", out var userName);
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-ApiKey", out var key);
            return _apiUserService.GetApiUserByCertificate(Request.HttpContext.Connection.ClientCertificate) ??
                _apiUserService.GetApiUserByApiKey(userName, key);
        }

        //Break out, or fill cache at startup?
        // use this pattern: public const string UNAUTHORIZED = nameof(UNAUTHORIZED);
        private static IEnumerable<ErrorResponse> ErrorResponses
        {
            get
            {
                //TODO: should move to cache!!
                //TODO: should handle information from the call, i.e. Order number and the api method called
                return new List<ErrorResponse>
                {
                    new ErrorResponse { StatusCode = 403, ErrorCode = "UNAUTHORIZED", ErrorMessage = "The api user could not be authorized." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = "ORDER_NOT_FOUND", ErrorMessage = "The provided order number could not be found on a request connected to your organsation." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = "REQUEST_NOT_FOUND", ErrorMessage = "The provided order number has no request in the correct state for the call." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = "INTERPRETER_NOT_FOUND", ErrorMessage = "The provided interpreter was not found." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = "ATTACHMENT_NOT_FOUND", ErrorMessage = "The file coould not be found." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = "REQUEST_NOT_IN_CORRECT_STATE", ErrorMessage = "The request or the underlying order was not in a correct state." },
               };
            }
        }

        public static RequestDetailsResponse GetResponseFromRequest(Request request)
        {
            var attach = request.Attachments;
            return new RequestDetailsResponse
            {
                Status = request.Status.GetCustomName(),
                StatusMessage = request.DenyMessage ?? request.CancelMessage,
                CreatedAt = request.CreatedAt,
                OrderNumber = request.Order.OrderNumber,
                Customer = request.Order.CustomerOrganisation.Name,
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
                    ContactInformation = l.OffSiteContactInformation ?? l.FullAddress,
                    Rank = l.Rank,
                    Key = l.InterpreterLocation.GetCustomName()
                }),
                CompetenceLevels = request.Order.CompetenceRequirements.Select(c => new CompetenceModel
                {
                    Key = c.CompetenceLevel.GetCustomName(),
                    Rank = c.Rank ?? 0
                }),
                CompetenceLevelsAreRequired = request.Order.SpecificCompetenceLevelRequired,
                AllowExceedingTravelCost = request.Order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldBeApproved || request.Order.AllowExceedingTravelCost == AllowExceedingTravelCost.YesShouldNotBeApproved,
                Description = request.Order.Description,
                AssignentType = request.Order.AssignentType.GetCustomName(),
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
                CalculatedPriceInformationFromRequest = new PriceInformationModel
                {
                    PriceCalculatedFromCompetenceLevel = request.Order.PriceCalculatedFromCompetenceLevel.GetCustomName(),
                    PriceRows = request.Order.PriceRows.GroupBy(r => r.PriceRowType)
                        .Select(p => new PriceRowModel
                        {
                            Description = p.Key.GetDescription(),
                            PriceRowType = p.Key.GetCustomName(),
                            Price = p.Count() == 1 ? p.Sum(s => s.TotalPrice) : 0,
                            CalculationBase = p.Count() == 1 ? p.Single()?.PriceCalculationCharge?.ChargePercentage : null,
                            CalculatedFrom = EnumHelper.Parent<PriceRowType, PriceRowType?>(p.Key)?.GetCustomName(),
                            PriceListRows = p.Where(l => l.PriceListRowId != null).Select(l => new PriceRowListModel
                            {
                                PriceListRowType = l.PriceListRow.PriceListRowType.GetCustomName(),
                                Description = l.PriceListRow.PriceListRowType.GetDescription(),
                                Price = l.Price,
                                Quantity = l.Quantity
                            })
                        })
                },
                CalculatedPriceInformationFromAnswer = request.PriceRows.Any() ? new PriceInformationModel
                {
                    PriceCalculatedFromCompetenceLevel = EnumHelper.GetCustomName((InterpreterLocation)request.InterpreterLocation),
                    PriceRows = request.PriceRows.GroupBy(r => r.PriceRowType)
                            .Select(p => new PriceRowModel
                            {
                                Description = p.Key.GetDescription(),
                                PriceRowType = p.Key.GetCustomName(),
                                Price = p.Count() == 1 ? p.Sum(s => s.TotalPrice) : 0,
                                CalculationBase = p.Count() == 1 ? p.Single()?.PriceCalculationCharge?.ChargePercentage : null,
                                CalculatedFrom = EnumHelper.Parent<PriceRowType, PriceRowType?>(p.Key)?.GetCustomName(),
                                PriceListRows = p.Where(l => l.PriceListRowId != null).Select(l => new PriceRowListModel
                                {
                                    PriceListRowType = l.PriceListRow.PriceListRowType.GetCustomName(),
                                    Description = l.PriceListRow.PriceListRowType.GetDescription(),
                                    Price = l.Price,
                                    Quantity = l.Quantity
                                })
                            })
                } : null,
                Interpreter = new InterpreterModel
                {
                    InterpreterId = request.Interpreter.InterpreterBrokerId,
                    Email = request.Interpreter.Email,
                    FirstName = request.Interpreter.FirstName,
                    LastName = request.Interpreter.LastName,
                    OfficialInterpreterId = request.Interpreter.OfficialInterpreterId,
                    PhoneNumber = request.Interpreter.PhoneNumber,
                    InterpreterInformationType = EnumHelper.GetCustomName(InterpreterInformationType.ExistingInterpreter)
                },
                InterpreterLocation = EnumHelper.GetCustomName((InterpreterLocation)request.InterpreterLocation),
                InterpreterCompetenceLevel = EnumHelper.GetCustomName((CompetenceAndSpecialistLevel)request?.CompetenceLevel),
                ExpectedTravelCosts = request.PriceRows.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.TravelCost)?.Price ?? 0,
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
