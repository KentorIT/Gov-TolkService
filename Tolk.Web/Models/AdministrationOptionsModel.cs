using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class AdministrationOptionsModel : IModel
    {
        [Display(Name = "Miljöinställningar")]
        public string EnvironmentDisplay { get; set; }
        [Display(Name = "Telluskoppling")]
        public string TellusDisplay { get; set; }
        [Display(Name = "Supportinställningar")]
        public string SupportDisplay { get; set; }
        [Display(Name = "Smtpinställningar")]
        public string SmtpDisplay { get; set; }
        [Display(Name = "Statuskollinställningar")]
        public string StatusCheckerDisplay { get; set; }
        [Display(Name = "Externa länkar")]
        public string ExternalLinksDisplay { get; set; }
        [Display(Name = "Exkluderade notifieringstyper för myndigheter")]
        public string ExcludedNotificationTypesForCustomer { get; set; }
        public int WorkDaysGracePeriodBeforeOrderAgreementCreation { get; set; }
        public int MonthsToApproveComplaints { get; set; }
        public bool AllowDeclineExtraInterpreterOnRequestGroups { get; set; }
        public bool RoundPriceDecimals { get; set; }
        public bool EnableSetLatestAnswerTimeForCustomer { get; set; }
        public bool EnableCustomerApi { get; set; }

        public bool EnableTimeTravel { get; set; }

        public bool EnableOrderGroups { get; set; }

        public bool EnableOrderUpdate { get; set; }

        public bool EnableRegisterUser { get; set; }

        public bool RunEntityScheduler { get; set; }

        public int HourToRunFrameworkAgreementValidation { get; set; }

        public int HourToRunDailyJobs { get; set; }

        [Display(Name = "Tillåtna filändelser för bifogade filer")]
        public string AllowedFileExtensions { get; set; }

        [Display(Name = "Max kombinerad storlek på bifogade filer")]
        public long CombinedMaxSizeAttachments { get; set; }

        [Display(Name = "Använd Stored Procedures för ladding av rapporter")]
        public bool UseStoredProceduresForReports { get; set; }

        [Display(Name = "Root sökväg")]
        public string PublicOrigin { get; set; }

        [Display(Name = "Peppolinställningar")]
        public string PeppolDisplay { get; set; }

        [Display(Name = "Flexibel order inställningar")]
        public string FlexibleOrderDisplay { get; set; }

        [Display(Name = "Påslagna features")]
        public string EnabledFeatures => $"Hopp i tiden: {EnableTimeTravel.ToSwedishString()}\nSammanhållna bokningar: {EnableOrderGroups.ToSwedishString()}\nOrderuppdateringar: {EnableOrderUpdate.ToSwedishString()}\nSjälvregistrering av konto: {EnableRegisterUser.ToSwedishString()}\nApi för myndigheter: {EnableCustomerApi.ToSwedishString()}\nSätta sista svarstid (myndighet): {EnableSetLatestAnswerTimeForCustomer.ToSwedishString()}\nSeparat behandling av extra tolkar: {AllowDeclineExtraInterpreterOnRequestGroups.ToSwedishString()}\nAvrunda priser: {RoundPriceDecimals.ToSwedishString()}";

        [Display(Name = "Entity Scheduler")]
        public string EntitySchedulerSettings => $"Kör Entity scheduler: {RunEntityScheduler.ToSwedishString()}\n\tStarttid dagliga jobb: {HourToRunDailyJobs.ToSwedishString("D2")}\n\tNär på dygnet verifieras systemets avtal: {HourToRunFrameworkAgreementValidation.ToSwedishString("D2")}\n\tAntal månader innan aut. godkännande av reklamationer: {MonthsToApproveComplaints}\n\tAntal dagar efter utfört uppdrag för skapande av ord-agreement: {WorkDaysGracePeriodBeforeOrderAgreementCreation}";

        #region methods

        internal static AdministrationOptionsModel GetModelFromTolkOptions(TolkOptions options)
        {
            return new AdministrationOptionsModel
            {
                EnableOrderUpdate = options.EnableOrderUpdate,
                EnableOrderGroups = options.EnableOrderGroups,
                EnableTimeTravel = options.EnableTimeTravel,
                EnableRegisterUser = options.EnableRegisterUser,
                RunEntityScheduler = options.RunEntityScheduler,
                CombinedMaxSizeAttachments = options.CombinedMaxSizeAttachments,
                AllowedFileExtensions = options.AllowedFileExtensions,
                UseStoredProceduresForReports = options.UseStoredProceduresForReports,
                HourToRunDailyJobs = options.HourToRunDailyJobs,
                HourToRunFrameworkAgreementValidation = options.HourToRunFrameworkAgreementValidation,
                PublicOrigin = options.PublicOrigin,
                PeppolDisplay = options.Peppol.Description,
                FlexibleOrderDisplay = options.FlexibleOrder.Description,
                EnvironmentDisplay = options.Env.Description,
                TellusDisplay = options.Tellus.Description,
                SupportDisplay = options.Support.Description,
                SmtpDisplay = options.Smtp.Description, 
                StatusCheckerDisplay = options.StatusChecker.Description,
                ExcludedNotificationTypesForCustomer = options.ExcludedNotificationTypesForCustomer,
                ExternalLinksDisplay = options.ExternalLinks.Description,
                EnableCustomerApi = options.EnableCustomerApi,
                EnableSetLatestAnswerTimeForCustomer = options.EnableSetLatestAnswerTimeForCustomer,
                AllowDeclineExtraInterpreterOnRequestGroups = options.AllowDeclineExtraInterpreterOnRequestGroups,
                RoundPriceDecimals = options.RoundPriceDecimals,
                MonthsToApproveComplaints = options.MonthsToApproveComplaints,                
            };
        }
        #endregion
    }
}
