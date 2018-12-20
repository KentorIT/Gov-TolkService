using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Enums;
using Tolk.Api.Payloads.Responses;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Helpers;
using Tolk.Web.Api.Services;

namespace Tolk.Web.Api.Controllers
{
    public class ListController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly TolkApiOptions _options;
        private readonly ApiUserService _apiUserService;

        public ListController(TolkDbContext tolkDbContext, IOptions<TolkApiOptions> options, ApiUserService apiUserService)
        {
            _dbContext = tolkDbContext;
            _options = options.Value;
            _apiUserService = apiUserService;

        }

        [HttpGet]
        public JsonResult AssignmentTypes()
        {
            return Json(EnumHelper.GetAllFullDescriptions<AssignmentType>().Select(d =>
            new
            {
                Key = d.CustomName,
                d.Description
            }));
        }

        [HttpGet]
        public JsonResult CompetenceLevels()
        {
            return Json(EnumHelper.GetAllFullDescriptions<CompetenceAndSpecialistLevel>().Select(d =>
            new
            {
                Key = d.CustomName,
                d.Description
            }));
        }

        [HttpGet]
        public JsonResult Languages()
        {
            return Json(_dbContext.Languages.Where(l => l.Active == true)
                .OrderBy(l => l.Name).Select(l => new
                {
                    Key = l.ISO_639_1_Code,
                    Desciption = l.Name
                }));
        }

        [HttpGet]
        public JsonResult Regions()
        {
            return Json(_dbContext.Regions
                .OrderBy(r => r.Name).Select(r => new
                {
                    Key = r.RegionId.ToString("D2"),
                    Desciption = r.Name
                }));
        }

        [HttpGet]
        public JsonResult Customers()
        {
            //How will the customers be identified? Need to have a safe way of declaring this! should it be the SFTI identifier?
            //Probably a webhook too! Customer_added, to denote the fact that there is a new possible orderer.  
            return Json(_dbContext.CustomerOrganisations
                .OrderBy(c => c.Name).Select(c => new
                {
                    Key = c.CustomerOrganisationId,
                    PriceListType = c.PriceListType.GetCustomName(),
                    Desciption = c.Name
                }));

        }

        [HttpGet]
        public JsonResult PriceListTypes()
        {
            return Json(EnumHelper.GetAllFullDescriptions<PriceListType>().Select(d =>
            new
            {
                Key = d.CustomName,
                d.Description
            }));
        }

        [HttpGet]
        public JsonResult PriceRowTypes()
        {
            return Json(EnumHelper.GetAllFullDescriptions<PriceRowType>().Select(d =>
            new
            {
                Key = d.CustomName,
                d.Description
            }));
        }

        [HttpGet]
        public JsonResult LocationTypes()
        {
            return Json(EnumHelper.GetAllFullDescriptions<InterpreterLocation>().Select(d =>
            new
            {
                Key = d.CustomName,
                d.Description
            }));
        }

        [HttpGet]
        public JsonResult RequirementTypes()
        {
            return Json(EnumHelper.GetAllFullDescriptions<RequirementType>().Select(d =>
            new
            {
                Key = d.CustomName,
                d.Description
            }));
        }

        [HttpGet]
        public JsonResult InterpreterInformationTypes()
        {
            return Json(EnumHelper.GetAllFullDescriptions<InterpreterInformationType>().Select(d =>
            new
            {
                Key = d.CustomName,
                d.Description
            }));
        }

        [HttpGet]
        public JsonResult BrokerInterpreters()
        {
            var apiUser = GetApiUser();
            if (apiUser == null)
            {
                return ReturError("UNAUTHORIZED");
            }

            return Json(new BrokerInterpretersResponse
            {
                Interpreters = _dbContext.InterpreterBrokers
                    .Where(i => i.BrokerId == apiUser.BrokerId)
                    .Select(i => new InterpreterModel
                    {
                        InterpreterId = i.InterpreterId,
                        Email = i.Email,
                        FirstName = i.FirstName,
                        LastName = i.LastName,
                        OfficialInterpreterId = i.OfficialInterpreterId,
                        PhoneNumber = i.PhoneNumber,
                        InterpreterInformationType = EnumHelper.GetCustomName(InterpreterInformationType.ExistingInterpreter)
                    }).ToList()
            });
        }

        #region SAME AS IN REQUEST, SHOULD BE MOVED

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
        #endregion
    }
}
