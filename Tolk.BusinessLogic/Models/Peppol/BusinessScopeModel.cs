using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Tolk.BusinessLogic.Models.Peppol
{
    [Serializable]
    public class BusinessScopeModel
    {
        [XmlElement(ElementName = "Scope")]
        public List<ScopeModel> Scopes { get; set; }
    }
}