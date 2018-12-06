using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Helpers;

namespace Tolk.Web.Api.Controllers
{
    public class ListController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly TolkApiOptions _options;

        public ListController(TolkDbContext tolkDbContext, IOptions<TolkApiOptions> options)
        {
            _dbContext = tolkDbContext;
            _options = options.Value;
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
    }
}
