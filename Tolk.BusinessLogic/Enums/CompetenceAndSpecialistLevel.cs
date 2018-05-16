using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{
    public enum CompetenceAndSpecialistLevel
    {
        [Description("Övrig tolk (ÖT)")]
        OtherInterpreter = 1,
        [Description("Utbildad tolk (UT)")]
        EducatedInterpreter = 2,
        [Description("Auktoriserad tolk (AT)")]
        AuthorizedInterpreter = 3,
        [Description("Sjukvårdstolk (ST)")]
        HealthCareSpecialist = 4,
        [Description("Rättstolk (RT)")]
        CourtSpecialist = 5,
    }
}
