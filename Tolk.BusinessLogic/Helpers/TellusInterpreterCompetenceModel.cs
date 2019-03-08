using System;

namespace Tolk.BusinessLogic.Helpers
{
    public class TellusInterpreterCompetenceModel
    {
        public string Language { get; set; }
        public string CompetenceLevel { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }

        public bool IsValidAt(DateTimeOffset startAt)
        {
            return ValidFrom.GetValueOrDefault(DateTime.MinValue) < startAt.Date && ValidTo.GetValueOrDefault(DateTime.MinValue) > startAt.Date;
        }
    }
}
