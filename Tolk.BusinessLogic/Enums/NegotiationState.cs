using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum NegotiationState
    {
        UnderNegotiation = 1,
        ContractValid = 2,
        TerminatedPrematurely = 3,
        ReplacedByOtherEntity = 4
    }
}
