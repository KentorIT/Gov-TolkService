using System;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class TellusCompetenceModel
    {
        public Language Language { get; set; }

        public CompetenceAndSpecialistLevel CompetenceLevel { get; set; }

        public DateTimeOffset? ValidFrom { get; set; }

        public DateTimeOffset? ValidTo { get; set; }
    }
}
