using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;


namespace Tolk.BusinessLogic.Entities
{
    public enum CompetenceLevel
    {
        [Description("Övrig tolk (ÖT)")]
        OtherInterpreter = 1,

        [Description("Utbildad tolk (UT)")]
        EducatedInterpreter = 2,

        [Description("Auktoriserad tolk (AT)")]
        AuthorizedInterpreter = 3,

        [Description("Rättsttolk (RT), Sjukvårdstolk (ST)")]
        SpecializedInterpreter = 4
    }
}
