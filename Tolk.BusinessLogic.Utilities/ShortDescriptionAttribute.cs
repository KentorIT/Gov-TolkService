using System;

namespace Tolk.BusinessLogic.Utilities
{

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ShortDescriptionAttribute : Attribute
    {
        public ShortDescriptionAttribute(string shortDescription)
        {
            ShortDescription = shortDescription;
        }

        /// <summary>
        /// Short description
        /// </summary>
        public string ShortDescription { get; private set; }

    }
}
