using AutoMapper;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.Controllers
{
    [Authorize(Policy = Policies.CustomerOrAdmin)]
    public class OrderGroupController : Controller
    {
        private readonly TolkDbContext _dbContext;
        private readonly PriceCalculationService _priceCalculationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly OrderService _orderService;
        private readonly DateCalculationService _dateCalculationService;
        private readonly ISwedishClock _clock;
        private readonly ILogger _logger;
        private readonly TolkOptions _options;
        private readonly INotificationService _notificationService;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly IMapper _mapper;

        public OrderGroupController(
            TolkDbContext dbContext,
            PriceCalculationService priceCalculationService,
            IAuthorizationService authorizationService,
            OrderService orderService,
            DateCalculationService dateCalculationService,
            ISwedishClock clock,
            ILogger<OrderController> logger,
            IOptions<TolkOptions> options,
            INotificationService notificationService,
            UserManager<AspNetUser> usermanager,
            IMapper mapper
            )
        {
            _dbContext = dbContext;
            _priceCalculationService = priceCalculationService;
            _authorizationService = authorizationService;
            _orderService = orderService;
            _dateCalculationService = dateCalculationService;
            _clock = clock;
            _logger = logger;
            _options = options.Value;
            _notificationService = notificationService;
            _userManager = usermanager;
            _mapper = mapper;
        }

        public async Task<IActionResult> View(int id, string message = null, string errorMessage = null)
        {
            //Get order model from db
            OrderGroup orderGroup = await GetOrderGroup(id);

            if ((await _authorizationService.AuthorizeAsync(User, orderGroup, Policies.View)).Succeeded)
            {
                return View(OrderGroupModel.GetModelFromOrderGroup(orderGroup));
            }
            return Forbid();
        }

        private async Task<OrderGroup> GetOrderGroup(int id)
        {
            return await _dbContext.OrderGroups
                .Include(o => o.Orders).ThenInclude(o => o.PriceRows).ThenInclude(p => p.PriceListRow)
                .SingleAsync(o => o.OrderGroupId == id);
        }
    }
}
