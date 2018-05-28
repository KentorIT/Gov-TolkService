using Microsoft.Extensions.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Data;

namespace Tolk.BusinessLogic.Services
{
    public class OrderService
    {
        private TolkDbContext _tolkDbContext;
        private ISystemClock _clock;

        public OrderService(
            TolkDbContext tolkDbContext,
            ISystemClock clock)
        {
            _tolkDbContext = tolkDbContext;
            _clock = clock;
        }
        
        public void HandleExpiredRequests()
        {
            var expiredRequests = _tolkDbContext.Requests
                .Where(r => r.ExpiresAt <= )
        }
    }
}
