using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Helpers;
using Tolk.BusinessLogic.Services;
using Tolk.Web.Models;

namespace Tolk.Web.Services
{
    public class VerificationService
    {
        private readonly TolkDbContext _dbContext;
        private readonly ISwedishClock _clock;
        private readonly TolkOptions _tolkOptions;

        private string TellusApiUrl { get => _tolkOptions.TellusSettings.BaseAddress + _tolkOptions.TellusSettings.RouteApi; }

        public VerificationService(TolkDbContext dbContext, ISwedishClock clock, TolkOptions tolkOptions)
        {
            _dbContext = dbContext;
            _clock = clock;
            _tolkOptions = tolkOptions;
        }

        public VerificationResult VerifyInterpreter(string interpreterId, int languageId, CompetenceAndSpecialistLevel competenceLevel)
        {
            var language = _dbContext.Languages.Single(l => l.LanguageId == languageId);
            var url = TellusApiUrl + _tolkOptions.TellusSettings.RouteGet;

            if (language.TellusName == null)
            {
                return VerificationResult.LanguageNotRegistered;
            }

            return VerificationResult.UnknownError;
        }
    }
}
