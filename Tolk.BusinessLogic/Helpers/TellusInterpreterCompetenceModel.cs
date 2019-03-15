using System;
using System.Collections.Generic;

namespace Tolk.BusinessLogic.Helpers
{
    public class TellusInterpreterCompetenceModel
    {
        /// <summary>
        /// ISO 639-2
        /// </summary>
        public string Language { get; set; }
        public TellusInterpreterCompetencePairModel CompetenceLevel { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }

        public bool IsValidAt(DateTimeOffset startAt)
        {
            return ValidFrom.GetValueOrDefault(DateTime.MinValue) < startAt.Date && ValidTo.GetValueOrDefault(DateTime.MinValue) > startAt.Date;
        }
    }
}
