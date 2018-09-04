using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Helpers
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NoDisplayNameAttribute : Attribute
    {
    }
}
