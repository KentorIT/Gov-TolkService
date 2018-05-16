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
        [Description("Övrig tolk (ÖT)")]
        OtherInterpreter = 1,

        [Parent(CompetenceLevel.EducatedInterpreter)]
        [Description("Utbildad tolk (UT)")]
        EducatedInterpreter = 2,

        [Parent(CompetenceLevel.AuthorizedInterpreter)]
        [Description("Auktoriserad tolk (AT)")]
        AuthorizedInterpreter = 3,

        [Parent(CompetenceLevel.SpecializedInterpreter)]
        [Description("Sjukvårdstolk (ST)")]
        HealthCareSpecialist = 4,

        [Parent(CompetenceLevel.SpecializedInterpreter)]
        [Description("Rättstolk (RT)")]
        CourtSpecialist = 5,
    }
}
