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

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.Broker)]
    public class RequestController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;

        public RequestController(TolkDbContext dbContext, UserManager<AspNetUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        protected int CurrentBrokerId
        {
            get
            {
                return int.Parse(User.Claims.Single(c => c.Type == TolkClaimTypes.BrokerId).Value);
            }
        }

        public IActionResult List()
        {
            return View(_dbContext.Requests.Include(r => r.Order)
                .Where(r => (r.Status == RequestStatus.Created || r.Status == RequestStatus.Received) &&
                    r.Ranking.BrokerRegion.Broker.BrokerId == CurrentBrokerId).Select(r => new RequestListItemModel
                        {
                            RequestId = r.RequestId,
                            Language = r.Order.Language.Name,
                            OrderNumber = r.Order.OrderNumber.ToString(),
                            CustomerName = r.Order.CustomerOrganisation.Name,
                            RegionName = r.Order.Region.Name,
                            Start = r.Order.StartDateTime,
                            End = r.Order.EndDateTime,
                        }));
        }

        public IActionResult Edit(int id)
        {
            var request = _dbContext.Requests.Include(r => r.Order).Single(o => o.RequestId == id);
            if (request.Status == RequestStatus.Created)
            {
                request.Status = RequestStatus.Received;
                //Set modified user, date and possible impersonator
                request.ModifiedDate = DateTimeOffset.Now;
                request.ModifiedBy = _userManager.GetUserId(User);
                request.ImpersonatingModifier = User.FindFirstValue(TolkClaimTypes.ImpersonatingUserId);
                _dbContext.SaveChanges();
            }
            //Get request model from db
            var model = RequestModel.GetModelFromRequest(request);
            model.BrokerId = CurrentBrokerId;
            return View(model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Edit(RequestModel model)
        {
            if (ModelState.IsValid)
            {
                //TODO: VERIFY THAT THE REQUEST IS CONNECTED TO THE USER'S BROKER!!
                var request = _dbContext.Requests.Include(r => r.Order)
                    .Single(o => o.RequestId == model.RequestId);
                request.Status = model.SetStatus;
                //TODO:Fix better offset-check!!
                request.ModifiedDate = DateTimeOffset.Now;
                request.ModifiedBy = _userManager.GetUserId(User);
                request.ImpersonatingModifier = User.FindFirstValue(TolkClaimTypes.ImpersonatingUserId);
                request.InterpreterId = model.InterpreterId;
                //TODO: This should differ depending on the incoming status.
                request.Order.Status = OrderStatus.RequestResponded;
                _dbContext.SaveChanges();

                return Redirect($"~/Home/Index?message=Svar har skickats");
            }
            return View("Edit", model);
        }
    }
}
