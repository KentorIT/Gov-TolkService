using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{

    /*
        NotCorrectCompetence = 201,
        [Display(Name = "Språk ej registrerat", Description = "Tolken finns i Kammarkollegiets tolkregister, men saknar den eftersökta kompetensen.")]
        [Description("Språk ej registrerat")]
        LanguageNotRegistered = 202,
        #endregion
@@ -33,13 +29,10 @@ namespace Tolk.BusinessLogic.Enums
        #region Validity undetermined
        // 3xx = validity undetermined
        // Interpreter found and has competence, but validity can't be determined
        [Display(Name = "Kompetens har ingen fastställd utgångstid", Description = "Tolken finns i Kammarkollegiets tolkregister, men saknar den eftersökta kompetensen.")]
        [Description("Kompetens har ingen fastställd utgångstid")]
        CompetenceValidityExpiryUndefined = 300,
        [Display(Name = "Kompetens utgången vid bokningstillfälle", Description = "Tolken är eller har varit giltig, men giltigheten kommer preliminärt att vara utgången vid bokningstillfället.")]
        [Description("Kompetens utgången vid bokningstillfälle")]
        CompetenceExpiredAtAssignment = 301,
        [Display(Name = "Kompetens giltig efter bokningstillfälle", Description = "Tolken är inte giltig just nu, men förväntas vara giltig efter bokningstillfället.")]
        [Description("Kompetens giltig efter bokningstillfälle")]
        CompetenceValidAfterAssignment = 302,
        #endregion
@@ -47,10 +40,8 @@ namespace Tolk.BusinessLogic.Enums
        #region Error
        // 4xx = error
        // Non-critical errors when trying to validate interpreter
        [Display(Name = "Fel vid verifiering", Description = "Ett okänt fel inträffade under verifiering av tolk mot Kammarkollegiets tolkregister.")]
        [Description("Fel vid verifiering")]
        UnknownError = 400,
        [Display(Name = "Uppkopplingsfel vid verifiering", Description = "Uppkoppling mot Kammarkollegiets tolkregister för tolkverifiering misslyckades.")]
        [Description("Uppkopplingsfel vid verifiering")]
        ConnectionError = 401,
 *      * */
    public enum VerificationResult
    {
        #region Valid
        // 1xx = valid 
        // Interpreter is undoubtedly valid
        //Tolken finns ej i Kammarkollegiets tolkregister.
        [Description("Tolk validerad ok")]
        Validated = 100,
        #endregion

        #region Not valid
        // 2xx = not valid
        // Interpreter is undoubtedly not found or not valid
        //Tolken finns ej i Kammarkollegiets tolkregister.
        [Description("Kompetens kunde ej verifieras")]
        NotFound = 200,
        //Tolken finns i Kammarkollegiets tolkregister, men saknar den eftersökta kompetensen.
        [Description("Kompetens kunde ej verifieras")]
        NotCorrectCompetence = 201,
        [Description("Språk ej registrerat")]
        LanguageNotRegistered = 202,
        #endregion

        #region Validity undetermined
        // 3xx = validity undetermined
        // Interpreter found and has competence, but validity can't be determined
        [Description("Kompetens kunde ej verifieras")]
        CompetenceValidityExpiryUndefined = 300,
        //Tolken är eller har varit giltig, men giltigheten kommer preliminärt att vara utgången vid bokningstillfället.
        [Description("Kompetens kunde ej verifieras")]
        CompetenceExpiredAtAssignment = 301,
        [Description("Kompetens kunde ej verifieras")]
        CompetenceValidAfterAssignment = 302,
        #endregion

        #region Error
        // 4xx = error
        // Non-critical errors when trying to validate interpreter
        [Description("Fel vid verifiering: Det gick inte att nå tolkregistret")]
        UnknownError = 400,
        [Description("Uppkopplingsfel vid verifiering")]
        ConnectionError = 401,
        #endregion
    }
}
