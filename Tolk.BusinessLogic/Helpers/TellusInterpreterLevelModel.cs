using System;

namespace Tolk.BusinessLogic.Helpers
{
    public class TellusInterpreterLevelModel
    {
        public string Langugage
        {
            //Override for a missed spelling in tellus api.
            get => Language;
            set { Language = value; }
        }
        public string Language { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }

        public bool IsValidAt(DateTimeOffset startAt)
        {
            return ValidFrom.GetValueOrDefault(DateTime.MinValue) < startAt.Date && ValidTo.GetValueOrDefault(DateTime.MinValue) > startAt.Date;
        }
    }
}
