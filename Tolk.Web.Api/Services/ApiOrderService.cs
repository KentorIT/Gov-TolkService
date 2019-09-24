using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Tolk.Api.Payloads.ApiPayloads;
using Tolk.Api.Payloads.Enums;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Api.Exceptions;
using Tolk.Web.Api.Helpers;

namespace Tolk.Web.Api.Services
{
    public class ApiOrderService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ILogger _logger;

        public ApiOrderService(TolkDbContext dbContext, ILogger<ApiOrderService> logger)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<Order> GetOrderAsync(string orderNumber, int brokerId)
        {
            var order = await _dbContext.Orders
                .Include(o => o.Requests).ThenInclude(r => r.Ranking)
                .SingleOrDefaultAsync(o => o.OrderNumber == orderNumber &&
                //Must have a request connected to the order for the broker, any status...
                o.Requests.Any(r => r.Ranking.BrokerId == brokerId));
            if (order == null)
            {
                throw new InvalidApiCallException(ErrorCodes.ORDER_NOT_FOUND);
            }
            return order;
        }
    }
}
