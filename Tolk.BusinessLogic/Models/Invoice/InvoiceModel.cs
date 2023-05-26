using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Models.OrderAgreement;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Models.Invoice
{
    [Serializable]
    [XmlRoot("Invoice", Namespace = Constants.invoiceDefaultNamespace)]
    public class InvoiceModel
    {
   
        [XmlIgnore]
        public string DocumentID;

        [XmlIgnore]
        public string OrderNumber { get; set; }

        [XmlIgnore]
        public DateTimeOffset IssuedAt { get; set; }

        [XmlElement(Namespace = Constants.cbc, Order = 1)]
        public string CustomizationID = Constants.InvoiceCustomizationId;

        [XmlElement(Namespace = Constants.cbc, Order = 2)]
        public string ProfileID = Constants.InvoiceProfileId;
        
        [XmlElement(Namespace = Constants.cbc, Order = 3)]
        public EndPointIDModel ID
        {
            get => new EndPointIDModel { Value = DocumentID };
            set => DocumentID = value.Value;
        }

        [XmlElement(Namespace = Constants.cbc, Order = 4)]
        public string IssueDate
        {
            get => IssuedAt.ToString("yyyy-MM-dd");
            set { }
        }

        [XmlElement(Namespace = Constants.cbc, Order = 5)]
        public string DueDate
        {
            get => IssuedAt.AddDays(30).ToString("yyyy-MM-dd");
            set { }
        }

        [XmlElement(Namespace = Constants.cbc, Order = 6)]
        public string InvoiceTypeCode = Constants.CommercialInvoiceTypeCode;

        [XmlElement(Namespace = Constants.cbc, Order = 7)]
        public string DocumentCurrencyCode
        {
            get => Constants.Currency;
            set { }
        }

        [XmlElement(Namespace = Constants.cac, Order = 8)]
        public InvoicePeriodModel InvoicePeriod
        {
            get => new InvoicePeriodModel
            {
                StartAt = IssuedAt,
                EndAt = IssuedAt
            };
            set { }
        }

        [XmlElement(Namespace = Constants.cac, Order = 9)]
        public ObjectWithIdModel OrderReference 
        {   get => new ObjectWithIdModel { ID = new EndPointIDModel { Value = OrderNumber } };
            set { }
        }

        [XmlElement(Namespace = Constants.cac, Order = 10)]
        public InvoiceOrganizationPartyModel AccountingSupplierParty { get; set; }

        [XmlElement(Namespace = Constants.cac, Order = 11)]
        public InvoiceOrganizationPartyModel AccountingCustomerParty { get; set; }

        [XmlElement(Namespace = Constants.cac, Order = 12)]
        public InvoiceDeliveryModel Delivery { get; set; }

        [XmlElement(Namespace = Constants.cac, Order = 13)]
        public PaymentMeansModel PaymentMeans { get => new PaymentMeansModel
            {
                PayeeFinancialAccount = new FinancialAccountModel
                {
                    ID = new EndPointIDModel
                    {
                        Value = "12345678"
                    },
                    FinancialInstitutionBranch = new ObjectWithIdModel
                    {
                        ID = new EndPointIDModel
                        {
                            Value = "SE:BANKGIRO"
                        }
                    }
                }};
            set { }
        }

        [XmlElement(Namespace = Constants.cac, Order = 14)]
        public PaymentTermsModel PaymentTerms = new PaymentTermsModel();

        [XmlElement(Namespace = Constants.cac, Order = 15)]
        public TaxTotalModel TaxTotal
        {
            get => new TaxTotalModel
            {
                TaxAmount = new AmountModel
                {
                    AmountSum = InvoiceLines.Sum(ol => ol.Price.PriceAmount.AmountSum * (decimal)ol.Item.ClassifiedTaxCategory.Percent / 100)
                },
                //Split(group by) per Vat number in orderlines
                TaxSubtotal = InvoiceLines.GroupBy(ol => ol.Item.ClassifiedTaxCategory.Percent)
                        .Select(g => new TaxSubtotalModel
                        {
                            TaxableAmount = new AmountModel
                            {
                                AmountSum = g.Sum(ol => ol.Price.PriceAmount.AmountSum)
                            },
                            TaxAmount = new AmountModel
                            {
                                AmountSum = g.Sum(ol => ol.Price.PriceAmount.AmountSum * (decimal)g.Key / 100)
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

        [XmlElement(Namespace = Constants.cac, Order = 16)]
        public LegalMonetaryTotalModel LegalMonetaryTotal
        {
            get
            {
                var lineSum = InvoiceLines.Sum(ol => ol.LineExtensionAmount.AmountSum);
                return new LegalMonetaryTotalModel
                {
                    LineExtensionAmount = new AmountModel { AmountSum = lineSum },
                    TaxSum = TaxTotal.TaxAmount.AmountSum,
                    PayableRoundingAmount = new AmountModel { AmountSum = GetRounding(lineSum + TaxTotal.TaxAmount.AmountSum) }
                };
            }
            set { }
        }

        [XmlElement(ElementName = "InvoiceLine", Namespace = Constants.cac, Order = 17)]
        public List<InvoiceLineModel> InvoiceLines { get; set; }
        public InvoiceModel(Request request, IEnumerable<PriceRowBase> prices)
        {
            DocumentID = Guid.NewGuid().ToString();
            IssuedAt = DateTimeOffset.Now;
            OrderNumber = request.Order.OrderNumber;
            Delivery = new InvoiceDeliveryModel { ActualDeliveryDate = request.CurrentlyActiveRequisition != null ? request.CurrentlyActiveRequisition.CreatedAt.ToString("yyyy-MM-dd") : request.Order.StartAt.ToString("yyyy-MM-dd") };
            AccountingSupplierParty = GetAccountingSupplierParty(request);
            AccountingCustomerParty = GetAccountingCustomerParty(request.Order);
            InvoiceLines = GetInvoiceLines(prices).ToList();
        }
        public InvoiceModel() { }
        private static InvoiceOrganizationPartyModel GetAccountingSupplierParty(Request request)
        {
            return new InvoiceOrganizationPartyModel
            {
                Party = new InvoicePartyModel
                {
                    EndpointID = new EndPointIDModel { SchemeId = Constants.PeppolIdByOrganizationNumberSchemeId, Value = request.Ranking.Broker.OrganizationNumber.ToNotHyphenatedFormat() },
                    PostalAddress = new AddressModel(),
                    PartyTaxScheme = new PartyTaxSchemeModel { CompanyID = $"SE111111111111" },
                    PartyLegalEntity = new PartyLegalEntityModel { RegistrationName = request.Ranking.Broker.Name }
                }
            };
        }
        private static InvoiceOrganizationPartyModel GetAccountingCustomerParty(Order order)
        {
            return new InvoiceOrganizationPartyModel
            {
                Party = new InvoicePartyModel
                {
                    EndpointID = new EndPointIDModel { SchemeId = Constants.PeppolIdByOrganizationNumberSchemeId, Value = order.CustomerOrganisation.OrganisationNumber.ToNotHyphenatedFormat() },
                    PostalAddress = new AddressModel(),
                    PartyLegalEntity = new PartyLegalEntityModel { RegistrationName = order.CustomerOrganisation.Name }
                }
            };
        }        
        private IEnumerable<InvoiceLineModel> GetInvoiceLines(IEnumerable<PriceRowBase> prices)
        {
            foreach (var article in (InvoiceableArticle[])Enum.GetValues(typeof(InvoiceableArticle)))
            {
                yield return new InvoiceLineModel
                {                  
                        ID = new EndPointIDModel { Value = ((int)article).ToString() },                                             
                        LineExtensionAmount = new AmountModel
                        {
                            AmountSum = prices
                                    .Where(pr => EnumHelper.Parent<PriceRowType, InvoiceableArticle>(pr.PriceRowType) == article)
                                    .Sum(pr => pr.TotalPrice)
                        },
                        InvoicePeriod = InvoicePeriod,
                        OrderLineReference = new OrderLineReferenceModel { LineID = new EndPointIDModel { Value = ((int)article).ToString() } },
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
                            }                            
                        },
    
                };
            }
        }
        private decimal GetRounding(decimal value)
        {
            value -= Math.Floor(value);
            // Rounding to avoid mismatch if rounding up, (ex. Taxinclusive non rounded = XX.575) rounded will then be saved as 0.425, when writing they will both round upwards (0.58 and 0.43 which together equals 1.01)
            value = decimal.Round(value, 2, MidpointRounding.AwayFromZero);                                    
            return value > Convert.ToDecimal(0.5) ? 1 - value : -value;
        }
    }
}
