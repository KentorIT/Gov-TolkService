using System;

namespace Tolk.Web.Helpers
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class NoDisplayNameAttribute : Attribute
    {
    }
}
