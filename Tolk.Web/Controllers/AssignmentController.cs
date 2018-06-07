using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Services;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using System.Threading.Tasks;

namespace Tolk.Web.Controllers
{
    public class AssignmentController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly IAuthorizationService _authorizationService;

        public AssignmentController(TolkDbContext dbContext,
            UserManager<AspNetUser> userManager,
            IAuthorizationService authorizationService)

        {
            _dbContext = dbContext;
            _userManager = userManager;
            _authorizationService = authorizationService;
        }

        public IActionResult List()
        {
            var requests = _dbContext.Requests.Include(r => r.Order).Where(r => r.Status == RequestStatus.Approved);
            // The list of Requests should differ, if the user is an interpreter, or is a broker-user.
            var interpreterId = User.TryGetInterpreterId();
            var brokerId = User.TryGetBrokerId();
            if (interpreterId.HasValue)
            {
                requests = requests.Where(r => r.InterpreterId == interpreterId);
            }
            if (brokerId.HasValue)
            {
                requests = requests.Where(r => r.Ranking.BrokerId == brokerId);
            }
            return View(requests.Select(r => new RequestListItemModel
            {
                RequestId = r.RequestId,
                Language = r.Order.Language.Name,
                OrderNumber = r.Order.OrderNumber.ToString(),
                CustomerName = r.Order.CustomerOrganisation.Name,
                RegionName = r.Order.Region.Name,
                Start = r.Order.StartDateTime,
                End = r.Order.EndDateTime,
                Status = r.Status
            }));
        }

        public async Task<IActionResult> View(int id)
        {
            var request = _dbContext.Requests
                    .Include(r => r.Requisitions)
                    .Include(r => r.Order).ThenInclude(o => o.CustomerOrganisation)
                    .Include(r => r.Order).ThenInclude(o => o.Language)
                    .Include(r => r.Ranking).ThenInclude(o => o.BrokerRegion).ThenInclude(br => br.Broker)
                    .Where(r => r.RequestId == id).Single();
            if ((await _authorizationService.AuthorizeAsync(User, request, Policies.View)).Succeeded)
            {
                return View(AssignmentModel.GetModelFromRequest(request));
            }
            return Forbid();
        }
    }
}
