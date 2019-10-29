using System;
using System.Collections.Generic;

namespace Tolk.BusinessLogic.Utilities
{
    public class TellusApi
    {
        public bool IsActivated { get; set; }
        public bool IsLanguagesCompetenceActivated { get; set; }
        public Uri Uri { get; set; }
        public Uri LanguagesUri { get; set; }
        public Uri LanguagesCompetenceInfoUri { get; set; }
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
