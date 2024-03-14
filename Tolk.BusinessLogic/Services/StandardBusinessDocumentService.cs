using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Models.Invoice;
using Tolk.BusinessLogic.Models.OrderAgreement;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class StandardBusinessDocumentService
    {
        private readonly ILogger<StandardBusinessDocumentService> _logger;
        private readonly ISwedishClock _clock;
        private readonly TolkDbContext _tolkDbContext;
        private readonly CacheService _cacheService;
        private readonly ITolkBaseOptions _options;
        private readonly DateCalculationService _dateCalculationService;
        private readonly EmailService _emailService;

        public StandardBusinessDocumentService(
            ILogger<StandardBusinessDocumentService> logger,
            ISwedishClock clock,
            TolkDbContext tolkDbContext, 
            CacheService cacheService, 
            ITolkBaseOptions options, 
            DateCalculationService dateCalculationService, 
            EmailService emailService)
        {
            _logger = logger;
            _clock = clock;
            _tolkDbContext = tolkDbContext;
            _cacheService = cacheService;
            _options = options;
            _dateCalculationService = dateCalculationService;
            _emailService = emailService;
        }       

        private PeppolPayload CreateOrderAgreement(Request request)
        {
            // Check if OA should be created from Requisition (Cancelled Order)
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            
            _logger.LogInformation("Start serializing order agreement from {requestId}.", request.RequestId);
            OrderAgreementModel model;
            if(request.CurrentlyActiveRequisition != null)
            {
                model = new OrderAgreementModel(request.CurrentlyActiveRequisition, _clock.SwedenNow, _tolkDbContext.RequisitionPriceRows.GetPriceRowsForRequisition(request.CurrentlyActiveRequisition.RequisitionId).ToList());
            }
            else
            {
                model = new OrderAgreementModel(request, _clock.SwedenNow, _tolkDbContext.RequestPriceRows.GetPriceRowsForRequest(request.RequestId).ToList());
            }
            return SerializeAndCreatePeppolPayload<OrderAgreementModel>(request, model,PeppolMessageType.OrderAgreement);           
        }

        public async Task<PeppolPayload> CreateAndStoreStandardDocument(int requestId)
        {
            var request = await _tolkDbContext.Requests.GetRequestForPeppolMessageCreation(requestId);
            PeppolPayload peppolPayload = null;
            // Check if there exists OrderAgreement or not                     
            if (!request.Order.PeppolPayloads.Any())
            {
                // If not create OA                 
                peppolPayload = CreateOrderAgreement(request);
                _tolkDbContext.PeppolPayloads.Add(peppolPayload);
            }
            else if(request.Order.CustomerOrganisation.UseOrderResponsesFromDate <= _clock.SwedenNow)
            { 
                // check if an OR exists 
                if (!request.Order.PeppolPayloads.Any(pp => pp.PeppolMessageType == PeppolMessageType.OrderResponse))
                {
                    // if not create OR                           
                    var orderAgreement = request.Order.PeppolPayloads.Single(); // Only one OA per order/request is allowed
                    var priceRows = await GetPriceRowComparisonResult(request, orderAgreement);
                    if (!priceRows.Any(pr => pr.HasChanged))
                    {
                        return orderAgreement;
                    }
                    peppolPayload = CreateOrderResponse(request, priceRows);
                    peppolPayload.ReplacingPayload = orderAgreement;
                    _tolkDbContext.Add(peppolPayload);
                    orderAgreement.ReplacedByPayload = peppolPayload;                                
                }
                else
                {
                    // else create new OR and update old ones replacedById                     
                    var latestOrderResponse = request.Order.PeppolPayloads
                        .Where(pp => pp.PeppolMessageType == PeppolMessageType.OrderResponse && pp.ReplacedById == null)
                        .SingleOrDefault();
                    var priceRows = await GetPriceRowComparisonResult(request, latestOrderResponse);
                    if (!priceRows.Any(pr => pr.HasChanged))
                    {
                        return latestOrderResponse;
                    }
                    peppolPayload = CreateOrderResponse(request, priceRows);
                    peppolPayload.ReplacingPayload = latestOrderResponse;
                    _tolkDbContext.Add(peppolPayload);
                    latestOrderResponse.ReplacedByPayload = peppolPayload;
                }
            }

            await _tolkDbContext.SaveChangesAsync();
            return peppolPayload;

        }

        private PeppolPayload CreateOrderResponse(Request request, List<PriceRowComparisonResult> priceRows)
        {                             
            OrderResponseModel model;            

            if (request.CurrentlyActiveRequisition != null)
            {
                _logger.LogInformation("Start serializing order response from {requisitionId}.", request.CurrentlyActiveRequisition.RequisitionId);
                model = new OrderResponseModel(request.CurrentlyActiveRequisition, _clock.SwedenNow, priceRows);
            }
            else
            {
                _logger.LogInformation("Start serializing order response from {requestId}.", request.RequestId);
                model = new OrderResponseModel(request, _clock.SwedenNow, priceRows);
            }
            return SerializeAndCreatePeppolPayload<OrderResponseModel>(request, model, PeppolMessageType.OrderResponse); 
        }       

        private async Task<List<PriceRowComparisonResult>> GetPriceRowComparisonResult(Request request, PeppolPayload payloadToReplace)
        {
            // Check if previous payload was based on request or requisition
            IEnumerable<PriceRowBase> previousPriceRows = payloadToReplace.RequisitionId.HasValue ?
                   await _tolkDbContext.RequisitionPriceRows.GetPriceRowsForRequisition(payloadToReplace.RequisitionId.Value).ToListAsync() :
                   await _tolkDbContext.RequestPriceRows.GetPriceRowsForRequest(payloadToReplace.RequestId).ToListAsync();
            // Get pricerows that new Request is based on            
            IEnumerable<PriceRowBase> newPriceRows = request.CurrentlyActiveRequisition != null ?
                   await _tolkDbContext.RequisitionPriceRows.GetPriceRowsForRequisition(request.CurrentlyActiveRequisition.RequisitionId).ToListAsync() :
                   await _tolkDbContext.RequestPriceRows.GetPriceRowsForRequest(request.RequestId).ToListAsync();

            var comparisonResult = new List<PriceRowComparisonResult>();

            if(newPriceRows.Count() == 0)
            {
                _logger.LogInformation("Error, no new priceRows where found for replacing request: {requestId}, request that was replaced {requestId} ", request.RequestId,payloadToReplace.RequestId);
                var articles = (InvoiceableArticle[])Enum.GetValues(typeof(InvoiceableArticle));
                comparisonResult = articles.Select(a => new PriceRowComparisonResult
                {
                    ArticleType = a,
                    TotalPrice = newPriceRows
                                  .Where(npr => EnumHelper.Parent<PriceRowType, InvoiceableArticle>(npr.PriceRowType) == a)
                                  .Sum(npr => npr.TotalPrice),
                    HasChanged = false
                }).ToList();

                return comparisonResult;
            }

            // Compare new priceRows with Old Pricerows
            foreach (var article in (InvoiceableArticle[])Enum.GetValues(typeof(InvoiceableArticle)))
            {
                // Rounded price is used to enable comparison
                var previousPrice = previousPriceRows
                                       .Where(ppr => EnumHelper.Parent<PriceRowType, InvoiceableArticle>(ppr.PriceRowType) == article)
                                       .Sum(ppr => ppr.RoundedTotalPrice);
                var newPrice = newPriceRows
                                  .Where(npr => EnumHelper.Parent<PriceRowType, InvoiceableArticle>(npr.PriceRowType) == article)
                                  .Sum(npr => npr.RoundedTotalPrice);

                comparisonResult.Add(new PriceRowComparisonResult
                {
                    ArticleType = article,
                    TotalPrice = newPriceRows
                                  .Where(npr => EnumHelper.Parent<PriceRowType, InvoiceableArticle>(npr.PriceRowType) == article)
                                  .Sum(npr => npr.TotalPrice),
                    HasChanged = newPrice != previousPrice
                });
            }
            return comparisonResult;
        }

        public async Task<bool> HandleStandardDocumentCreation()
        {
            if (!_dateCalculationService.IsWorkingDay(_clock.SwedenNow))
            {
                return false;
            }
            var startAtSettings = _cacheService.OrganisationNotificationSettings
                .Where(n => n.NotificationType == NotificationType.OrderAgreementCreated)
                .Select(n => new { n.ReceivingOrganisationId, n.StartUsingNotificationAt }).ToList();
            var orderAgreementCustomerIds = _cacheService.CustomerSettings
                .Where(c => c.UsedCustomerSettingTypes.Contains(CustomerSettingType.UseOrderAgreements))
                .Select(c => c.CustomerOrganisationId).ToList();
                        
            foreach (int customerOrganisationId in orderAgreementCustomerIds)
            {
                var validUseFrom = startAtSettings.SingleOrDefault(s => s.ReceivingOrganisationId == customerOrganisationId)?.StartUsingNotificationAt ??
                    new DateTime(1900, 1, 1);
                var requestIds = await GetRequestIdsForDocumentCreation(customerOrganisationId, validUseFrom);

                _logger.LogInformation("For customer {customerOrganisationId}:  Found {count} requests to create order agreements for: {requestIds}",
                    customerOrganisationId, requestIds.Count, string.Join(", ", requestIds.Select(r => r)));

                foreach (var requestId in requestIds)
                {

                    try
                    {
                        await CreateAndStoreStandardDocument(requestId);
                        _logger.LogInformation("Processing completed order agreement for {requestId}.", requestId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failure processing order agreement creation for {requestId}", requestId);
                        await SendErrorMail(nameof(HandleStandardDocumentCreation), ex);
                    }
                }
            }            
            return true;
        }

        public async Task<List<int>> GetRequestIdsForDocumentCreation(int customerOrganisationId, DateTime validUseFrom)
        {
            DateTimeOffset occasionStartedAtOrBefore = _clock.SwedenNow;

            // Get all requestIds where it DOES NOT EXISTS any PeppolPayloads OR Requisitions and Starttime for Occasion has passed.      
            var requestIds = await _tolkDbContext.Requests
                .GetRequestsForOrderAgreementCreation(customerOrganisationId, validUseFrom)
                .Where(r => r.Order.StartAt <= occasionStartedAtOrBefore)
                .Select(r => r.RequestId)
                .ToListAsync();

            // Get all requestIds from Requisitions where no PeppolPayload has been created,             
            requestIds.AddRange(await _tolkDbContext.Requisitions
                .GetRequisitionForPeppolMessageCreation(customerOrganisationId, validUseFrom)                                       
                .Where(r => r.Request.Order.StartAt <= occasionStartedAtOrBefore)
                .Select(r => r.Request.RequestId)
                .ToListAsync());          

            // Get all requestIds from Automatically created Requisitions where no PeppolPayload has been created
             requestIds.AddRange(await _tolkDbContext.Requisitions
                .GetAutoGeneratedRequisitionForOrderAgreementCreation(customerOrganisationId, validUseFrom)
                .Select(r => r.Request.RequestId)
                .ToListAsync());

            return requestIds;
        }

        public async Task<PeppolPayload> CreateMockInvoiceFromRequest(Request request)
        {
            InvoiceModel model;
            if(request.CurrentlyActiveRequisition != null)
            {
                model = new InvoiceModel(request, await _tolkDbContext.RequisitionPriceRows.GetPriceRowsForRequisition(request.CurrentlyActiveRequisition.RequisitionId).ToListAsync());
            }
            else
            {
                model = new InvoiceModel(request, await _tolkDbContext.RequestPriceRows.GetPriceRowsForRequest(request.RequestId).ToListAsync());
            }                               
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            SerializeInvoiceModel(model, writer);
            memoryStream.Position = 0;
            byte[] byteArray = new byte[memoryStream.Length];
            memoryStream.Read(byteArray, 0, (int)memoryStream.Length);
            memoryStream.Close();
            return new PeppolPayload
            {
                Payload = byteArray,
                Request = request
            };            
        }

        private async Task SendErrorMail(string methodname, Exception ex)
        {
            await _emailService.SendErrorEmail(nameof(StandardBusinessDocumentService), methodname, ex);
        }

        private PeppolPayload SerializeAndCreatePeppolPayload<T>(Request request, OrderResponseModelBase model, PeppolMessageType type) where T : OrderResponseModelBase
        {          
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            SerializeModel<T>(model, writer);

            if (request.CurrentlyActiveRequisition != null)
            {
                _logger.LogInformation("Finished serializing {type} from {requisitionId}.", typeof(T), request.CurrentlyActiveRequisition.RequisitionId);
            }
            else
            {
                _logger.LogInformation("Finished serializing {type} from {requestId}.", typeof(T), request.RequestId);
            }

            memoryStream.Position = 0;
            byte[] byteArray = new byte[memoryStream.Length];
            memoryStream.Read(byteArray, 0, (int)memoryStream.Length);
            memoryStream.Close();

            return new PeppolPayload(request, type, byteArray, _clock.SwedenNow, model.ID.Value);
        }

        private static void SerializeModel<T>(OrderResponseModelBase model, StreamWriter writer)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add(nameof(Constants.cac), Constants.cac);
            ns.Add(nameof(Constants.cbc), Constants.cbc);
            XmlSerializer xser = new XmlSerializer(typeof(T), Constants.defaultNamespace);
            xser.Serialize(writer, model, ns);
        }
        private static void SerializeInvoiceModel(InvoiceModel model, StreamWriter writer)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add(nameof(Constants.cac), Constants.cac);
            ns.Add(nameof(Constants.cbc), Constants.cbc);
            XmlSerializer xser = new XmlSerializer(typeof(InvoiceModel), Constants.invoiceDefaultNamespace);
            xser.Serialize(writer, model, ns);
        }
    }
}

