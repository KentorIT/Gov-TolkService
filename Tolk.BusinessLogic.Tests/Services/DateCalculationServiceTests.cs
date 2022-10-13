using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Services
{
    public class DateCalculationServiceTests
    {
        private const string DbNameWithHolidays = "DateCalculationService_WithHolidays";
        private readonly StubSwedishClock _clock;

        public DateCalculationServiceTests()
        {
            using var tolkDbContext = CreateTolkDbContext(DbNameWithHolidays);
            tolkDbContext.AddRange(MockEntities.Holidays.Where(newHoliday =>
            !tolkDbContext.Holidays.Select(existingHoliday => existingHoliday.Date).Contains(newHoliday.Date)));
            tolkDbContext.SaveChanges();
            _clock = new StubSwedishClock("2018-12-12 00:00:00");

        }

        private TolkDbContext CreateTolkDbContext(string databaseName = "empty")
        {
            var options = new DbContextOptionsBuilder<TolkDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new TolkDbContext(options);
        }
        private CacheService CreateCacheService(TolkDbContext dbContext)
        {
            IDistributedCache cache = Mock.Of<IDistributedCache>();
            TolkBaseOptionsService optionService = new TolkBaseOptionsService(Options.Create(new TolkOptions() { RoundPriceDecimals = true }));
            return new CacheService(cache, dbContext, optionService, _clock);
        }

        [Fact]
        public void GetWorkDaysBetween_ThrowsIfFirstDateIsAfterSecondDate()
        {
            using var tolkDbContext = CreateTolkDbContext();
            var subject = new DateCalculationService(CreateCacheService(tolkDbContext));

            Action a = () => subject.GetWorkDaysBetween(new DateTime(1), new DateTime(0));

            a.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetWorkDaysBetween_ThrowsIfFirstDateIsNotPlainDate()
        {
            using var tolkDbContext = CreateTolkDbContext();
            var subject = new DateCalculationService(CreateCacheService(tolkDbContext));

            Action a = () => subject.GetWorkDaysBetween(new DateTime(2018, 06, 04, 14, 00, 00), new DateTime(2018, 06, 05));

            a.Should().Throw<ArgumentException>()
                .And.ParamName.Should().Be("firstDate");
        }

        [Fact]
        public void GetWorkDaysBetween_ThrowsIfSecondDateIsNotPlainDate()
        {
            using var tolkDbContext = CreateTolkDbContext();
            var subject = new DateCalculationService(CreateCacheService(tolkDbContext));

            Action a = () => subject.GetWorkDaysBetween(new DateTime(2018, 06, 04), new DateTime(2018, 06, 05, 14, 00, 00));

            a.Should().Throw<ArgumentException>()
                .And.ParamName.Should().Be("secondDate");
        }

        [Fact]
        public void GetWorkDaysBetween_ThrowsDateTimeKindsAreDifferent()
        {
            using var tolkDbContext = CreateTolkDbContext();
            var subject = new DateCalculationService(CreateCacheService(tolkDbContext));

            Action a = () => subject.GetWorkDaysBetween(
            new DateTime(2018, 5, 1, 0, 0, 0, DateTimeKind.Local),
            new DateTime(2018, 6, 1, 0, 0, 0, DateTimeKind.Unspecified));

            a.Should().Throw<ArgumentException>();
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
        [InlineData("2016-08-05", "2016-08-07", 1)] // Friday-Sunday 
        [InlineData("2016-08-04", "2016-08-05", 1)] // Thursday-Friday 
        [InlineData("2016-08-05", "2016-08-08", 1)] // Friday-Monday 
        [InlineData("2016-08-06", "2016-08-08", 0)] // Saturday-Monday 
        [InlineData("2016-08-06", "2016-08-14", 5)] // Saturday-next Sunday
        [InlineData("2016-08-07", "2016-08-13", 5)] // Sunday-Saturday
        [InlineData("2018-03-28", "2018-04-03", 2)] // Wednesday-Tuesday over Easter weekend
        [InlineData("2018-04-27", "2018-05-02", 2)] // Friday-Wednesday over 1st of May (tuesday)
        [InlineData("2018-05-10", "2018-05-12", 1)] // Thursday-Saturday over Ascension day
        [InlineData("2018-08-01", "2018-08-31", 22)] //August month
        public void GetWorkDaysBefore(string firstDate, string secondDate, int actual)
        {
            using var tolkDbContext = CreateTolkDbContext(DbNameWithHolidays);
            var subject = new DateCalculationService(CreateCacheService(tolkDbContext));

            subject.GetWorkDaysBetween(DateTime.Parse(firstDate), DateTime.Parse(secondDate))
            .Should().Be(actual, "there are {0} workdays between {1} and {2}", actual, firstDate, secondDate);
        }

        [Theory]
        [InlineData("2018-09-05 16:00:00", "2018-09-07 16:00:00", 2)] // Wednesday-Friday exacy 48h (2 24h period)
        [InlineData("2018-09-05 16:00:00", "2018-09-07 17:00:00", 2)] // Wednesday-Friday more than 48h (2 24h period)
        [InlineData("2018-09-05 16:00:00", "2018-09-07 15:00:00", 1)] // Wednesday-Friday less than 48h (1 24h period)

        [InlineData("2018-09-03 16:00:00", "2018-09-04 16:00:00", 1)] // Monday-Tuesday exact 24h (1 24h period)
        [InlineData("2018-09-03 16:00:00", "2018-09-04 17:00:00", 1)] // Monday-Tuesday more than 24h but less than 48h (1 24h period)
        [InlineData("2018-09-03 16:00:00", "2018-09-04 15:00:00", 0)] // Monday-Tuesday less than 24h (0 24h period)

        [InlineData("2018-09-03 16:00:00", "2018-09-07 16:00:00", 4)] // Monday-Friday exact 96h (4 24h period)
        [InlineData("2018-09-03 16:00:00", "2018-09-07 17:00:00", 4)] // Monday-Friday more than 96h but less than 120h (4 24h period)
        [InlineData("2018-09-03 16:00:00", "2018-09-07 15:00:00", 3)] // Monday-Friday less than 96h (3 24h period)

        [InlineData("2018-09-07 16:00:00", "2018-09-08 16:00:00", 0)] // Friday-Saturday exact 24h, but not working days hours (0 24h period)
        [InlineData("2018-09-07 16:00:00", "2018-09-08 17:00:00", 0)] // Friday-Saturday more than 24h, but not working days hours (0 24h period)
        [InlineData("2018-09-07 16:00:00", "2018-09-08 15:00:00", 0)] // Friday-Saturday less than 24h, but not working days hours (0 24h period)

        [InlineData("2018-09-07 16:00:00", "2018-09-09 16:00:00", 0)] // Friday-Sunday exact 48h, but not working days hours (0 24h period)
        [InlineData("2018-09-07 16:00:00", "2018-09-09 17:00:00", 0)] // Friday-Sunday more than 48h, but not working days hours (0 24h period)
        [InlineData("2018-09-07 16:00:00", "2018-09-09 15:00:00", 0)] // Friday-Sunday less than 48h, but not working days hours (0 24h period)

        [InlineData("2018-09-08 16:00:00", "2018-09-09 16:00:00", 0)] // Saturday-Sunday exact 24h, but not working days hours (0 24h period)
        [InlineData("2018-09-08 16:00:00", "2018-09-09 17:00:00", 0)] // Saturday-Sunday more than 24h, but not working days hours (0 24h period)
        [InlineData("2018-09-08 16:00:00", "2018-09-09 15:00:00", 0)] // Saturday-Sunday less than 24h, but not working days hours (0 24h period)

        [InlineData("2018-09-07 16:00:00", "2018-09-10 16:00:00", 1)] // Friday-Monday exact 72h, but only 24 working days hours (1 24h period)
        [InlineData("2018-09-07 16:00:00", "2018-09-10 17:00:00", 1)] // Friday-Monday more than 72h, but only 24 working days hours (1 24h period)
        [InlineData("2018-09-07 16:00:00", "2018-09-10 15:00:00", 0)] // Friday-Monday less than 72h, less than 24 working days hours (0 24h period)

        [InlineData("2018-09-07 16:00:00", "2018-09-11 16:00:00", 2)] // Friday-Tuesday exact 96h, but only 48 working days hours (2 24h period)
        [InlineData("2018-09-07 16:00:00", "2018-09-11 17:00:00", 2)] // Friday-Tuesday more than 96h, but only 48 working days hours (2 24h period)
        [InlineData("2018-09-07 16:00:00", "2018-09-11 15:00:00", 1)] // Friday-Tuesday less than 96h, less than 48 working days hours (1 24h period)

        [InlineData("2018-09-08 16:00:00", "2018-09-15 16:00:00", 5)] // Saturday-Saturday exact 5 24h period
        [InlineData("2018-09-08 16:00:00", "2018-09-15 17:00:00", 5)] // Saturday-Saturday more than 5 but less than 6 24h period
        [InlineData("2018-09-08 16:00:00", "2018-09-15 15:00:00", 5)] // Saturday-Saturday more than 5 but less than 6 24h period

        [InlineData("2018-09-03 16:00:00", "2018-09-10 16:00:00", 5)] // Monday-Monday exact 5 24h period
        [InlineData("2018-09-03 16:00:00", "2018-09-10 17:00:00", 5)] // Monday-Monday more than 5 but less than 6 24h period
        [InlineData("2018-09-03 16:00:00", "2018-09-10 11:00:00", 4)] // Monday-Monday less than 5, 4 24h period

        [InlineData("2018-09-05 16:00:00", "2018-09-14 16:00:00", 7)] // Wednesday-Friday (7 24h period)
        [InlineData("2018-09-05 16:00:00", "2018-09-14 17:00:00", 7)] // Wednesday-Friday (7 24h period)
        [InlineData("2018-09-05 16:00:00", "2018-09-14 15:00:00", 6)] // Wednesday-Friday (6 24h period)

        [InlineData("2018-03-30 16:00:00", "2018-04-02 16:00:00", 0)] // Easter period (Friday to easter Monday) 0 24h period
        [InlineData("2018-03-30 16:00:00", "2018-04-02 17:00:00", 0)] // Easter period (Friday to easter Monday) 0 24h period
        [InlineData("2018-03-30 16:00:00", "2018-04-02 15:00:00", 0)] // Easter period (Friday to easter Monday) 0 24h period
        [InlineData("2018-03-29 16:00:00", "2018-04-02 16:00:00", 0)] // Easter period (Thursday to easter Monday) 0 24h period
        [InlineData("2018-03-29 16:00:00", "2018-04-02 17:00:00", 0)] // Easter period (Thursday to easter Monday) 0 24h period
        [InlineData("2018-03-29 16:00:00", "2018-04-02 15:00:00", 0)] // Easter period (Thursday to easter Monday) 0 24h period
        [InlineData("2018-03-28 16:00:00", "2018-04-02 16:00:00", 1)] // Easter period (Wednesday to easter Monday) 1 24h period
        [InlineData("2018-03-28 16:00:00", "2018-04-02 17:00:00", 1)] // Easter period (Wednesday to easter Monday) 1 24h period
        [InlineData("2018-03-28 16:00:00", "2018-04-02 15:00:00", 1)] // Easter period (Wednesday to easter Monday) 1 24h period
        [InlineData("2018-03-27 16:00:00", "2018-04-02 16:00:00", 2)] // Easter period (Tuesday to easter Monday) 2 24h period
        [InlineData("2018-03-27 16:00:00", "2018-04-02 17:00:00", 2)] // Easter period (Tuesday to easter Monday) 2 24h period
        [InlineData("2018-03-27 16:00:00", "2018-04-02 15:00:00", 2)] // Easter period (Tuesday to easter Monday) 2 24h period
        [InlineData("2018-03-27 16:00:00", "2018-04-03 16:00:00", 3)] // Easter period (Tuesday to Tuesday after easter Monday) 3 24h period
        [InlineData("2018-03-27 16:00:00", "2018-04-03 17:00:00", 3)] // Easter period (Tuesday to Tuesday after easter Monday) 3 24h period
        [InlineData("2018-03-27 16:00:00", "2018-04-03 15:00:00", 2)] // Easter period (Tuesday to Tuesday after easter Monday) 2 24h period
        [InlineData("2018-04-02 16:00:00", "2018-04-05 16:00:00", 2)] // Easter period (Monday to Thursday) 2 24h period

        //try seconds and thousands of seconds
        [InlineData("2018-09-05 16:00:03.002", "2018-09-07 16:00:03.009", 2)] // Wednesday-Friday more than 48h (2 24h period)
        [InlineData("2018-09-05 16:00:03.009", "2018-09-07 16:00:03.002", 1)] // Wednesday-Friday less than 48h (1 24h period)
        [InlineData("2018-09-05 16:00:03", "2018-09-07 16:00:04", 2)] // Wednesday-Friday  more than 48h (2 24h period)
        [InlineData("2018-09-05 16:00:04", "2018-09-07 16:00:03", 1)] // Wednesday-Friday less than (1 24h period)
        [InlineData("2018-09-05 16:00:00", "2018-09-05 18:00:00", 0)] // Part of day Wednesday (0 24h period)
        public void GetNoOf24HsPeriodsOfWorkDaysBetween(string firstDate, string secondDate, int actual)
        {
            using var tolkDbContext = CreateTolkDbContext(DbNameWithHolidays);
            var subject = new DateCalculationService(CreateCacheService(tolkDbContext));

            subject.GetNoOf24HsPeriodsWorkDaysBetween(DateTime.Parse(firstDate), DateTime.Parse(secondDate))
            .Should().Be(actual, "there are {0} full 24h periods of workday time between {1} and {2}", actual, firstDate, secondDate);
        }

        [Theory]
        [InlineData("2018-09-03 16:00:00", "2018-09-04 16:00:00", 24)] // Monday-Tuesday exact 24h
        [InlineData("2018-09-03 16:00:00", "2018-09-04 17:00:00", 25)] // Monday-Tuesday more than 24h +1
        [InlineData("2018-09-03 16:00:00", "2018-09-04 15:00:00", 23)] // Monday-Tuesday less than 24h -1

        [InlineData("2018-09-05 16:00:00", "2018-09-07 16:00:00", 48)] // Wednesday-Friday exact 48h 
        [InlineData("2018-09-05 16:00:00", "2018-09-07 17:00:00", 49)] // Wednesday-Friday more than 48h +1 
        [InlineData("2018-09-05 16:00:00", "2018-09-07 15:00:00", 47)] // Wednesday-Friday less than 48h -1 

        [InlineData("2018-09-07 16:00:00", "2018-09-08 16:00:00", 8)] // Friday-Saturday exact 24h, only 8 work hours of friday
        [InlineData("2018-09-07 16:00:00", "2018-09-08 17:00:00", 8)] // Friday-Saturday more than 24h, only 8 work hours of friday
        [InlineData("2018-09-07 16:00:00", "2018-09-08 15:00:00", 8)] // Friday-Saturday less than 24h,  only 8 work hours of friday

        [InlineData("2018-09-07 16:00:00", "2018-09-09 16:00:00", 8)] // Friday-Sunday 8 work hours of friday
        [InlineData("2018-09-07 16:00:00", "2018-09-09 17:00:00", 8)] // Friday-Sunday 8 work hours of friday
        [InlineData("2018-09-07 16:00:00", "2018-09-09 15:00:00", 8)] // Friday-Sunday 8 work hours of friday
        [InlineData("2018-09-07 17:00:00", "2018-09-09 15:00:00", 7)] // Friday-Sunday 7 work hours of friday

        [InlineData("2018-09-08 16:00:00", "2018-09-09 16:00:00", 0)] // Saturday-Sunday exact 24h, but not working days hours (0h period)
        [InlineData("2018-09-08 16:00:00", "2018-09-09 17:00:00", 0)] // Saturday-Sunday more than 24h, but not working days hours (0h period)
        [InlineData("2018-09-08 16:00:00", "2018-09-09 15:00:00", 0)] // Saturday-Sunday less than 24h, but not working days hours (0h period)

        [InlineData("2018-09-07 16:00:00", "2018-09-10 16:00:00", 24)] // Friday-Monday exact 72h, but only 24 working days hours (1 * 24h)
        [InlineData("2018-09-07 16:00:00", "2018-09-10 17:00:00", 25)] // Friday-Monday more than 72h, but only 24 working days hours (1*24h + 1)
        [InlineData("2018-09-07 16:00:00", "2018-09-10 15:00:00", 23)] // Friday-Monday less than 72h, less than 24 working days hours (1*24h - 1)

        [InlineData("2018-09-07 16:00:00", "2018-09-11 16:00:00", 48)] // Friday-Tuesday 48h
        [InlineData("2018-09-07 16:00:00", "2018-09-11 17:00:00", 49)] // Friday-Tuesday 48 + 1h
        [InlineData("2018-09-07 16:00:00", "2018-09-11 15:00:00", 47)] // Friday-Tuesday 48 - 1h

        [InlineData("2018-09-08 16:00:00", "2018-09-15 16:00:00", 120)] // Saturday-Saturday 5 * 24h 
        [InlineData("2018-09-08 16:00:00", "2018-09-15 17:00:00", 120)] // Saturday-Saturday 5 * 24h + 1
        [InlineData("2018-09-08 16:00:00", "2018-09-15 15:00:00", 120)] // Saturday-Saturday 5 * 24h - 1

        [InlineData("2018-09-03 16:00:00", "2018-09-10 16:00:00", 120)] // Monday-Monday 5 * 24h
        [InlineData("2018-09-03 16:00:00", "2018-09-10 17:00:00", 121)] // Monday-Monday 5 * 24h + 1
        [InlineData("2018-09-03 16:00:00", "2018-09-10 15:00:00", 119)] // Monday-Monday 5 * 24h - 1

        [InlineData("2018-09-05 16:00:00", "2018-09-14 16:00:00", 168)] // Wednesday-Friday 7 * 24h
        [InlineData("2018-09-05 16:00:00", "2018-09-14 17:00:00", 169)] // Wednesday-Friday 7 * 24h + 1
        [InlineData("2018-09-05 16:00:00", "2018-09-14 15:00:00", 167)] // Wednesday-Friday 7 * 24h - 1

        [InlineData("2018-03-30 16:00:00", "2018-04-02 16:00:00", 0)] // Easter period (Friday to easter Monday) 0h working period
        [InlineData("2018-03-30 16:00:00", "2018-04-02 17:00:00", 0)] // Easter period (Friday to easter Monday) 0h working period
        [InlineData("2018-03-30 16:00:00", "2018-04-02 15:00:00", 0)] // Easter period (Friday to easter Monday) 0h working period
        [InlineData("2018-03-29 16:00:00", "2018-04-02 16:00:00", 8)] // Easter period (Thursday to easter Monday) 8h Thursday
        [InlineData("2018-03-29 16:00:00", "2018-04-02 17:00:00", 8)] // Easter period (Thursday to easter Monday) 8h Thursday
        [InlineData("2018-03-29 16:00:00", "2018-04-02 15:00:00", 8)] // Easter period (Thursday to easter Monday) 8h Thursday
        [InlineData("2018-03-28 16:00:00", "2018-04-02 16:00:00", 32)] // Easter period (Wednesday to easter Monday) 24h + 8h Thursday
        [InlineData("2018-03-28 16:00:00", "2018-04-02 17:00:00", 32)] // Easter period (Wednesday to easter Monday) 24h + 8h Thursday
        [InlineData("2018-03-28 16:00:00", "2018-04-02 15:00:00", 32)] // Easter period (Wednesday to easter Monday) 24h + 8h Thursday
        [InlineData("2018-03-27 16:00:00", "2018-04-02 16:00:00", 56)] // Easter period (Tuesday to easter Monday) 2 24h period
        [InlineData("2018-03-27 16:00:00", "2018-04-02 17:00:00", 56)] // Easter period (Tuesday to easter Monday) 2 24h period
        [InlineData("2018-03-27 16:00:00", "2018-04-02 15:00:00", 56)] // Easter period (Tuesday to easter Monday) 2 24h period
        [InlineData("2018-03-27 16:00:00", "2018-04-03 16:00:00", 72)] // Easter period (Tuesday to Tuesday after easter Monday) 3*24h 
        [InlineData("2018-03-27 16:00:00", "2018-04-03 17:00:00", 73)] // Easter period (Tuesday to Tuesday after easter Monday) 3*24h +1 
        [InlineData("2018-03-27 16:00:00", "2018-04-03 15:00:00", 71)] // Easter period (Tuesday to Tuesday after easter Monday) 3*24h -1
        [InlineData("2018-04-02 16:00:00", "2018-04-05 16:00:00", 64)] // Easter period (Monday to Thursday) 2*24h + 16h thursday

        //try seconds and thousends of seconds
        [InlineData("2018-09-05 16:00:03.002", "2018-09-07 16:00:03.009", 48)] // Wednesday-Friday more than 48h but less than 49
        [InlineData("2018-09-05 16:00:03.009", "2018-09-07 16:00:03.002", 47)] // Wednesday-Friday less than 48h
        [InlineData("2018-09-05 16:00:03", "2018-09-07 16:00:04", 48)] // Wednesday-Friday more than 48h
        [InlineData("2018-09-05 16:00:04", "2018-09-07 16:00:03", 47)] // Wednesday-Friday less than 48h

        //part of day
        [InlineData("2018-09-05 16:00:00", "2018-09-05 18:00:00", 2)] // Wednesday (2h period)
        [InlineData("2018-09-05 16:00:00", "2018-09-05 16:20:00", 0)] // Wednesday less than 1 hour
        [InlineData("2018-09-09 16:00:00", "2018-09-09 18:00:00", 0)] // Sunday (2h but non work day)
        [InlineData("2018-04-02 16:00:00", "2018-04-02 19:00:00", 0)] // Easter Friday (3h but non work day)
        public void GetNoOfHoursOfWorkDaysBetween(string firstDate, string secondDate, int actual)
        {
            using var tolkDbContext = CreateTolkDbContext(DbNameWithHolidays);
            var subject = new DateCalculationService(CreateCacheService(tolkDbContext));

            subject.GetNoOfHoursOfWorkDaysBetween(DateTime.Parse(firstDate), DateTime.Parse(secondDate))
            .Should().Be(actual, "there are {0} hours of workday time between {1} and {2}", actual, firstDate, secondDate);
        }

        [Theory]
        [InlineData("2018-09-08", "2018-09-07")] //Normal Saturday
        [InlineData("2018-09-09", "2018-09-07")] //Normal Sunday
        [InlineData("2018-04-02", "2018-03-29")] //Easter Monday
        [InlineData("2018-05-01", "2018-04-30")] //First of May Holiday
        [InlineData("2018-09-05", "2018-09-05")] //Normal work day

        public void GetLastWorkDay(string date, string expected)
        {
            using var tolkDbContext = CreateTolkDbContext(DbNameWithHolidays);
            var subject = new DateCalculationService(CreateCacheService(tolkDbContext));

            subject.GetLastWorkDay(DateTime.Parse(date))
                .Should().Be(DateTime.Parse(expected), "that is the last workday before {0}", date);
        }

        [Theory]
        [InlineData("2018-09-08", false)] //Normal Saturday
        [InlineData("2018-09-09", false)] //Normal Sunday
        [InlineData("2018-04-02", false)] //Easter Monday
        [InlineData("2018-05-01", false)] //First of May Holiday
        [InlineData("2018-09-05", true)] //Normal work day

        public void IsWorkDay(string date, bool expected)
        {
            using var tolkDbContext = CreateTolkDbContext(DbNameWithHolidays);
            var subject = new DateCalculationService(CreateCacheService(tolkDbContext));
            string errorMessage = expected ? "should be a workday" : "should not be a workday";
            subject.IsWorkingDay(DateTime.Parse(date)).Should().Be(expected, "{0} {1}", date, errorMessage);
        }

        [Theory]
        [InlineData("2016-08-01", "2016-08-01")]
        [InlineData("2016-08-06", "2016-08-08")]
        [InlineData("2016-08-07", "2016-08-08")]
        [InlineData("2018-03-30", "2018-04-03")]
        [InlineData("2018-09-05", "2018-09-05")]//Normal work day
        public void GetFirstWorkDay(string date, string expected)
        {
            using var tolkDbContext = CreateTolkDbContext(DbNameWithHolidays);
            var subject = new DateCalculationService(CreateCacheService(tolkDbContext));
            subject.GetFirstWorkDay(DateTime.Parse(date)).Should().Be(DateTime.Parse(expected), "that is the first workday after {0}", date);
        }

        [Theory]
        [InlineData("2021-10-04 10:00:00", "2021-09-30 10:00:00", 2)]
        [InlineData("2021-10-15 10:00:00", "2021-10-13 10:00:00", 2)]
        [InlineData("2021-10-12 10:00:00", "2021-10-08 10:00:00", 2)]
        [InlineData("2021-10-13 10:00:00", "2021-10-11 10:00:00", 2)]
        [InlineData("2021-10-07 10:00:00", "2021-10-05 10:00:00", 2)]//Normal work day
        public void GetDateForANumberOfWorkdaysAgo(string date, string expected, int numberOfAddedDays)
        {
            using var tolkDbContext = CreateTolkDbContext(DbNameWithHolidays);
            var subject = new DateCalculationService(CreateCacheService(tolkDbContext));
            subject.GetDateForANumberOfWorkdaysAgo(DateTime.Parse(date), numberOfAddedDays).Should().Be(DateTime.Parse(expected));
        }
    }
}
