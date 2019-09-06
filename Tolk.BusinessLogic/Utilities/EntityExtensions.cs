using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Entities;

namespace Tolk.BusinessLogic.Utilities
{
    public static class EntityExtensions
    {
        public static IQueryable<Order> CustomerOrders(this IQueryable<Order> orders, int customerOrganisationId, int userId, IEnumerable<int> customerUnits, bool isCentralAdminOrOrderHandler = false, bool includeContact = false)
        {
            var filteredOrders = orders.Where(o => o.CustomerOrganisationId == customerOrganisationId);
            return isCentralAdminOrOrderHandler ? filteredOrders :
                filteredOrders.Where(o => ((o.CreatedBy == userId || (includeContact && o.ContactPersonId == userId)) && o.CustomerUnitId == null) ||
                    customerUnits.Contains(o.CustomerUnitId.Value));
        }
    }
}
