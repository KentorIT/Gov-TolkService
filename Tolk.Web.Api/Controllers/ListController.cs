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
using H = Tolk.Web.Api.Helpers;
using Tolk.Web.Api.Services;
using System.Threading.Tasks;
using Tolk.Web.Api.Exceptions;
using Tolk.Web.Api.Helpers;

namespace Tolk.Web.Api.Controllers
{
    public class ListController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly H.TolkApiOptions _options;
        private readonly ApiUserService _apiUserService;

        public ListController(TolkDbContext tolkDbContext, IOptions<H.TolkApiOptions> options, ApiUserService apiUserService)
        {
            _dbContext = tolkDbContext;
            _options = options.Value;
            _apiUserService = apiUserService;
        }

        [HttpGet]
        public JsonResult AssignmentTypes()
        {
            return DescriptionsAsJson<AssignmentType>();
        }

        [HttpGet]
        public JsonResult CompetenceLevels()
        {
            return DescriptionsAsJson<CompetenceAndSpecialistLevel>();
        }

        [HttpGet]
        public JsonResult Languages()
        {
            return Json(_dbContext.Languages.Where(l => l.Active == true)
                .OrderBy(l => l.Name).Select(l => new
                {
                    Key = l.ISO_639_Code,
                    Desciption = l.Name
                }));
        }

        [HttpGet]
        public JsonResult Regions()
        {
            return Json(_dbContext.Regions
                .OrderBy(r => r.Name).Select(r => new
                {
                    Key = r.RegionId.ToSwedishString("D2"),
                    Desciption = r.Name
                }));
        }

        [HttpGet]
        public JsonResult Customers()
        {
            return Json(_dbContext.CustomerOrganisations
                .OrderBy(c => c.Name).Select(c => new CustomerItemResponse
                {
                    Key = c.OrganisationPrefix,
                    OrganisationNumber = c.OrganisationNumber,
                    PriceListType = c.PriceListType.GetCustomName(),
                    Name = c.Name, 
                    Description = c.ParentCustomerOrganisationId != null ? $"Organiserad under {c.ParentCustomerOrganisation.Name}" : null
                }));

        }

        [HttpGet]
        public JsonResult PriceListTypes()
        {
            return DescriptionsAsJson<PriceListType>();
        }

        [HttpGet]
        public JsonResult PriceRowTypes()
        {
            return DescriptionsAsJson<PriceRowType>();
        }

        [HttpGet]
        public JsonResult LocationTypes()
        {
            return DescriptionsAsJson<InterpreterLocation>();
        }

        [HttpGet]
        public JsonResult RequirementTypes()
        {
            return DescriptionsAsJson<RequirementType>();
        }

        [HttpGet]
        public JsonResult InterpreterInformationTypes()
        {
            return DescriptionsAsJson<InterpreterInformationType>();
        }

        [HttpGet]
        public JsonResult TaxCardTypes()
        {
            return DescriptionsAsJson<TaxCardType>();
        }

        [HttpGet]
        public async Task<JsonResult> BrokerInterpreters()
        {
            try
            {
                var apiUser = await GetApiUser();

                return Json(new BrokerInterpretersResponse
                {
                    Interpreters = _dbContext.InterpreterBrokers
                    .Where(i => i.BrokerId == apiUser.BrokerId)
                    .Select(i => new InterpreterModel
                    {
                        InterpreterId = i.InterpreterBrokerId,
                        Email = i.Email,
                        FirstName = i.FirstName,
                        LastName = i.LastName,
                        OfficialInterpreterId = i.OfficialInterpreterId,
                        PhoneNumber = i.PhoneNumber,
                        InterpreterInformationType = EnumHelper.GetCustomName(InterpreterInformationType.ExistingInterpreter)
                    }).ToList()
                });
            }
            catch (InvalidApiCallException ex)
            {
                return ReturnError(ex.ErrorCode);
            }
        }

        [HttpGet]
        public JsonResult ComplaintTypes()
        {
            return DescriptionsAsJson<ComplaintType>();
        }

        [HttpGet]
        public JsonResult RequestStatuses()
        {
            return DescriptionsAsJson<RequestStatus>();
        }

        [HttpGet]
        public JsonResult ComplaintStatuses()
        {
            return DescriptionsAsJson<ComplaintStatus>();
        }

        [HttpGet]
        public JsonResult RequisitionStatuses()
        {
            return DescriptionsAsJson<RequisitionStatus>();
        }

        [HttpGet]
        public JsonResult ErrorCodes()
        {
            return Json(TolkApiOptions.ErrorResponses.Select(d => d));
        }

        private JsonResult DescriptionsAsJson<T>()
        {
            return Json(EnumHelper.GetAllFullDescriptions<T>().Select(d =>
            new
            {
                Key = d.CustomName,
                d.Description
            }));
        }

        #region SAME AS IN REQUEST, SHOULD BE MOVED

        //Break out to error generator service...
        private JsonResult ReturnError(string errorCode)
        {
            //TODO: Add to log, information...
            var message = TolkApiOptions.ErrorResponses.Single(e => e.ErrorCode == errorCode);
            Response.StatusCode = message.StatusCode;
            return Json(message);
        }

        //Break out to a auth pipline
        private async Task<AspNetUser> GetApiUser()
        {
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-UserName", out var userName);
            Request.Headers.TryGetValue("X-Kammarkollegiet-InterpreterService-ApiKey", out var key);
            return await _apiUserService.GetApiUser(Request.HttpContext.Connection.ClientCertificate, userName, key);
        }

        #endregion
    }
}
