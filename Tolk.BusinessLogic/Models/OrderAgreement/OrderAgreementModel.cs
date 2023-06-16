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
    public class OrderAgreementModel : OrderResponseModelBase
    {

        private OrderAgreementModel() {
        }
        public OrderAgreementModel(Requisition requisition, DateTimeOffset generatedAt, IEnumerable<PriceRowBase> prices) : base(requisition, generatedAt)
        {
            CustomizationID = Constants.OrderAgreementCustomizationId;
            ProfileID = Constants.OrderAgreementProfileId;
            OrderReference = new ObjectWithIdModel { ID = new EndPointIDModel { Value = Constants.NotApplicableNotification } };

            DocumentID = $"{Constants.IdPrefix}{OrderNumber}";            
            Contract = new ObjectWithIdModel { ID = new EndPointIDModel { Value = requisition.Request.Ranking.FrameworkAgreement.AgreementNumber } };                
            OrderLines = GetOrderLines(requisition.SessionStartedAt, requisition.SessionEndedAt, prices).ToList();
        }

        public OrderAgreementModel(Request request, DateTimeOffset generatedAt, IEnumerable<PriceRowBase> prices) : base(request,generatedAt)
        {
            CustomizationID = Constants.OrderAgreementCustomizationId;
            ProfileID = Constants.OrderAgreementProfileId;
            OrderReference = new ObjectWithIdModel { ID = new EndPointIDModel { Value = Constants.NotApplicableNotification } };
            var order = request.Order;
            DocumentID = $"{Constants.IdPrefix}{OrderNumber}";            
            Contract = new ObjectWithIdModel { ID = new EndPointIDModel { Value = request.Ranking.FrameworkAgreement.AgreementNumber } };                        
            OrderLines = GetOrderLines(order.StartAt, order.EndAt, prices).ToList();
        }

        [XmlElement(Namespace = Constants.cbc, Order = 7)]
        public string Note { get => _note; set { } }

        [XmlElement(Namespace = Constants.cbc, Order = 8)]
        public string DocumentCurrencyCode
        {
            get => Constants.Currency;
            set { }
        }

        //Fakturareferens        
        [XmlElement(Namespace = Constants.cbc, Order = 9)]
        public string CustomerReference { get => _customerReference; set { } } 

        [XmlElement(Namespace = Constants.cac, Order = 10)]
        public ObjectWithIdModel OrderReference { get; set; }

        [XmlElement(Namespace = Constants.cac, Order = 11)]
        public ObjectWithIdModel Contract { get; set; }

        // REQUIRED for OA OPTIONAL for OR, always set on both        
        [XmlElement(Namespace = Constants.cac, Order = 12)]       
        public OrganizationPartyModel SellerSupplierParty { get => _sellerSupplierParty; set { } }
  
        [XmlElement(Namespace = Constants.cac, Order = 13)]
        public OrganizationPartyModel BuyerCustomerParty { get => _buyerCustomerParty; set { } }

        [XmlElement(Namespace = Constants.cac, Order = 14)]
        public TaxTotalModel TaxTotal
        {
            get => new TaxTotalModel
            {
                TaxAmount = new AmountModel
                {
                    AmountSum = OrderLines.Sum(ol => ol.LineItem.Price.PriceAmount.AmountSum * (decimal)ol.LineItem.Item.ClassifiedTaxCategory.Percent / 100)
                },
                //Split(group by) per Vat number in orderlines
                TaxSubtotal = OrderLines.GroupBy(ol => ol.LineItem.Item.ClassifiedTaxCategory.Percent)
                        .Select(g => new TaxSubtotalModel
                        {
                            TaxableAmount = new AmountModel
                            {
                                AmountSum = g.Sum(ol => ol.LineItem.Price.PriceAmount.AmountSum)
                            },
                            TaxAmount = new AmountModel
                            {
                                AmountSum = g.Sum(ol => ol.LineItem.Price.PriceAmount.AmountSum * (decimal)g.Key / 100)
                            },
                            TaxCategory = new TaxCategoryModel
                            {
                                Percent = g.Key
                            }
                        }
                    ).ToList()
            };
            set { }
        }

        [XmlElement(Namespace = Constants.cac, Order = 15)]
        public LegalMonetaryTotalModel LegalMonetaryTotal
        {
            get
            {
                var lineSum = OrderLines.Sum(ol => ol.LineItem.LineExtensionAmount.AmountSum);
                return new LegalMonetaryTotalModel
                {
                    LineExtensionAmount = new AmountModel { AmountSum = lineSum },
                    TaxSum = TaxTotal.TaxAmount.AmountSum,
                    PayableRoundingAmount = new AmountModel { AmountSum = GetRounding(lineSum + TaxTotal.TaxAmount.AmountSum) }
                };
            }
            set { }
        }

        [XmlElement(ElementName = "OrderLine", Namespace = Constants.cac, Order = 16)]
        public List<OrderLineModel> OrderLines { get; set; }

        private static IEnumerable<OrderLineModel> GetOrderLines(DateTimeOffset startAt, DateTimeOffset endAt, IEnumerable<PriceRowBase> prices)
        {
            foreach (var article in (InvoiceableArticle[])Enum.GetValues(typeof(InvoiceableArticle)))
            {
                yield return new OrderLineModel
                {
                    LineItem = new LineItemModel
                    {
                        ID = new EndPointIDModel { Value = ((int)article).ToString() },
                        Delivery = new DeliveryModel
                        {
                            PromisedDeliveryPeriod = article == InvoiceableArticle.InterpreterCompensationIncludingSocialCharge ?
                            new PromisedDeliveryPeriodModel
                            {
                                StartAt = startAt,
                                EndAt = endAt
                            } :
                            null
                        },
                        Item = new ItemModel
                        {
                            Name = article.GetDescription(),
                            SellersItemIdentification = new ObjectWithIdModel { ID = new EndPointIDModel { Value = ((int)article).ToString() } },
                            ClassifiedTaxCategory = new TaxCategoryModel
                            {
                                Percent = article.GetVat() * 100
                            }

                        },
                        Price = new PriceModel
                        {
                            PriceAmount = new AmountModel
                            {
                                AmountSum = prices
                                    .Where(pr => EnumHelper.Parent<PriceRowType, InvoiceableArticle>(pr.PriceRowType) == article)
                                    .Sum(pr => pr.TotalPrice)
                            },
                            PriceType = Constants.ContractPrice
                        },
                        LineExtensionAmount = new AmountModel
                        {
                            AmountSum = prices
                                    .Where(pr => EnumHelper.Parent<PriceRowType, InvoiceableArticle>(pr.PriceRowType) == article)
                                    .Sum(pr => pr.TotalPrice)
                        }
                    }
                };
            }
        }
        private decimal GetRounding(decimal value)
        {
            value -= Math.Floor(value);
            return value > Convert.ToDecimal(0.5) ? 1 - value : -value;
        }
    }
}
