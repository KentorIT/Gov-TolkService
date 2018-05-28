using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Helpers;
using Xunit;
using FluentAssertions;
using Tolk.BusinessLogic.Data;
using Microsoft.EntityFrameworkCore;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Tests.Helpers
{
    public class DateCalculationServiceTests
    {
        private TolkDbContext CreateTolkDbContext(string databaseName = "empty")
        {
            var options = new DbContextOptionsBuilder<TolkDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new TolkDbContext(options);
        }

        [Fact]
        public void GetWorkDaysBetween_ThrowsIfFirstDateIsAfterSecondDate()
        {
            using (var tolkDbContext = CreateTolkDbContext())
            {
                var subject = new DateCalculationService(tolkDbContext);

                Action a = () => subject.GetWorkDaysBetween(new DateTime(1), new DateTime(0));

                a.Should().Throw<ArgumentException>();
            }
        }

        [Fact]
        public void GetWorkDaysBetween_ThrowsIfFirstDateIsNotPlainDate()
        {
            using (var tolkDbContext = CreateTolkDbContext())
            {
                var subject = new DateCalculationService(tolkDbContext);

                Action a = () => subject.GetWorkDaysBetween(new DateTime(2018, 06, 04, 14, 00, 00), new DateTime(2018, 06, 05));

                a.Should().Throw<ArgumentException>()
                    .And.ParamName.Should().Be("firstDate");
            }
        }

        [Fact]
        public void GetWorkDaysBetween_ThrowsIfSecondDateIsNotPlainDate()
        {
            using (var tolkDbContext = CreateTolkDbContext())
            {
                var subject = new DateCalculationService(tolkDbContext);

                Action a = () => subject.GetWorkDaysBetween(new DateTime(2018, 06, 04), new DateTime(2018, 06, 05, 14, 00, 00));

                a.Should().Throw<ArgumentException>()
                    .And.ParamName.Should().Be("secondDate");
            }
        }

        [Fact]
        public void GetWorkDaysBetween_ThrowsDateTimeKindsAreDifferent()
        {
            using (var tolkDbContext = CreateTolkDbContext())
            {
                var subject = new DateCalculationService(tolkDbContext);

                Action a = () => subject.GetWorkDaysBetween(
                new DateTime(2018, 5, 1, 0, 0, 0, DateTimeKind.Local),
                new DateTime(2018, 6, 1, 0, 0, 0, DateTimeKind.Unspecified));

                a.Should().Throw<ArgumentException>();
            }
        }

        [Theory]
        // Note 2016-08-01 was a Monday.
        [InlineData("2016-08-01", "2016-08-04", 3)] // Monday-Thursday
        [InlineData("2016-08-01", "2016-08-08", 5)] // Monday-Monday
        [InlineData("2016-08-04", "2016-08-08", 2)] // Thursday-Monday
        [InlineData("2016-08-04", "2016-08-07", 2)] // Thursday-Sunday
        [InlineData("2016-08-06", "2016-08-09", 1)] // Saturday-Tuesday
        [InlineData("2016-08-07", "2016-08-09", 1)] // Sunday-Tuesday
        [InlineData("2016-08-01", "2016-08-06", 5)] // Monday-Saturday
        [InlineData("2016-08-06", "2016-08-07", 0)] // Saturday-Sunday
        [InlineData("2016-08-06", "2016-08-14", 5)] // Saturday-next Sunday
        [InlineData("2016-08-07", "2016-08-13", 5)] // Sunday-Saturday
        [InlineData("2018-03-28", "2018-04-03", 2)] // Wednesday-Tuesday over Easter weekend
        [InlineData("2018-04-27", "2018-05-02", 2)] // Friday-Wednesday over 1st of May (tuesday)
        [InlineData("2018-05-10", "2018-05-12", 1)] // Thursday-Saturday over Ascension day
        public void GetWorkDaysBefore(string firstDate, string secondDate, int actual)
        {
            using (var tolkDbContext = CreateTolkDbContext("DateCalculationService_GetWorkDaysBetween"))
            {
                var holidays = new[] {
                    new Holiday() { Date = new DateTime(2018,03,29), DateType=DateType.DayBeforeBigHoliday},
                    new Holiday() { Date = new DateTime(2018,03,30), DateType=DateType.BigHolidayFullDay},
                    new Holiday() { Date = new DateTime(2018,04,01), DateType=DateType.BigHolidayFullDay},
                    new Holiday() { Date = new DateTime(2018,04,02), DateType=DateType.BigHolidayFullDay},
                    new Holiday() { Date = new DateTime(2018,04,03), DateType=DateType.DayAfterBigHoliday},
                    new Holiday() { Date = new DateTime(2018,05,01), DateType=DateType.Holiday},
                    new Holiday() { Date = new DateTime(2018,05,10), DateType=DateType.Holiday},
                    new Holiday() { Date = new DateTime(2018,05,18), DateType=DateType.DayBeforeBigHoliday},
                    new Holiday() { Date = new DateTime(2018,05,19), DateType=DateType.BigHolidayFullDay},
                };

                tolkDbContext.AddRange(holidays.Where(newHoliday =>
                !tolkDbContext.Holidays.Select(existingHoliday => existingHoliday.Date).Contains(newHoliday.Date)));

                tolkDbContext.SaveChanges();
            }

            using (var tolkDbContext = CreateTolkDbContext("DateCalculationService_GetWorkDaysBetween"))
            {
                var subject = new DateCalculationService(tolkDbContext);

                subject.GetWorkDaysBetween(DateTime.Parse(firstDate), DateTime.Parse(secondDate))
                .Should().Be(actual, "there are {0} workdays between {1} and {2}", actual, firstDate, secondDate);
            }
        }
    }
}
