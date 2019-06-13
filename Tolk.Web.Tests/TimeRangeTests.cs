using System;
using Tolk.Web.Models;
using Tolk.BusinessLogic.Helpers;
using Xunit;
using FluentAssertions;

namespace Tolk.Web.Tests
{
    public class TimeRangeTests
    {
        [Theory]
        [InlineData("16:00", "2018-04-19")]
        [InlineData("01:00", "2018-04-20")]
        [InlineData("14:59:59", "2018-04-20")]
        [InlineData("15:00", "2018-04-19")]
        public void EndDateCalculation(string endTime, string expectedEndDate)
        {
            var subject = new TimeRange
            {
                StartDate = new DateTime(2018, 04, 19),
                StartTime = TimeSpan.FromHours(15),
                EndTime = TimeSpan.Parse(endTime)
            };

            subject.EndDateTime.Value.Date.ToShortDateString().Should().Be(expectedEndDate);

            subject.StartDateTime.ToString().Should().Be("2018-04-19 15:00:00 +02:00");
        }

        [Theory]
        [InlineData("2018-04-19 16:00:00 +02:00", false)]
        [InlineData("2018-04-19 14:59:59 +02:00", true)]
        [InlineData("2018-04-20 15:00:00 +02:00", true)]
        public void EndDateTimeSetter(string endTime, bool shouldThrow)
        {
            var subject = new TimeRange
            {
                StartDate = new DateTime(2018, 04, 19),
                StartTime = TimeSpan.FromHours(15)
            };

            Action a = () => subject.EndDateTime = DateTimeOffset.Parse(endTime);

            if(shouldThrow)
            {
                a.Should().Throw<InvalidOperationException>();
            }
            else
            {
                a.Should().NotThrow();
            }
        }
    }
}
