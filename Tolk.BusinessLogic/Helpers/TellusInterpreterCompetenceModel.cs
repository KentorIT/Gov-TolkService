using System;

namespace Tolk.BusinessLogic.Helpers
{
    public class TellusInterpreterCompetenceModel
    {
        public string language { get; set; }
        public string competenceLevel { get; set; }
        public DateTime? validFrom { get; set; }
        public DateTime? validTo { get; set; }

        public bool IsValidAt(DateTimeOffset startAt)
        {
            return validFrom.GetValueOrDefault(DateTime.MinValue) < startAt.Date && validTo.GetValueOrDefault(DateTime.MinValue) > startAt.Date;
        }
    }
}
