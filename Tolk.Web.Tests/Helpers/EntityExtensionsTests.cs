using FluentAssertions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;
using Tolk.Web.Models.AccountViewModels;
using Xunit;

namespace Tolk.Web.Tests.Helpers
{
    public class ValidationAttributeTests
    {

        public ValidationAttributeTests()
        {
        }

        [Theory]
        [InlineData("apa.com", "test@apa.com", true, true)]
        [InlineData("bepa.com", "test@apa.com", true, false)]
        [InlineData("apa.com", "test@apa.com", false, true)]
        [InlineData("bepa.com", "test@apa.com", false, true)]
        [InlineData("apa.com", "test", true, true)]
        [InlineData("apa.com", "test", false, true)]
        [InlineData("apa.com", null, true, true)]
        [InlineData(null, "test@apa.com", true, true)]
        public void CustomerOrdersWithUser(string currentDomain, string newEmail, bool check, bool validAnswer)
        {
            var target = new ValidationTarget { X = check, Domain = currentDomain, Test = newEmail };
            var context = new ValidationContext(target);
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(target, context, results, true).Should().Be(validAnswer);
        }

        private class ValidationTarget
        {
            public bool X { get; set; }
            public string Domain { get; set; }

            [RequireSameEmailDomain(ValidEmailDomainProperty = nameof(Domain), ValidateIfTrue = nameof(X))]
            public string Test { get; set; }
        }
    }
}
