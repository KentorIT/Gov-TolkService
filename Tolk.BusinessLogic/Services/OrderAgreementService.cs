using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Models.OrderAgreement;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class OrderAgreementService
    {
        private readonly ILogger<OrderAgreementService> _logger;
        private readonly ISwedishClock _clock;
        private readonly TolkDbContext _tolkDbContext;
        private readonly CacheService _cacheService;
        private readonly ITolkBaseOptions _options;
        private readonly DateCalculationService _dateCalculationService;
        private readonly EmailService _emailService;

        public OrderAgreementService(
            TolkDbContext tolkDbContext,
            ILogger<OrderAgreementService> logger,
            ISwedishClock clock,
            CacheService cacheService,
            DateCalculationService dateCalculationService,
            ITolkBaseOptions options,
            EmailService emailService
            )
        {
            _tolkDbContext = tolkDbContext;
            _logger = logger;
            _clock = clock;
            _cacheService = cacheService;
            _dateCalculationService = dateCalculationService;
            _options = options;
            _emailService = emailService;
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

        public async Task<string> CreateOrderAgreementFromRequest(int requestId, StreamWriter writer, int? previousOrderAgreementIndex = null)
        {
            _logger.LogInformation("Start serializing order agreement from {requestId}.", requestId);

            var request = await _tolkDbContext.Requests.GetRequestForAgreement(requestId);
            var model = new OrderAgreementModel(request, _clock.SwedenNow, _tolkDbContext.RequestPriceRows.GetPriceRowsForRequest(requestId).ToList());
            SerializeModel(model, writer);
            _logger.LogInformation("Finished serializing order agreement from {requestId}.", requestId);
            return model.ID.Value;
        }

        public async Task<bool> HandleOrderAgreementCreation()
        {
            if (!_dateCalculationService.IsWorkingDay(_clock.SwedenNow.UtcDateTime))
            {
                return false;
            }
            var startAtSettings = _cacheService.OrganisationNotificationSettings
                .Where(n => n.NotificationType == NotificationType.OrderAgreementCreated)
                .Select(n => new { n.ReceivingOrganisationId, n.StartUsingNotificationAt }).ToList();
            var orderAgreementCustomerIds = _cacheService.CustomerSettings
                .Where(c => c.UsedCustomerSettingTypes.Contains(CustomerSettingType.UseOrderAgreements))
                .Select(c => c.CustomerOrganisationId).ToList();

            var occasionsEndedAtOrBefore = _dateCalculationService.GetDateForANumberOfWorkdaysAgo(_clock.SwedenNow.UtcDateTime, _options.WorkDaysGracePeriodBeforeOrderAgreementCreation);
            //1. find all nonhandled requests and requisitions for customers that is set as using order agreements
            // a. When requisition is Approved or Reviewed date is irrelevant
            foreach (int customerOrganisationId in orderAgreementCustomerIds)
            {
                var validUseFrom = startAtSettings.SingleOrDefault(s => s.ReceivingOrganisationId == customerOrganisationId)?.StartUsingNotificationAt ??
                    new DateTime(1900,1,1);
                //MOVE GETTER TO EXTENSIONS
                var baseInformationForOrderAgreementsToCreate = await _tolkDbContext.Requisitions
                    .GetRequisitionsForOrderAgreementCreation(customerOrganisationId, validUseFrom)
                    .Select(r => new OrderAgreementIdentifierModel { RequisitionId = r.RequisitionId, RequestId = r.RequestId })
                    .ToListAsync();

                //b.When requisition is AutomaticGeneratedFromCancelledOrder
                baseInformationForOrderAgreementsToCreate.AddRange(await _tolkDbContext.Requisitions
                    .GetRequisitionsFromCancellation(customerOrganisationId, validUseFrom)
                    .Select(r => new OrderAgreementIdentifierModel { RequisitionId = r.RequisitionId, RequestId = r.RequestId })
                    .ToListAsync());

                // c. x workdays after the occasion was ended
                //   - If there is a requisition created, use that, but only if there is no order agreement created on the request before.
                baseInformationForOrderAgreementsToCreate.AddRange(await _tolkDbContext.Requisitions.Where(r =>
                    (r.Status == RequisitionStatus.Created) &&
                    r.Request.Order.CustomerOrganisationId == customerOrganisationId &&
                    r.OrderAgreementPayload == null &&
                    !r.Request.OrderAgreementPayloads.Any() &&
                    r.Request.Order.EndAt < occasionsEndedAtOrBefore &&
                    r.Request.Order.EndAt > validUseFrom
                    )
                    .Select(r => new OrderAgreementIdentifierModel { RequisitionId = r.RequisitionId, RequestId = r.RequestId })
                    .ToListAsync());

                //d. x workdays after the occasion was ended
                //   - If there is a no requisition created, use request.
                baseInformationForOrderAgreementsToCreate.AddRange(await _tolkDbContext.Requests.Where(r =>
                    (r.Status == RequestStatus.Delivered || r.Status == RequestStatus.Approved) &&
                    r.Order.CustomerOrganisationId == customerOrganisationId &&
                    !r.OrderAgreementPayloads.Any() &&
                    !r.Requisitions.Any() &&
                    r.Order.EndAt < occasionsEndedAtOrBefore &&
                    r.Order.EndAt > validUseFrom
                    )
                    .Select(r => new OrderAgreementIdentifierModel { RequestId = r.RequestId })
                    .ToListAsync());

                _logger.LogInformation("For customer {customerOrganisationId}:  Found {count} requests to create order agreements for: {requestIds}",
                    customerOrganisationId, baseInformationForOrderAgreementsToCreate.Count, string.Join(", ", baseInformationForOrderAgreementsToCreate.Select(r => r.RequestId)));

                foreach (var entity in baseInformationForOrderAgreementsToCreate)
                {
                    try
                    {
                        if (entity.RequisitionId.HasValue)
                        {
                            await CreateAndStoreOrderAgreementPayload(entity, CreateOrderAgreementFromRequisition);
                        }
                        else
                        {
                            await CreateAndStoreOrderAgreementPayload(entity, CreateOrderAgreementFromRequest);
                        }
                        await _tolkDbContext.SaveChangesAsync();
                        _logger.LogInformation("Processing completed order agreement for {requestId}.", entity.RequestId);

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failure processing order agreement creation for {requestId}", entity.RequestId);
                        await SendErrorMail(nameof(HandleOrderAgreementCreation), ex);
                    }
                }
            }
            return true;
        }

        public async Task CreateAndStoreOrderAgreementPayload(OrderAgreementIdentifierModel entity, Func<int, StreamWriter, int?, Task<string>> payloadCreator)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);

            var previousIndex = (await _tolkDbContext.OrderAgreementPayloads.Where(r => r.RequestId == entity.RequestId).OrderByDescending(p => p.Index).FirstOrDefaultAsync())?.Index;
            string payloadIdentifier = await payloadCreator(entity.CreateById, writer, previousIndex);
            memoryStream.Position = 0;
            byte[] byteArray = new byte[memoryStream.Length];
            memoryStream.Read(byteArray, 0, (int)memoryStream.Length);
            memoryStream.Close();
            //Break out to method, to here. Returns a OrderAgreementPayload?

            var index = previousIndex.HasValue ? previousIndex.Value + 1 : 1;
            //Save it to db
            var payload = new OrderAgreementPayload
            {
                RequestId = entity.RequestId,
                Payload = byteArray,
                RequisitionId = entity.RequisitionId,
                CreatedAt = _clock.SwedenNow,
                Index = index,
                IdentificationNumber = payloadIdentifier
            };
            _tolkDbContext.OrderAgreementPayloads.Add(payload);
            if (previousIndex.HasValue)
            {
                var previous = await _tolkDbContext.OrderAgreementPayloads.SingleAsync(p => p.RequestId == entity.RequestId && p.Index == previousIndex);
                previous.ReplacedByPayload = payload;
            }

        }

        private async Task SendErrorMail(string methodname, Exception ex)
        {
            await _emailService.SendErrorEmail(nameof(OrderAgreementService), methodname, ex);
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