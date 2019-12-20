using System;

namespace Tolk.Web.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PlaceholderAttribute : Attribute
    {
        public string Text { get; set; }

        public PlaceholderAttribute() { }

        public PlaceholderAttribute(string text)
        {
            Text = text;
        }
    }
}
