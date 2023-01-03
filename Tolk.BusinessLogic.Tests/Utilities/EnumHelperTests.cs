using FluentAssertions;
using System.Linq;
using Tolk.BusinessLogic.Tests.TestHelpers;
using Tolk.BusinessLogic.Utilities;
using Xunit;
namespace Tolk.BusinessLogic.Tests.Utilities
{
    public class EnumHelperTests
    {
        [Theory]
        [InlineData(TestParent.ParentOne, 2)]
        [InlineData(TestParent.ParentTwo, 4)]
        [InlineData(TestParent.ParentThree, 1)]
        [InlineData(TestParent.ParentFour, 0)]
        public void GetEnumsWithParent(TestParent type, int expected)
        {
            EnumHelper.GetEnumsWithParent<TestChild, TestParent>(type).Count().Should().Be(expected);
        }
#warning Make a lot more tests!
    }
}
