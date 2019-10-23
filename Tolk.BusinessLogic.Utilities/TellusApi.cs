using System.Collections.Generic;

namespace Tolk.BusinessLogic.Utilities
{
    public class TellusApi
    {
        public bool IsActivated { get; set; }
        public bool IsLanguagesCompetenceActivated { get; set; }
        public string Uri { get; set; }
        public string LanguagesUri { get; set; }
        public string LanguagesCompetenceInfoUri { get; set; }
        public string UnusedIsoCodes { get; set; }
        public IEnumerable<string> UnusedIsoCodesList
        {
            get
            {
                return UnusedIsoCodes.Split(';');
            }
        }
    }
}
