﻿using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.BusinessLogic.Models.OrderAgreement;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Models
{
    public class OrderAgreementTests
    {
        private readonly Order MockOrder;
        private readonly Requisition MockRequisition;
        private readonly Request MockRequest;

        public OrderAgreementTests()
        {
            var mockCustomerUsers = MockEntities.MockCustomerUsers(MockEntities.MockCustomers);
            MockOrder = new Order(mockCustomerUsers[2], null, mockCustomerUsers[2].CustomerOrganisation, new DateTimeOffset(2018, 05, 07, 13, 00, 00, new TimeSpan(02, 00, 00)))
            {
                OrderId = 8,
                CustomerReferenceNumber = "EmptyOrder",
                OrderNumber = "2018-000008",
                Status = OrderStatus.Requested,
                Requests = new List<Request>()
            };
            MockRequest = new Request
            {
                Status = RequestStatus.Approved,
                Order = new Order(MockOrder)
                {
                    Status = OrderStatus.RequestRespondedAwaitingApproval,
                },
                Ranking = new Ranking { RankingId = 1, Broker = new Broker { Name = "MockBroker", OrganizationNumber = "123123-1234",PeppolId= "0007:1231231234" }, Rank = 1, FrameworkAgreement = MockEntities.FrameworkAgreements[0] },
            };
            MockRequisition = new Requisition
            {
                Status = RequisitionStatus.Created,
                Request = MockRequest
            };
        }

        [Fact]
        public void CreateOrderAgreementFromRequest_Valid()
        {
            CreateOrderAgreementFromRequests_Valid(false);
        }

        [Fact]
        public void CreateOrderAgreementFromRequestTwoBrokerFees_Valid()
        {
            CreateOrderAgreementFromRequests_Valid(true);
        }

        private void CreateOrderAgreementFromRequests_Valid(bool useTwoBrookerFees)
        {
            var pricerows = useTwoBrookerFees ? MockEntities.MockRequestPriceRowsTwoBrokerFees : MockEntities.MockRequestPriceRows;
            var request = MockRequest;
            var now = DateTime.UtcNow;

            var agreement = new OrderAgreementModel(request, now, pricerows);
            Assert.Equal(request.Order.OrderNumber, agreement.SalesOrderID);
            Assert.Equal($"{Constants.IdPrefix}{request.Order.OrderNumber}", agreement.ID.Value);
            Assert.Equal(Constants.Currency, agreement.DocumentCurrencyCode);            
            Assert.Equal(Constants.NotApplicableNotification, agreement.OrderReference.ID.Value);
            Assert.Equal(now.ToString("yyyy-MM-dd"), agreement.IssueDate);
            Assert.Equal(now.ToString("HH:mm:ss"), agreement.IssueTime);
            Assert.Equal(MockEntities.FrameworkAgreements[0].AgreementNumber, agreement.Contract.ID.Value);
            Assert.Equal(request.Order.InvoiceReference, agreement.CustomerReference);
            Assert.Equal(Constants.Currency, agreement.DocumentCurrencyCode);
            Assert.Equal(request.Order.CustomerOrganisation.OrganisationNumber.ToNotHyphenatedFormat(), agreement.BuyerCustomerParty.Party.EndpointID.Value);
            Assert.Equal(request.Ranking.Broker.OrganizationNumber.ToNotHyphenatedFormat(), agreement.SellerSupplierParty.Party.EndpointID.Value);
            decimal sum = pricerows.Where(pr => pr.PriceRowType != PriceRowType.RoundedPrice).Sum(pr => pr.TotalPrice);

            Assert.Equal(sum, agreement.OrderLines.Sum(ol => ol.LineItem.Price.PriceAmount.AmountSum));
            Assert.Equal(sum, agreement.OrderLines.Sum(ol => ol.LineItem.LineExtensionAmount.AmountSum));
            Assert.Equal(sum, agreement.LegalMonetaryTotal.LineExtensionAmount.AmountSum);
            Assert.Equal(sum, agreement.LegalMonetaryTotal.TaxExclusiveAmount.AmountSum);

            //HARDCODED TEST FOR 25% VAT and the rounded amounts
            decimal taxAmount = sum * (decimal)0.25;
            decimal totalTaxInclusiveAmount = sum * (decimal)1.25;
            Assert.Equal(taxAmount, agreement.TaxTotal.TaxAmount.AmountSum);
            Assert.Equal(totalTaxInclusiveAmount, agreement.LegalMonetaryTotal.TaxInclusiveAmount.AmountSum);
            decimal rounding = GetRounding(totalTaxInclusiveAmount);
            decimal roundedSum = totalTaxInclusiveAmount + rounding;
            Assert.Equal(rounding, agreement.LegalMonetaryTotal.PayableRoundingAmount.AmountSum);
            Assert.Equal(roundedSum, agreement.LegalMonetaryTotal.PayableAmount.AmountSum);
        }

        [Fact]
        public void CreateOrderAgreementFromRequisition_Valid()
        {
            var requisition = MockRequisition;
            var now = DateTime.UtcNow;

            var agreement = new OrderAgreementModel(requisition, now, MockEntities.MockRequisitionPriceRows);
            Assert.Equal(requisition.Request.Order.OrderNumber, agreement.SalesOrderID);
            Assert.Equal($"{Constants.IdPrefix}{requisition.Request.Order.OrderNumber}", agreement.ID.Value);
            Assert.Equal(Constants.Currency, agreement.DocumentCurrencyCode);            
            Assert.Equal(Constants.NotApplicableNotification, agreement.OrderReference.ID.Value);
            Assert.Equal(now.ToString("yyyy-MM-dd"), agreement.IssueDate);
            Assert.Equal(now.ToString("HH:mm:ss"), agreement.IssueTime);
            Assert.Equal(MockEntities.FrameworkAgreements[0].AgreementNumber, agreement.Contract.ID.Value);
            Assert.Equal(requisition.Request.Order.InvoiceReference, agreement.CustomerReference);
            Assert.Equal(Constants.Currency, agreement.DocumentCurrencyCode);
            Assert.Equal(requisition.Request.Order.CustomerOrganisation.OrganisationNumber.ToNotHyphenatedFormat(), agreement.BuyerCustomerParty.Party.EndpointID.Value);
            Assert.Equal(requisition.Request.Ranking.Broker.OrganizationNumber.ToNotHyphenatedFormat(), agreement.SellerSupplierParty.Party.EndpointID.Value);
            decimal sum = MockEntities.MockRequisitionPriceRows.Where(pr => pr.PriceRowType != PriceRowType.RoundedPrice).Sum(pr => pr.TotalPrice);
            Assert.Equal(sum, agreement.OrderLines.Sum(ol => ol.LineItem.Price.PriceAmount.AmountSum));
            Assert.Equal(sum, agreement.OrderLines.Sum(ol => ol.LineItem.LineExtensionAmount.AmountSum));
            Assert.Equal(sum, agreement.LegalMonetaryTotal.LineExtensionAmount.AmountSum);
            Assert.Equal(sum, agreement.LegalMonetaryTotal.TaxExclusiveAmount.AmountSum);

            //HARDCODED TEST FOR 25% VAT  and the rounded amounts
            decimal taxAmount = sum * (decimal)0.25;
            decimal totalTaxInclusiveAmount = sum * (decimal)1.25;
            Assert.Equal(taxAmount, agreement.TaxTotal.TaxAmount.AmountSum);
            Assert.Equal(totalTaxInclusiveAmount, agreement.LegalMonetaryTotal.TaxInclusiveAmount.AmountSum);
            decimal rounding = GetRounding(totalTaxInclusiveAmount);
            decimal roundedSum = totalTaxInclusiveAmount + rounding;
            Assert.Equal(rounding, agreement.LegalMonetaryTotal.PayableRoundingAmount.AmountSum);
            Assert.Equal(roundedSum, agreement.LegalMonetaryTotal.PayableAmount.AmountSum);
        }

        [Theory]
        [InlineData(RequisitionStatus.Commented)]
        [InlineData(RequisitionStatus.DeniedByCustomer)]
        public void CreateOrderAgreementFromRequisition_InvalidStatus(RequisitionStatus status)
        {
            var requisition = MockRequisition;
            requisition.Status = status;
            Assert.Throws<InvalidOperationException>(() => new OrderAgreementModel(requisition, DateTime.UtcNow, MockEntities.MockRequisitionPriceRows));
        }

        [Theory]
        [InlineData(RequisitionStatus.Approved)]
        [InlineData(RequisitionStatus.AutomaticGeneratedFromCancelledOrder)]
        [InlineData(RequisitionStatus.Created)]
        [InlineData(RequisitionStatus.Reviewed)]
        public void CreateOrderAgreementFromRequisition_ValidStatus(RequisitionStatus status)
        {
            var requisition = MockRequisition;
            requisition.Status = status;
            requisition.ProcessedAt = DateTime.Now;
            var agreement = new OrderAgreementModel(requisition, DateTime.UtcNow, MockEntities.MockRequisitionPriceRows);
            Assert.Equal(requisition.Request.Order.OrderNumber, agreement.OrderNumber);
        }

        [Theory]
        [InlineData(RequisitionStatus.Approved, "2021-10-10 12:31:00", "2021-10-10 11:00:00", true)]
        [InlineData(RequisitionStatus.AutomaticGeneratedFromCancelledOrder, "2021-10-10 12:31:00", null, false)]
        [InlineData(RequisitionStatus.Created, "2021-10-10 12:31:00", null, false)]
        [InlineData(RequisitionStatus.Reviewed, "2021-10-10 12:31:00", "2021-10-10 11:00:00", true)]
        public void CreateOrderAgreementFromRequisition_IssuedDates(RequisitionStatus status, string generatedDateTime, string processedDateTime, bool expectsProcessedAt)
        {
            var requisition = MockRequisition;
            requisition.Status = status;
            if (expectsProcessedAt)
            {
                requisition.ProcessedAt = DateTime.Parse(processedDateTime);
            }
            var agreement = new OrderAgreementModel(requisition, DateTime.Parse(generatedDateTime), MockEntities.MockRequisitionPriceRows);
            if (expectsProcessedAt)
            {
                Assert.Equal(DateTime.Parse(processedDateTime).ToString("yyyy-MM-dd"), agreement.IssueDate);
                Assert.Equal(DateTime.Parse(processedDateTime).ToString("HH:mm:ss"), agreement.IssueTime);
            }
            else
            {
                Assert.Equal(DateTime.Parse(generatedDateTime).ToString("yyyy-MM-dd"), agreement.IssueDate);
                Assert.Equal(DateTime.Parse(generatedDateTime).ToString("HH:mm:ss"), agreement.IssueTime);
            }
        }
        [Theory]
        [InlineData(InvoiceableArticle.TravelCost)]
        [InlineData(InvoiceableArticle.AdministrativeCharge)]
        [InlineData(InvoiceableArticle.InterpreterCompensationIncludingSocialCharge)]
        [InlineData(InvoiceableArticle.BrokerFee)]
        public void CreateOrderResponseFromRequisitionWithUpdatedPrices(InvoiceableArticle articleWithUpdatedPrice)
        {
            var requisition = MockRequisition;
            var articles = (InvoiceableArticle[])Enum.GetValues(typeof(InvoiceableArticle));
            var priceRows = articles.Select(a => new PriceRowComparisonResult
            {
                ArticleType = a,
                TotalPrice = 100,
                HasChanged = a == articleWithUpdatedPrice
            }).ToList();

            var response = new OrderResponseModel(requisition,new DateTime(2022,1,1), priceRows);
            var changedLine = response.OrderLines.Where(ol => ol.LineItem.ID.Value == ((int)articleWithUpdatedPrice).ToString()).First();
            var unchangedLines = response.OrderLines.Where(ol => ol.LineItem.ID.Value != ((int)articleWithUpdatedPrice).ToString()).ToList();
            Assert.NotEqual($"{Constants.IdPrefix}{requisition.Request.Order.OrderNumber}", response.ID.Value);
            Assert.Equal(Constants.LineAcceptedWithChange, changedLine.LineItem.LineStatusCode);
            Assert.Equal(3, unchangedLines.Count());
            Assert.True(unchangedLines.All(ucl => ucl.LineItem.LineStatusCode == Constants.LineAcceptedWithoutChange));            
        }                

        private decimal GetRounding(decimal value)
        {
            value -= Math.Floor(value);
            return value > Convert.ToDecimal(0.5) ? 1 - value : -value;
        }
    }
}
