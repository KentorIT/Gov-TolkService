using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Enums;
using Tolk.Api.Payloads.Responses;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Exceptions;
using Tolk.Web.Api.Helpers;
using Tolk.Web.Api.Services;
using H = Tolk.Web.Api.Helpers;

namespace Tolk.Web.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ListController : ControllerBase
    {
        private readonly TolkDbContext _dbContext;
        private readonly H.TolkApiOptions _options;
        private readonly ApiUserService _apiUserService;

        public ListController(TolkDbContext tolkDbContext, IOptions<H.TolkApiOptions> options, ApiUserService apiUserService)
        {
            _dbContext = tolkDbContext;
            _options = options?.Value;
            _apiUserService = apiUserService;
        }

        [HttpGet(nameof(AssignmentTypes))]
        public IActionResult AssignmentTypes()
        {
            return DescriptionsAsJson<AssignmentType>();
        }

        [HttpGet(nameof(CompetenceLevels))]
        public IActionResult CompetenceLevels()
        {
            return DescriptionsAsJson<CompetenceAndSpecialistLevel>();
        }

        [HttpGet(nameof(Languages))]
        public IActionResult Languages()
        {
            return Ok(_dbContext.Languages.Where(l => l.Active == true)
                .OrderBy(l => l.Name).Select(l => new
                {
                    Key = l.ISO_639_Code,
                    Desciption = l.Name
                }));
        }

        [HttpGet(nameof(Regions))]
        public IActionResult Regions()
        {
            return Ok(_dbContext.Regions
                .OrderBy(r => r.Name).Select(r => new
                {
                    Key = r.RegionId.ToSwedishString("D2"),
                    Desciption = r.Name
                }));
        }

        [HttpGet(nameof(Customers))]
        public IActionResult Customers()
        {
            return Ok(_dbContext.CustomerOrganisations
                .OrderBy(c => c.Name).Select(c => new CustomerItemResponse
                {
                    Key = c.OrganisationPrefix,
                    OrganisationNumber = c.OrganisationNumber,
                    PriceListType = c.PriceListType.GetCustomName(),
                    Name = c.Name,
                    Description = c.ParentCustomerOrganisationId != null ? $"Organiserad under {c.ParentCustomerOrganisation.Name}" : null
                }));
        }

        [HttpGet(nameof(PriceListTypes))]
        public IActionResult PriceListTypes()
        {
            return DescriptionsAsJson<PriceListType>();
        }
        [HttpGet(nameof(TravelCostAgreementTypes))]
        public IActionResult TravelCostAgreementTypes()
        {
            return DescriptionsAsJson<TravelCostAgreementType>();
        }

        [HttpGet(nameof(PriceRowTypes))]
        public IActionResult PriceRowTypes()
        {
            return DescriptionsAsJson<PriceRowType>();
        }

        [HttpGet(nameof(LocationTypes))]
        public IActionResult LocationTypes()
        {
            return DescriptionsAsJson<InterpreterLocation>();
        }

        [HttpGet(nameof(RequirementTypes))]
        public IActionResult RequirementTypes()
        {
            return DescriptionsAsJson<RequirementType>();
        }

        [HttpGet(nameof(InterpreterInformationTypes))]
        public IActionResult InterpreterInformationTypes()
        {
            return DescriptionsAsJson<InterpreterInformationType>();
        }

        [HttpGet(nameof(TaxCardTypes))]
        public IActionResult TaxCardTypes()
        {
            return DescriptionsAsJson<TaxCardType>();
        }

        [HttpGet(nameof(BrokerInterpreters))]
        public async Task<IActionResult> BrokerInterpreters()
        {
            try
            {
                var apiUser = await GetApiUser();

                return Ok(new BrokerInterpretersResponse
                {
                    Interpreters = _dbContext.InterpreterBrokers
                    .Where(i => i.BrokerId == apiUser.BrokerId)
                    .Select(i => new InterpreterDetailsModel
                    {
                        IsActive = i.IsActive,
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

        [HttpGet(nameof(ComplaintTypes))]
        public IActionResult ComplaintTypes()
        {
            return DescriptionsAsJson<ComplaintType>();
        }

        [HttpGet(nameof(RequestStatuses))]
        public IActionResult RequestStatuses()
        {
            return DescriptionsAsJson<RequestStatus>();
        }

        [HttpGet(nameof(ComplaintStatuses))]
        public IActionResult ComplaintStatuses()
        {
            return DescriptionsAsJson<ComplaintStatus>();
        }

        [HttpGet(nameof(RequisitionStatuses))]
        public IActionResult RequisitionStatuses()
        {
            return DescriptionsAsJson<RequisitionStatus>();
        }

        [HttpGet(nameof(ErrorCodes))]
        public IActionResult ErrorCodes()
        {
            return Ok(TolkApiOptions.ErrorResponses.Select(d => d));
        }

        private IActionResult DescriptionsAsJson<T>()
        {
            return Ok(EnumHelper.GetAllFullDescriptions<T>().Select(d =>
            new
            {
                Key = d.CustomName,
                d.Description
            }));
        }

        #region SAME AS IN REQUEST, SHOULD BE MOVED

        //Break out to error generator service...
        private IActionResult ReturnError(string errorCode)
        {
            //TODO: Add to log, information...
            var message = TolkApiOptions.ErrorResponses.Single(e => e.ErrorCode == errorCode);
            Response.StatusCode = message.StatusCode;
            return Ok(message);
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
