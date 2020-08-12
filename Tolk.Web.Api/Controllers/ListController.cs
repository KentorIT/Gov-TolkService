using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Enums;
using Tolk.Api.Payloads.Responses;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Authorization;
using Tolk.Web.Api.Helpers;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Api.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ListController : ControllerBase
    {
        private readonly TolkDbContext _dbContext;
        private readonly CacheService _cacheService;

        public ListController(TolkDbContext tolkDbContext, CacheService cacheService)
        {
            _dbContext = tolkDbContext;
            _cacheService = cacheService;
        }

        [Description("Detta är ett försök att få lite dokumentation via description")]
        [HttpGet]
        [AllowAnonymous]
        public ActionResult<IEnumerable<ListItemResponse>> AssignmentTypes()
        {
            return DescriptionsAsJson<AssignmentType>();
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> CompetenceLevels()
        {
            return DescriptionsAsJson<CompetenceAndSpecialistLevel>();
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> Languages()
        {
            return Ok(_dbContext.Languages.Where(l => l.Active == true)
                .OrderBy(l => l.Name).Select(l => new
                {
                    Key = l.ISO_639_Code,
                    Desciption = l.Name
                }));
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> Regions()
        {
            return Ok(_dbContext.Regions
                .OrderBy(r => r.Name).Select(r => new
                {
                    Key = r.RegionId.ToSwedishString("D2"),
                    Desciption = r.Name
                }));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ListItemResponse>>> Customers()
        {
            var customers = await _dbContext.CustomerOrganisations.GetAllCustomers().ToListAsync();
            return Ok(customers.Select(c => new CustomerItemResponse
            {
                Key = c.OrganisationPrefix,
                OrganisationNumber = c.OrganisationNumber,
                PriceListType = c.PriceListType.GetCustomName(),
                Name = c.Name,
                TravelCostAgreementType = c.TravelCostAgreementType.GetCustomName(),
                UseSelfInvoicingInterpreter = _cacheService.CustomerSettings.Any(cs => cs.CustomerOrganisationId == c.CustomerOrganisationId && cs.UsedCustomerSettingTypes.Any(cst => cst == CustomerSettingType.UseSelfInvoicingInterpreter)),
                Description = c.ParentCustomerOrganisationId != null ? $"Organiserad under {c.ParentCustomerOrganisation.Name}" : null
            }));
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> PriceListTypes()
        {
            return DescriptionsAsJson<PriceListType>();
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> TravelCostAgreementTypes()
        {
            return DescriptionsAsJson<TravelCostAgreementType>();
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> PriceRowTypes()
        {
            return DescriptionsAsJson<PriceRowType>();
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> LocationTypes()
        {
            return DescriptionsAsJson<InterpreterLocation>();
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> RequirementTypes()
        {
            return DescriptionsAsJson<RequirementType>();
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> InterpreterInformationTypes()
        {
            return DescriptionsAsJson<InterpreterInformationType>();
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> TaxCardTypes()
        {
            return DescriptionsAsJson<TaxCardType>();
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> RequestUpdateTypes()
        {
            return DescriptionsAsJson<OrderChangeLogType>();
        }

        [HttpGet]
        [Authorize(Policies.Broker)]
        public async Task<ActionResult<IEnumerable<ListItemResponse>>> BrokerInterpreters()
        {
            return Ok(new BrokerInterpretersResponse
            {
                Interpreters = await _dbContext.InterpreterBrokers
                .Where(i => i.BrokerId == User.TryGetBrokerId())
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
                }).ToListAsync()
            });
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> ComplaintTypes()
        {
            return DescriptionsAsJson<ComplaintType>();
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> RequestStatuses()
        {
            return DescriptionsAsJson<RequestStatus>();
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> ComplaintStatuses()
        {
            return DescriptionsAsJson<ComplaintStatus>();
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> RequisitionStatuses()
        {
            return DescriptionsAsJson<RequisitionStatus>();
        }

        [HttpGet]
        public ActionResult<IEnumerable<ListItemResponse>> ErrorCodes()
        {
            return Ok(TolkApiOptions.BrokerApiErrorResponses.Union(TolkApiOptions.CommonErrorResponses).Select(d => d));
        }

        private ActionResult<IEnumerable<ListItemResponse>> DescriptionsAsJson<T>()
        {
            return Ok(EnumHelper.GetAllFullDescriptions<T>().Select(d =>
            new ListItemResponse
            {
                Key = d.CustomName,
                Description = d.Description
            }));
        }
    }
}
