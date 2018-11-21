using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Helpers
{
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
