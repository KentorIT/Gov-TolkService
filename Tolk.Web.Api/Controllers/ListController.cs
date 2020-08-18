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
using NSwag.Annotations;

namespace Tolk.Web.Api.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    [Authorize]
    public class ListController : ControllerBase
    {
        private readonly TolkDbContext _dbContext;
        private readonly CacheService _cacheService;

        public ListController(TolkDbContext tolkDbContext, CacheService cacheService)
        {
            _dbContext = tolkDbContext;
            _cacheService = cacheService;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på typer av tolkning som kan beställas")]
        [OpenApiTag("List", AddToDocument = true, Description = "Grupp av metoder som returnerar de listor på olika saker som behövs i systemet")]
        public ActionResult<IEnumerable<ListItemResponse>> AssignmentTypes()
        {
            return DescriptionsAsJson<AssignmentType>();
        }
        
        [HttpGet]
        [AllowAnonymous]
        [OpenApiIgnore] //Not applicable for broker api
        public ActionResult<IEnumerable<ListItemResponse>> AllowExceedingTravelCostTypes()
        {
            return DescriptionsAsJson<AllowExceedingTravelCost>();
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på godkända kompetensnivåer för en tolk")]
        [OpenApiTag("List")]
        public ActionResult<IEnumerable<ListItemResponse>> CompetenceLevels()
        {
            return DescriptionsAsJson<CompetenceAndSpecialistLevel>();
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på de språk som kan avropas genom systemet")]
        [OpenApiTag("List")]
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
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på de regioner som avrop kan ske från")]
        [OpenApiTag("List")]
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
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<CustomerItemResponse>))]
        [Description("Returnerar en lista på de myndigheter som avropar tolk genom systemet")]
        [OpenApiTag("List")]
        public async Task<ActionResult<IEnumerable<CustomerItemResponse>>> Customers()
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
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på de prislistor som hanteras i systemet")]
        [OpenApiTag("List")]
        public ActionResult<IEnumerable<ListItemResponse>> PriceListTypes()
        {
            return DescriptionsAsJson<PriceListType>();
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på de sätt myndigheter kan beräkna reskostnadsersättning")]
        [OpenApiTag("List")]
        public ActionResult<IEnumerable<ListItemResponse>> TravelCostAgreementTypes()
        {
            return DescriptionsAsJson<TravelCostAgreementType>();
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på prisradstyper som hanteras i systemet")]
        [OpenApiTag("List")]
        public ActionResult<IEnumerable<ListItemResponse>> PriceRowTypes()
        {
            return DescriptionsAsJson<PriceRowType>();
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på inställelsesätt för tolkar som hanteras i systemet")]
        [OpenApiTag("List")]
        public ActionResult<IEnumerable<ListItemResponse>> LocationTypes()
        {
            return DescriptionsAsJson<InterpreterLocation>();
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på de tillkommande krav/önskemål som hanteras i systemet")]
        [OpenApiTag("List")]
        public ActionResult<IEnumerable<ListItemResponse>> RequirementTypes()
        {
            return DescriptionsAsJson<RequirementType>();
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på typer av informationsmängder kopplat till tolkar")]
        [OpenApiTag("List")]
        public ActionResult<IEnumerable<ListItemResponse>> InterpreterInformationTypes()
        {
            return DescriptionsAsJson<InterpreterInformationType>();
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på typer av skattsedlar som hanteras vid skapande av rekvisition i systemet")]
        [OpenApiTag("List")]
        public ActionResult<IEnumerable<ListItemResponse>> TaxCardTypes()
        {
            return DescriptionsAsJson<TaxCardType>();
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på ändringar som kan ske på ett avrop")]
        [OpenApiTag("List")]
        public ActionResult<IEnumerable<ListItemResponse>> RequestUpdateTypes()
        {
            return DescriptionsAsJson<OrderChangeLogType>();
        }

        [HttpGet]
        [Authorize(Policies.Broker)]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på de tolkar som är registerade för den anropande förmedlingen")]
        [OpenApiTag("List")]
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
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på typer av reklamationer som kan registreras på ett uppdrag")]
        [OpenApiTag("List")]
        public ActionResult<IEnumerable<ListItemResponse>> ComplaintTypes()
        {
            return DescriptionsAsJson<ComplaintType>();
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på de tillstånd ett avrop kan befinna sig i")]
        [OpenApiTag("List")]
        public ActionResult<IEnumerable<ListItemResponse>> RequestStatuses()
        {
            return DescriptionsAsJson<RequestStatus>();
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på de tillstånd en reklamation kan befinna sig i")]
        [OpenApiTag("List")]
        public ActionResult<IEnumerable<ListItemResponse>> ComplaintStatuses()
        {
            return DescriptionsAsJson<ComplaintStatus>();
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ListItemResponse>))]
        [Description("Returnerar en lista på de tillstånd en rekvisition kan befinna sig i")]
        [OpenApiTag("List")]
        public ActionResult<IEnumerable<ListItemResponse>> RequisitionStatuses()
        {
            return DescriptionsAsJson<RequisitionStatus>();
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ErrorResponse>))]
        [Description("Returnerar en lista på de felkoder som kan returneras av systemet")]
        [OpenApiTag("List")]
        public ActionResult<IEnumerable<ErrorResponse>> ErrorCodes()
        {
            return Ok(TolkApiOptions.BrokerApiErrorResponses.Union(TolkApiOptions.CommonErrorResponses).Select(d => d));
        }

        [HttpGet]
        [OpenApiIgnore]
        public ActionResult<IEnumerable<ListItemResponse>> CustomerErrorCodes()
        {
            return Ok(TolkApiOptions.CustomerApiErrorResponses.Union(TolkApiOptions.CommonErrorResponses).Select(d => d));
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
