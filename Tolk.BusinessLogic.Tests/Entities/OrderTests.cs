using System;
using Xunit;
using FluentAssertions;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;
using System.Linq;
using Tolk.BusinessLogic.Tests.TestHelpers;

namespace Tolk.BusinessLogic.Tests.Entities
{
    public class OrderTests
    {
        [Theory]
        [InlineData("2018-05-25 14:00", "2018-05-23 16:00", "2018-05-24 15:00:00 +02:00")]
        public void CalculateExpiryForNewRequests(
            string startDateTime, string currentDateTime, string expected)
        {
            var subject = new Order
            {
                StartDateTime = DateTime.Parse(startDateTime).ToDateTimeOffsetSweden(),
            };

            var actual = subject.CalculateExpiryForNewRequest(new StubSystemClock(currentDateTime));

            actual.ToString().Should().Be(expected);
        }
    }
}
