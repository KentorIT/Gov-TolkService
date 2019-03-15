using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Helpers
{
    public class TellusInterpreterEducationModel
    {
        /// <summary>
        /// ISO 639-2
        /// </summary>
        public string Language { get; set; }
        /// <summary>
        /// ISO 639-2
        /// </summary>
        public string FromLanguage { get; set; }
        /// <summary>
        /// ISO 639-2
        /// </summary>
        public string ToLanguage { get; set; }
        public string EducationLevel { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }
}
