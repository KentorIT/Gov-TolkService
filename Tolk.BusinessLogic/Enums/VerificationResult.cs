using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tolk.BusinessLogic.Enums
{
    public enum VerificationResult
    {
        #region Valid
        // 1xx = valid 
        // Interpreter is undoubtedly valid
        [Display(Name = "Tolk validerad", Description = "Tolken finns i Kammarkollegiets tolkregister, och har den eftersökta kompetensen.")]
        Validated = 100,
        #endregion

        #region Not valid
        // 2xx = not valid
        // Interpreter is undoubtedly not found or not valid
        [Display(Name = "Tolk finns ej", Description = "Tolken finns ej i Kammarkollegiets tolkregister.")]
        NotFound = 200,
        [Display(Name = "Tolk har ej eftersökt kompetens", Description = "Tolken finns i Kammarkollegiets tolkregister, men saknar den eftersökta kompetensen.")]
        NotCorrectCompetence = 201,
        [Display(Name = "Språk ej registrerat", Description = "Valt språk finns ej registrerat i Kammarkollegiets tolkregister.")]
        LanguageNotRegistered = 202,
        #endregion

        #region Validity undetermined
        // 3xx = validity undetermined
        // Interpreter found and has competence, but validity can't be determined
        [Display(Name = "Kompetens har ingen fastställd utgångstid", Description = "Då giltighetens sluttid ej är fastställd saknas kännedom om tolkens giltighet.")]
        CompetenceValidityExpiryUndefined = 300,
        [Display(Name = "Kompetens har ingen fastställd starttid", Description = "Då giltighetens starttid ej är fastställd saknas kännedom om tolkens giltighet.")]
        CompetenceValidityStartDateUndefined = 301,
        [Display(Name = "Kompetens utgången vid bokningstillfälle", Description = "Tolken är eller har varit giltig, men giltigheten kommer preliminärt att vara utgången vid bokningstillfället.")]
        CompetenceExpiredAtAssignment = 303,
        [Display(Name = "Kompetens giltig efter bokningstillfälle", Description = "Tolken är inte giltig just nu, men förväntas vara giltig efter bokningstillfället.")]
        CompetenceValidAfterAssignment = 302,
        #endregion

        #region Error
        // 4xx = error
        // Non-critical errors when trying to validate interpreter
        [Display(Name = "Fel vid verifiering", Description = "Ett okänt fel inträffade under verifiering av tolk mot Kammarkollegiets tolkregister.")]
        UnknownError = 400,
        [Display(Name = "Uppkopplingsfel vid verifiering", Description = "Uppkoppling mot Kammarkollegiets tolkregister för tolkverifiering misslyckades.")]
        ConnectionError = 401,
        #endregion
    }
}
