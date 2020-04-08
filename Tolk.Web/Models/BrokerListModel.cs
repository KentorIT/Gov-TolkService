using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class BrokerListModel
    {
        public string BrokerName { get; set; }

        public RequestStatus Status { get; set; }

        public string DenyMessage { get; set; }

        internal static async Task<IEnumerable<BrokerListModel>> GetFromList(IQueryable<Request> requests)
            => await requests.Select(r => new BrokerListModel
            {
                Status = r.Status,
                BrokerName = r.Ranking.Broker.Name,
                DenyMessage = r.DenyMessage,
            }).ToListAsync();
    }
}