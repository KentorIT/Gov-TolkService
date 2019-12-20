using System.ComponentModel;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Entities
{
    public enum CompetenceLevel
    {
        [CustomName("other_interpreter")]
        [Description("Övrig tolk (ÖT)")]
        [ShortDescription("ÖT")]
        OtherInterpreter = 1,

        [CustomName("educated_interpreter")]
        [Description("Utbildad tolk (UT)")]
        [ShortDescription("UT")]
        EducatedInterpreter = 2,

        [CustomName("authorized_interpreter")]
        [Description("Auktoriserad tolk (AT)")]
        [ShortDescription("AT")]
        AuthorizedInterpreter = 3,

        [CustomName("specialist_interpreter")]
        [Description("Rättstolk (RT), Sjukvårdstolk (ST)")]
        [ShortDescription("RT/ST")]
        SpecializedInterpreter = 4
    }
}
