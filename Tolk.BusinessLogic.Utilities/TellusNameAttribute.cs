using System;

namespace Tolk.BusinessLogic.Utilities
{
    [AttributeUsage(AttributeTargets.Field)]
    public class TellusNameAttribute : Attribute
    {
        public string Name { get; set; }

        public TellusNameAttribute(string name)
        {
            Name = name;
        }
    }
}
