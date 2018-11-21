using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Enums
{
    public enum CompetenceAndSpecialistLevel
    {
        [Parent(CompetenceLevel.OtherInterpreter)]
        [Description("Övrig tolk")]
        OtherInterpreter = 1,

        [Parent(CompetenceLevel.EducatedInterpreter)]
        [Description("Utbildad tolk")]
        EducatedInterpreter = 2,

        [Parent(CompetenceLevel.AuthorizedInterpreter)]
        [Description("Auktoriserad tolk")]
        AuthorizedInterpreter = 3,

        [Parent(CompetenceLevel.SpecializedInterpreter)]
        [Description("Sjukvårdstolk")]
        HealthCareSpecialist = 4,

        [Parent(CompetenceLevel.SpecializedInterpreter)]
        [Description("Rättstolk")]
        CourtSpecialist = 5,
    }
}
