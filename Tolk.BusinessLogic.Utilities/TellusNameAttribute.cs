using System;

namespace Tolk.BusinessLogic.Utilities
{
    public class TellusNameAttribute : Attribute
    {
        public string Id { get; set; }
        public string Value { get; set; }

        public TellusNameAttribute(string id, string value)
        {
            Id = id;
            Value = value;
        }
    }
}
