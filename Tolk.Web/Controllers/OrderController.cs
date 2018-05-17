using Microsoft.AspNetCore.Mvc;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;
using System.Security.Claims;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Services;
using Microsoft.EntityFrameworkCore;
using Tolk.BusinessLogic.Utilities;
using System.Collections.Generic;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.Customer)]
    public class OrderController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;

        public OrderController(TolkDbContext dbContext, UserManager<AspNetUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        #region possible moves to base controller

        protected string CurrentUserId
        {
            get
            {
                return _userManager.GetUserId(User);
            }
        }

        protected int CurrentCustomerOrgansationId
        {
            get
            {
                return int.Parse(User.Claims.Single(c => c.Type == TolkClaimTypes.CustomerOrganisationId).Value);
            }
        }

        #endregion

        public IActionResult List()
        {
            return View(_dbContext.Orders.Include(o => o.Language).Include(o => o.Region)
                .Where(r => r.CreatedBy == CurrentUserId && r.CustomerOrganisationId == CurrentCustomerOrgansationId)
                .Select(o => new OrderListItemModel
                {
                    OrderId = o.OrderId,
                    Language = o.Language.Name,
                    OrderNumber = o.OrderNumber.ToString(),
                    RegionName = o.Region.Name,
                    Start = o.StartDateTime,
                    End = o.EndDateTime,
                    Status = o.Status
                }));
        }

        public IActionResult Details(int id)
        {
            //Get order model from db
            var order = _dbContext.Orders
                .Include(o => o.CreatedByUser)
                .Include(o => o.Region)
                .Include(o => o.CustomerOrganisation)
                .Include(o => o.Language)
                .Include(o => o.Requests)
                .ThenInclude(r => r.Ranking)
                .ThenInclude(r => r.BrokerRegion)
                .ThenInclude(r => r.Broker)
                .Single(o => o.OrderId == id);
            var competenceLevel = EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>(order.RequiredCompetenceLevel);
            var listType = order.CustomerOrganisation.PriceListType;
            //TODO: Handle if there are no rows due to start/end date restrictions. Should take the one with the latest end date in that case.
            decimal extraTimePrice = 0;
            var extraCosts = GetMinutesPerPriceType(order.StartDateTime, order.EndDateTime);
            //TODO: get the rows for the list type, startdate and competence first, to get one call to db...
            var price = _dbContext.PriceListRows
                .OrderBy(r => r.MaxMinutes)
                .First(r =>
                    r.CompetenceLevel == competenceLevel &&
                    r.PriceListType == listType &&
                    r.PriceRowType == PriceRowType.BasePrice &&
                    r.StartDate <= order.StartDateTime.DateTime && r.EndDate >= order.StartDateTime.DateTime &&
                    r.MaxMinutes >= extraCosts.Single(c => c.PriceRowType == PriceRowType.BasePrice).Minutes).Price;
            foreach (var priceTime in extraCosts.Where(c => c.Minutes > 0 && c.PriceRowType != PriceRowType.BasePrice))
            {
                var extraPriceInfo = _dbContext.PriceListRows
                    .Single(r =>
                        r.CompetenceLevel == competenceLevel &&
                        r.PriceListType == listType &&
                        r.PriceRowType == priceTime.PriceRowType &&
                        r.StartDate <= order.StartDateTime.DateTime && r.EndDate >= order.StartDateTime.DateTime);
                int n = extraCosts.Single(c => c.PriceRowType == priceTime.PriceRowType).Minutes / extraPriceInfo.MaxMinutes;
                extraTimePrice += n * extraPriceInfo.Price;
            }
            var model = OrderModel.GetModelFromOrder(order);
            //TODO: Handle this better. Preferably with a list that you can use contains on
            var rank = order.Requests.Single(r =>
                r.Status == RequestStatus.Created ||
                r.Status == RequestStatus.Received ||
                r.Status == RequestStatus.Accepted ||
                r.Status == RequestStatus.SentToInterpreter ||
                r.Status == RequestStatus.Approved
                ).Ranking;
            model.CalculatedPrice = (price + extraTimePrice) * rank.BrokerFee;
            model.BrokerName = rank.BrokerRegion.Broker.Name;
            return View("Details", model);
        }

        public IActionResult Edit(int id)
        {
            var order = _dbContext.Orders.Single(o => o.OrderId == id);
            //Get order model from db
            return View(OrderModel.GetModelFromOrder(order));
        }

        public IActionResult Add()
        {
            return View("Edit");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Add(OrderModel model)
        {
            if (ModelState.IsValid)
            {
                Order order;
                if (model.OrderId.HasValue)
                {
                    order = _dbContext.Orders.Single(o => o.OrderId == model.OrderId);
                    //Modified by, Modified date, impersonator modifier
                }
                else
                {
                    order = new Order
                    {
                        //Hardcodes
                        RequiredInterpreterLocation = 1,
                        Status = OrderStatus.Requested,
                        CreatedBy = _userManager.GetUserId(User),
                        CreatedDate = DateTime.Now,
                        CustomerOrganisationId = CurrentCustomerOrgansationId,
                        ImpersonatingCreator = User.FindFirstValue(TolkClaimTypes.ImpersonatingUserId)
                    };
                }

                order = model.UpdateOrder(order);
                if (!model.OrderId.HasValue)
                {
                    Order.CreateRequest(_dbContext, order);
                }
                _dbContext.SaveChanges();

                //TODO: If this is a edit, something else should happen to the requests in some way...
                return Details(order.OrderId);
            }
            return View("Edit", model);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Edit(OrderModel model)
        {
            return Add(model);
        }

        private IEnumerable<PriceTime> GetMinutesPerPriceType(DateTimeOffset startDateTime, DateTimeOffset endDateTime)
        {
            int maxMinutes = 330;
            //TODO: Has no calculation for övertid
            TimeSpan span = endDateTime - startDateTime;
            int totalMinutes = (int)span.TotalMinutes;
            int extraMinutes = 0;
            if (totalMinutes > maxMinutes)
            {
                extraMinutes = totalMinutes - maxMinutes;
                totalMinutes = maxMinutes;
            }
            yield return new PriceTime { Minutes = totalMinutes, PriceRowType = PriceRowType.BasePrice };
            yield return new PriceTime { Minutes = extraMinutes, PriceRowType = PriceRowType.PriceOverMaxTime };
            //TODO: Add Check for Big holidays
            yield return new PriceTime { Minutes = 0, PriceRowType = PriceRowType.BigHolidayWeekendIWH };
            //TODO: Get number of minutes during weekend, that is not in BigHolidayWeekendIWH.
            yield return new PriceTime { Minutes = 0, PriceRowType = PriceRowType.WeekendIWH };
            //TODO: Get number of minutes before 07:00 and after 18:00, that is not in BigHolidayWeekendIWH.
            yield return new PriceTime { Minutes = 0, PriceRowType = PriceRowType.InconvenientWorkingHours };
        }
    }

    public class PriceTime
    {
        public int Minutes { get; set; }
        public PriceRowType PriceRowType { get; set; }
    }
}
