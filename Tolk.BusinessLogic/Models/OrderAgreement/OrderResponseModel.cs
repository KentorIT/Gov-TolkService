using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Models.OrderAgreement
{
    [Serializable]    
    [XmlRoot("OrderResponse")]
    public class OrderResponseModel : OrderResponseModelBase
    {
        public OrderResponseModel()
        {            
        }
        public OrderResponseModel(Request request,DateTimeOffset generatedAt, IEnumerable<PriceRowComparisonResult> prices)
        {
            var order = request.Order;
            CustomizationID = Constants.OrderResponseCustomizationId;
            ProfileID = Constants.OrderResponseProfileId;            
            DocumentID = Guid.NewGuid().ToString();
            IssuedAt = generatedAt;
            Note = "Created from request";
            OrderNumber = order.OrderNumber;
            CustomerReference = order.InvoiceReference;
            OrderReference = new ObjectWithIdModel { ID = new EndPointIDModel { Value = $"{Constants.IdPrefix}{OrderNumber}" } };           
            SellerSupplierParty = GetSellerSupplierParty(request);
            BuyerCustomerParty = GetBuyerCustomerParty(order);
            OrderLines = GetOrderLines(order.StartAt, order.EndAt, prices).ToList();
        }    
        public OrderResponseModel(Requisition requisition, DateTimeOffset generatedAt, IEnumerable<PriceRowComparisonResult> prices)
        {
            switch (requisition.Status)
            {
                case RequisitionStatus.Approved:
                case RequisitionStatus.Reviewed:
                    IssuedAt = requisition.ProcessedAt.Value;
                break;
                case RequisitionStatus.Created:
                case RequisitionStatus.AutomaticGeneratedFromCancelledOrder:
                    IssuedAt = generatedAt;
                break;
                case RequisitionStatus.DeniedByCustomer:
                case RequisitionStatus.Commented:
                    throw new InvalidOperationException($"Requisition {requisition.RequisitionId} is {requisition.Status}. If the customer is in dissagreement, an order response cannot be created.");
            }

            var order = requisition.Request.Order;             
            CustomizationID = Constants.OrderResponseCustomizationId;
            ProfileID = Constants.OrderResponseProfileId;            
            DocumentID = Guid.NewGuid().ToString();
            IssuedAt = generatedAt;
            Note = "Created from requisition";
            OrderNumber = order.OrderNumber;
            CustomerReference = order.InvoiceReference;
            OrderReference = new ObjectWithIdModel { ID = new EndPointIDModel { Value = $"{Constants.IdPrefix}{OrderNumber}" } };
            SellerSupplierParty = GetSellerSupplierParty(requisition.Request);
            BuyerCustomerParty = GetBuyerCustomerParty(order);
            OrderLines = GetOrderLines(order.StartAt, order.EndAt, prices).ToList();
        }
        
        [XmlElement(Namespace = Constants.cbc, Order = 7)]
        public string OrderResponseCode = Constants.OrderConditionallyAccepted;

        [XmlElement(Namespace = Constants.cbc, Order = 8)]
        public string Note { get; set; }

        [XmlElement(Namespace = Constants.cbc, Order = 9)]
        public string DocumentCurrencyCode
        {
            get => Constants.Currency;
            set { }
        }

        //Fakturareferens        
        [XmlElement(Namespace = Constants.cbc, Order = 10)]
        public string CustomerReference { get; set; }

        [XmlElement(Namespace = Constants.cac, Order = 11)]
        public ObjectWithIdModel OrderReference { get; set; }

        // REQUIRED for OA OPTIONAL for OR, always set on both        
        [XmlElement(Namespace = Constants.cac, Order = 13)]
        public OrganizationPartyModel SellerSupplierParty { get; set; }

        [XmlElement(Namespace = Constants.cac, Order = 14)]
        public OrganizationPartyModel BuyerCustomerParty { get; set; }
        [XmlElement(ElementName = "OrderLine", Namespace = Constants.cac, Order = 15)]
        public List<OrderLineModel> OrderLines { get; set; }

        private static IEnumerable<OrderLineModel> GetOrderLines(DateTimeOffset startAt, DateTimeOffset endAt, IEnumerable<PriceRowComparisonResult> prices)
        {            
            foreach (var price in prices)
            {
                yield return new OrderLineModel
                {
                    LineItem = new LineItemModel
                    {
                        ID = new EndPointIDModel { Value = ((int)price.ArticleType).ToString() },
                        LineStatusCode = price.HasChanged ? Constants.LineAcceptedWithChange : Constants.LineAcceptedWithoutChange,                     
                        Item = new ItemModel
                        {
                            Name = price.ArticleType.GetDescription(),
                            SellersItemIdentification = new ObjectWithIdModel { ID = new EndPointIDModel { Value = ((int)price.ArticleType).ToString() } },                          
                        },
                        Price = new PriceModel
                        {
                            PriceAmount = new AmountModel
                            {
                                AmountSum = price.TotalPrice
                            }
                        }
                    },                    
                    OrderLineReference = new OrderLineReferenceModel { LineID = new EndPointIDModel { Value = ((int)price.ArticleType).ToString()} }
                };
            }
        }     
    }
}
