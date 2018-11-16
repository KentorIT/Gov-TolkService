using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Tolk.Web.Helpers.RequiredIf;

namespace Tolk.Web.Helpers.RequiredIf
{
    public enum Condition
    {
        ValueEquals,
        PropertyIsNotNull
    }
}

namespace Tolk.Web.Helpers
{
    public class RequiredIfAttribute : ValidationAttribute
    {
        public string OtherProperty { get; set; }

        public Condition CheckIf { get; set; } = Condition.ValueEquals;

        public object Value { get; set; }

        public new string ErrorMessageString { get; set; } = null;

        public RequiredIfAttribute() { }

        public RequiredIfAttribute(string OtherProperty, object Value)
        {
            this.OtherProperty = OtherProperty;
            this.Value = Value;
        }

        public RequiredIfAttribute(string OtherProperty, Condition CheckIf)
        {
            this.OtherProperty = OtherProperty;
            this.CheckIf = CheckIf;
        }

        protected override ValidationResult IsValid(object item, ValidationContext validationContext)
        {
            PropertyInfo otherProperty = validationContext.ObjectInstance.GetType().GetProperty(OtherProperty);
            object otherPropertyValue = otherProperty.GetValue(validationContext.ObjectInstance, null);

            if (otherPropertyValue != null)
            {
                if ((CheckIf == Condition.ValueEquals && Value != null && otherPropertyValue != null && otherPropertyValue == Value && item == null)
                    || (CheckIf == Condition.PropertyIsNotNull && item == null))
                {
                    return new ValidationResult(ErrorMessageString ?? $"{validationContext.DisplayName ?? validationContext.MemberName} måste anges");
                }
            }

            return ValidationResult.Success;
        }
    }
}
