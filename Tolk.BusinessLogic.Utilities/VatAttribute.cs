using System;

namespace Tolk.BusinessLogic.Utilities
{
    /// <summary>
    /// Used to set vat on invoiceable articles.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class VatAttribute : Attribute
    {
        /// <summary>
        /// ctor
        /// </summary>
        public VatAttribute(double vat)
        {
            Vat = vat;
        }

        /// <summary>
        /// The parent
        /// </summary>
        public double Vat { get; private set; }
    }
}
