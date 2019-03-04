using System;

namespace Tolk.BusinessLogic.Helpers
{
    public class TellusNameAttribute : Attribute
    {
        public string Name { get; set; }

        public TellusNameAttribute(string name)
        {
            Name = name;
        }
    }
}
