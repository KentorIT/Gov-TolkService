namespace Tolk.BusinessLogic.Utilities
{
    /// <summary>
    /// Value-description pair an enum value.
    /// </summary>
    /// <typeparam name="TEnum">Type of the enum.</typeparam>
    public class EnumValue<TEnum>
    {
        /// <summary>
        /// Enum value.
        /// </summary>
        public TEnum Value { get; private set; }

        /// <summary>
        /// Description of the enum value.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="value">Value to store.</param>
        /// <param name="description">Description to store.</param>
        public EnumValue(TEnum value, string description)
        {
            Value = value;
            Description = description;
        }
    }

}
