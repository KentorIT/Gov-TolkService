using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Models.OrderAgreement;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class OrderAgreementService
    {
        private readonly ILogger<OrderAgreementService> _logger;
        private readonly ISwedishClock _clock;
        private readonly TolkDbContext _tolkDbContext;

        public OrderAgreementService(
            TolkDbContext tolkDbContext,
            ILogger<OrderAgreementService> logger,
            ISwedishClock clock)
        {
            _tolkDbContext = tolkDbContext;
            _logger = logger;
            _clock = clock;
        }

        public async Task<string> CreateOrderAgreementFromRequisition(int requisitionId, StreamWriter writer, int? previousOrderAgreementIndex = null)
        {
            _logger.LogInformation("Start serializing order agreement from {requisitionId}.", requisitionId);

            var requisition = await _tolkDbContext.Requisitions.GetRequisitionForAgreement(requisitionId);
            var model = new OrderAgreementModel(requisition, _clock.SwedenNow, _tolkDbContext.RequisitionPriceRows.GetPriceRowsForRequisition(requisitionId).ToList(), previousOrderAgreementIndex);
            SerializeModel(model, writer);
            _logger.LogInformation("Finished serializing order agreement from {requisitionId}.", requisitionId);
            return model.ID.Value;
        }

        public async Task<string> CreateOrderAgreementFromRequest(int requestId, StreamWriter writer)
        {
            _logger.LogInformation("Start serializing order agreement from {requestId}.", requestId);

            var request = await _tolkDbContext.Requests.GetRequestForAgreement(requestId);
            var model = new OrderAgreementModel(request, _clock.SwedenNow, _tolkDbContext.RequestPriceRows.GetPriceRowsForRequest(requestId).ToList());
            SerializeModel(model, writer);
            _logger.LogInformation("Finished serializing order agreement from {requestId}.", requestId);
            return model.ID.Value;
        }

        private static void SerializeModel(OrderAgreementModel model, StreamWriter writer)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add(nameof(Constants.cac), Constants.cac);
            ns.Add(nameof(Constants.cbc), Constants.cbc);
            XmlSerializer xser = new XmlSerializer(typeof(OrderAgreementModel), Constants.defaultNamespace);
            xser.Serialize(writer, model, ns);
        }
    }
}