using System.ComponentModel;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum CompetenceAndSpecialistLevel
    {
    	[CustomName("no_interpreter", useInApi: false)]
        [Description("Tolk ej tillsatt")]
        NoInterpreter = 0,

        [CustomName("other_interpreter")]
        [Parent(CompetenceLevel.OtherInterpreter)]
        [Description("Övrig tolk")]
        OtherInterpreter = 1,

        [CustomName("educated_interpreter")]
        [Parent(CompetenceLevel.EducatedInterpreter)]
        [Description("Utbildad tolk")]
        EducatedInterpreter = 2,

        [CustomName("authorized_interpreter")]
        [TellusName("1", "auktoriserad tolk")]
        [Parent(CompetenceLevel.AuthorizedInterpreter)]
        [Description("Auktoriserad tolk")]
        AuthorizedInterpreter = 3,

        [CustomName("health_care_specialist_interpreter")]
        [TellusName("2", "sjukvårdstolk")]
        [Parent(CompetenceLevel.SpecializedInterpreter)]
        [Description("Sjukvårdstolk")]
        HealthCareSpecialist = 4,

        [CustomName("legal_specialist_interpreter")]
        [TellusName("3", "rättstolk")]
        [Parent(CompetenceLevel.SpecializedInterpreter)]
        [Description("Rättstolk")]
        CourtSpecialist = 5,
    }
}
