namespace Tolk.BusinessLogic.Utilities
{
    /// <summary>
    /// Value-description pair an enum value.
    /// </summary>
    /// <typeparam name="TEnum">Type of the enum.</typeparam>
    public class EnumDescription<TEnum> : EnumValue<TEnum>
    {
        public string CustomName { get; private set; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="value">Value to store.</param>
        /// <param name="description">Description to store.</param>
        public EnumDescription(TEnum value, string description, string customName)
            : base(value, description)
        {
            CustomName = customName;
        }
    }

}
