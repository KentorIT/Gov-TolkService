using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Attributes
{
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
