using FluentAssertions;
using System.Linq;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Xunit;
namespace Tolk.BusinessLogic.Tests.Utilities
{
    public class EnumHelperTests
    {
        [Theory]
        [InlineData(NegotiationState.UnderNegotiation, 6)]
        [InlineData(NegotiationState.ContractValid, 4)]
        [InlineData(NegotiationState.TerminatedPrematurely, 10)]
        public void GetEnumsWithParent(NegotiationState type, int expected)
        {
#warning Make a specific enums for tests!
            EnumHelper.GetEnumsWithParent<RequestStatus, NegotiationState>(type).Count().Should().Be(expected);
        }
#warning Make a lot more tests!
    }
}
