using System;

namespace Tolk.Web.Helpers
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PrefixAttribute : Attribute
    {
        public enum Position
        {
            Label,
            Value
        }

        public Position PrefixPosition { get; set; }

        public string Text { get; set; }
    }
}
