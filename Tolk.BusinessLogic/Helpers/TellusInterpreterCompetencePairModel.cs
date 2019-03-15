using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Helpers
{
    public class TellusInterpreterCompetencePairModel
    {
        public string Id { get; set; }
        public string Value { get; set; }

        public TellusInterpreterCompetencePairModel(string id, string value)
        {
            Id = id;
            Value = value;
        }
    }
}
