using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Helpers;
using Xunit;
using FluentAssertions;

namespace Tolk.BusinessLogic.Tests.Helpers
{
    public class DateTimeHelpersTests
    {
        [Fact]
        public void GetWorkDaysBetween_ThrowsIfFirstDateIsAfterSecondDate()
        {
            Action a = () => DateTimeHelpers.GetWorkDaysBetween(new DateTime(1), new DateTime(0));

            a.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetWorkDaysBetween_ThrowsIfFirstDateIsNotPlainDate()
        {
            Action a = () => DateTimeHelpers.GetWorkDaysBetween(new DateTime(2018, 06, 04, 14, 00, 00), new DateTime(2018, 06, 05));

            a.Should().Throw<ArgumentException>()
                .And.ParamName.Should().Be("firstDate");
        }

        [Fact]
        public void GetWorkDaysBetween_ThrowsIfSecondDateIsNotPlainDate()
        {
            Action a = () => DateTimeHelpers.GetWorkDaysBetween(new DateTime(2018, 06, 04), new DateTime(2018, 06, 05, 14, 00, 00));

            a.Should().Throw<ArgumentException>()
                .And.ParamName.Should().Be("secondDate");
        }

        [Fact]
        public void GetWorkDaysBetween_ThrowsDateTimeKindsAreDifferent()
        {
            Action a = () => DateTimeHelpers.GetWorkDaysBetween(
                new DateTime(2018, 5, 1, 0, 0, 0, DateTimeKind.Local),
                new DateTime(2018, 6, 1, 0, 0, 0, DateTimeKind.Unspecified));

            a.Should().Throw<ArgumentException>();
        }

        [Theory]
        // Note 2016-08-01 was a Monday.
        [InlineData("2016-08-01", "2016-08-04", 3)] // Monday-Thursday
        [InlineData("2016-08-01", "2016-08-08", 5)] // Monday-Monday
        [InlineData("2016-08-04", "2016-08-08", 2)] // Thursday-Monday
        [InlineData("2016-08-06", "2016-08-09", 1)] // Saturday-Tuesday
        [InlineData("2016-08-07", "2016-08-09", 1)] // Sunday-Tuesday
        [InlineData("2016-08-01", "2016-08-06", 4)] // Monday-Saturday
        [InlineData("2016-08-06", "2016-08-07", 0)] // Saturday-Sunday
        [InlineData("2016-08-06", "2016-08-14", 5)] // Saturday-Sunday
        public void GetWorkDaysBetween(string firstDate, string secondDate, int actual)
        {
            DateTimeHelpers.GetWorkDaysBetween(DateTime.Parse(firstDate), DateTime.Parse(secondDate))
                .Should().Be(actual, "there are {0} workdays between {1} and {2}", actual, firstDate, secondDate);
        }
    }
}
