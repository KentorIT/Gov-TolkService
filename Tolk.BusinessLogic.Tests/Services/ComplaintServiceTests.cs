using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Services
{
    public class ComplaintServiceTests
    {
        private readonly TolkDbContext _tolkDbContext;
        private readonly ILogger<ComplaintService> _logger;
        private readonly INotificationService _notificationService;
        private readonly StubSwedishClock _clock;

        public ComplaintServiceTests()
        {
            _tolkDbContext = CreateTolkDbContext();
            _logger = Mock.Of<ILogger<ComplaintService>>();
            _notificationService = new StubNotificationService();
            _clock = new StubSwedishClock("2018-12-12 00:00:00");
        }

        private TolkDbContext CreateTolkDbContext(string databaseName = "empty")
        {
            var options = new DbContextOptionsBuilder<TolkDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new TolkDbContext(options);
        }

        [Theory]
        [InlineData(RequestStatus.Accepted)]
        [InlineData(RequestStatus.DeclinedByBroker)]
        public void Create_InvalidStatus(RequestStatus status)
        {
            var service = new ComplaintService(_tolkDbContext, _clock, _notificationService, _logger);
            var request = new Request
            {
                Status = status,
                Complaints = new List<Complaint>()
            };
            Assert.Throws<InvalidOperationException>(() =>
                service.Create(request, 1, null, "apa", ComplaintType.BadDelivery));
        }

        [Fact]
        public void Create_PreviousComplaint()
        {
            var service = new ComplaintService(_tolkDbContext, _clock, _notificationService, _logger);
            var request = new Request
            {
                Status = RequestStatus.Approved,
                Complaints = new List<Complaint>() { new Complaint()}
            };
            Assert.Throws<InvalidOperationException>(() =>
                service.Create(request, 1, null, "apa", ComplaintType.BadDelivery));
        }

        [Theory]
        [InlineData(RequestStatus.Approved)]
        public void Create(RequestStatus status)
        {
            var service = new ComplaintService(_tolkDbContext, _clock, _notificationService, _logger);
            var request = new Request
            {
                Status = status,
                Complaints = new List<Complaint>()
            };
            service.Create(request, 1, null, "apa", ComplaintType.BadDelivery);
        }

        [Theory]
        [InlineData(ComplaintStatus.Confirmed)]
        [InlineData(ComplaintStatus.Disputed)]
        public void Accept_InvalidStatus(ComplaintStatus status)
        {
            var service = new ComplaintService(_tolkDbContext, _clock, _notificationService, _logger);
            var complaint = new Complaint
            {
                Status = status
            };
            Assert.Throws<InvalidOperationException>(() =>
                service.Accept(complaint, 1, null));
        }

        [Theory]
        [InlineData(ComplaintStatus.Created)]
        public void Accept(ComplaintStatus status)
        {
            var service = new ComplaintService(_tolkDbContext, _clock, _notificationService, _logger);
            var complaint = new Complaint
            {
                Status = status
            };
            service.Accept(complaint, 1, null);
        }
    }
}
