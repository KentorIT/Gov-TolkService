using FluentAssertions;
using System;
using System.Globalization;
using Tolk.BusinessLogic.Helpers;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Helpers
{
    public class DateTimeExtensionsTests
    {
        [Theory]
        [InlineData("2018-05-25 14:00", "2018-05-25 14:00:00 +02:00")]
        [InlineData("2018-01-01 02:00", "2018-01-01 02:00:00 +01:00")]
        [InlineData("2018-03-25 01:59", "2018-03-25 01:59:00 +01:00")]
        [InlineData("2018-03-25 03:00", "2018-03-25 03:00:00 +02:00")]
        [InlineData("2018-10-28 01:59", "2018-10-28 01:59:00 +02:00")]
        [InlineData("2018-10-28 04:00", "2018-10-28 04:00:00 +01:00")]
        public void ToDateTimeOffsetSweden(string input, string expected)
            => DateTime.Parse(input).ToDateTimeOffsetSweden()
            .ToString(new CultureInfo("sv-SE"))
            .Should().Be(expected);
    }
}
