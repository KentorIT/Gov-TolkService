﻿using System;
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
    public class OrderAgreementModel
    {
        public static readonly EndPointIDModel OwnerPeppolId = new EndPointIDModel { SchemeId = Constants.PeppolSchemeId, Value = "7350053850019" };

        private OrderAgreementModel() { }
        public OrderAgreementModel(Requisition requisition, DateTimeOffset generatedAt, IEnumerable<PriceRowBase> prices, int? previousOrderAgreementIndex = null)
        {
            if (previousOrderAgreementIndex.HasValue && previousOrderAgreementIndex < 1)
            {
                throw new InvalidOperationException($"{nameof(previousOrderAgreementIndex)} cannot be zero or negative.");
            }
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
                    throw new InvalidOperationException($"Requisition {requisition.RequisitionId} is {requisition.Status}. If the customer is in dissagreement, an order agreement cannot be created.");
            }

            var order = requisition.Request.Order;

            Index = previousOrderAgreementIndex + 1 ?? 1;
            Note = "Created from requisition";
            // if autogenerated use the requisition createdAt 
            // if only created, use DateTime.Now            
            OrderNumber = order.OrderNumber;
            OrderReference = previousOrderAgreementIndex != null ? new ObjectWithIdModel { ID = new EndPointIDModel {Value = $"{Constants.IdPrefix}{OrderNumber}-{previousOrderAgreementIndex}" } } : new ObjectWithIdModel { ID = new EndPointIDModel { Value = Constants.NotApplicableNotification } };
            SellerSupplierParty = new OrganizationPartyModel
            {
                Party = new PartyModel
                {
                    EndpointID = OwnerPeppolId,
                    PartyIdentification = new PartyIdentificationModel { ID = new EndPointIDModel { SchemeId = Constants.OrganizationNumberSchemeId, Value = requisition.Request.Ranking.Broker.OrganizationNumber } },
                    PartyLegalEntity = new PartyLegalEntityModel { RegistrationName = requisition.Request.Ranking.Broker.Name }
                }
            };
            BuyerCustomerParty = new OrganizationPartyModel
            {
                Party = new PartyModel
                {
                    EndpointID = OwnerPeppolId,
                    PartyIdentification = new PartyIdentificationModel { ID = new EndPointIDModel { SchemeId = Constants.PeppolSchemeId, Value = order.CustomerOrganisation.PeppolId } },
                    PartyLegalEntity = new PartyLegalEntityModel { RegistrationName = order.CustomerOrganisation.Name }
                }
            };
            CustomerReference = order.InvoiceReference;

            OrderLines = GetOrderLines(requisition, prices).ToList();
        }

        //public OrderAgreementModel(Request request)
        //{
        //}

        [XmlIgnore]
        public int OrderId { get; set; }
        [XmlIgnore]
        public string OrderNumber { get; set; }
        [XmlIgnore]
        public int Index { get; set; }
        [XmlIgnore]
        public DateTimeOffset IssuedAt { get; set; }

        [XmlElement(Namespace = Constants.cbc)]
        public string CustomizationID
        {
            get => Constants.CustomizationId;
            set { }
        }

        [XmlElement(Namespace = Constants.cbc)]
        public string ProfileID
        {
            get => Constants.ProfileId;
            set { }
        }

        [XmlElement(Namespace = Constants.cbc)]
        public EndPointIDModel ID
        {
            get => new EndPointIDModel { Value = $"{Constants.IdPrefix}{OrderNumber}-{Index}" };
            set { }
        }

        [XmlElement(Namespace = Constants.cbc)]
        public string SalesOrderID
        {
            get => OrderNumber;
            set { }
        }

        [XmlElement(Namespace = Constants.cbc)]
        public string IssueDate
        {
            get => IssuedAt.ToString("yyyy-MM-dd");
            set { }
        }

        [XmlElement(Namespace = Constants.cbc)]
        public string IssueTime
        {
            get => IssuedAt.ToString("hh:mm:ss");
            set { }
        }

        [XmlElement(Namespace = Constants.cbc)]
        public string Note { get; set; }

        [XmlElement(Namespace = Constants.cbc)]
        public string DocumentCurrencyCode
        {
            get => Constants.Currency;
            set { }
        }

        //Fakturareferens
        [XmlElement(Namespace = Constants.cbc)]
        public string CustomerReference { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public ObjectWithIdModel OrderReference { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public ObjectWithIdModel Contract
        {
            get => new ObjectWithIdModel { ID = new EndPointIDModel { Value = Constants.ContractNumber } };
            set { }
        }

        [XmlElement(Namespace = Constants.cac)]
        public OrganizationPartyModel SellerSupplierParty { get; set; }

        [XmlElement(Namespace = Constants.cac)]
        public OrganizationPartyModel BuyerCustomerParty { get; set; }

        [XmlElement(Namespace = Constants.cac)]
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

        [XmlElement(Namespace = Constants.cac)]
        public LegalMonetaryTotalModel LegalMonetaryTotal
        {
            get => new LegalMonetaryTotalModel
            {
                LineExtensionAmount = new AmountModel
                {
                    AmountSum = OrderLines.Sum(ol => ol.LineItem.LineExtensionAmount.AmountSum)
                },
                TaxSum = TaxTotal.TaxAmount.AmountSum
            };
            set { }
        }

        [XmlElement(ElementName = "OrderLine", Namespace = Constants.cac)]
        public List<OrderLineModel> OrderLines { get; set; }

        private static IEnumerable<OrderLineModel> GetOrderLines(Requisition requisition, IEnumerable<PriceRowBase> prices)
        {
            foreach (var article in (InvoiceableArticle[])Enum.GetValues(typeof(InvoiceableArticle)))
            {
                string note = null;
                if (article == InvoiceableArticle.InterpreterCompensationIncludingSocialCharge)
                {
                    note = "Den beskrivning av rader som man vill presentera";

                }
                yield return new OrderLineModel
                {
                    LineItem = new LineItemModel
                    {
                        ID = new EndPointIDModel { Value = ((int)article).ToString() },
                        Note = note,
                        Delivery = new DeliveryModel
                        {
                            PromisedDeliveryPeriod = article == InvoiceableArticle.InterpreterCompensationIncludingSocialCharge ?
                            new PromisedDeliveryPeriodModel
                            {
                                StartAt = requisition.SessionStartedAt,
                                EndAt = requisition.SessionEndedAt,
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
                                    .Sum(pr => pr.Price)
                            }
                        }
                    }
                };
            }
        }
    }
}
