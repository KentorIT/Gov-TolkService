using DocumentFormat.OpenXml.Drawing;
using Microsoft.Extensions.Options;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Services
{
    public class TolkBaseOptionsService : ITolkBaseOptions

    {
        private readonly TolkOptions _options;

        public TolkBaseOptionsService(IOptions<TolkOptions> options)
        {
            _options = options?.Value;
        }

        public Environment Env => _options.Env;

        public System.Uri TolkWebBaseUrl => _options.PublicOrigin.AsUri();

        public int MonthsToApproveComplaints => _options.MonthsToApproveComplaints;

        public TellusApi Tellus => _options.Tellus;

        public SupportSettings Support => _options.Support;

        public SmtpSettings Smtp => _options.Smtp;

        public StatusCheckerSettings StatusChecker => _options.StatusChecker;

        public bool RunEntityScheduler => _options?.RunEntityScheduler ?? true;

        public bool AllowDeclineExtraInterpreterOnRequestGroups => _options.AllowDeclineExtraInterpreterOnRequestGroups;

        public bool RoundPriceDecimals => _options.RoundPriceDecimals;

        public bool EnableSetLatestAnswerTimeForCustomer => _options.EnableSetLatestAnswerTimeForCustomer;

        public bool EnableCustomerApi => _options.EnableCustomerApi;

        public int WorkDaysGracePeriodBeforeOrderAgreementCreation => _options.WorkDaysGracePeriodBeforeOrderAgreementCreation;

        public string ExcludedNotificationTypesForCustomer => _options.ExcludedNotificationTypesForCustomer;

        //Only use this when the context needs to be separated from the session based context.
        public TolkDbContext GetContext() => _options.GetContext();
    }
}
