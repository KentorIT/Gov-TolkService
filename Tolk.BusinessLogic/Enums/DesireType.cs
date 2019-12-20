using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum DesireType
    {
        [Description("Önskemål om kompetensnivå")]
        Request = 1,

        [Description("Krav på kompetensnivå")]
        Requirement = 2,
    }
}
