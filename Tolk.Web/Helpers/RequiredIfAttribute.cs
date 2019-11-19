using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
    public class RequiredIfAttribute : ValidationAttribute, IClientModelValidator
    {
        public string OtherProperty { get; set; }

        public Condition CheckIf { get; set; } = Condition.ValueEquals;

        public object Value { get; set; }

        public Type OtherPropertyType { get; set; } = typeof(string);

        public bool AlwaysDisplayRequiredStar { get; set; } = false;

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
            return ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-requiredif", $"{context.ModelMetadata.DisplayName} måste anges");
            MergeAttribute(context.Attributes, "data-val-requiredif-otherproperty", OtherProperty);
            MergeAttribute(context.Attributes, "data-val-requiredif-otherpropertytype", CheckIf == Condition.ValueEquals ? OtherPropertyType.ToString() : "notnull");
            MergeAttribute(context.Attributes, "data-val-requiredif-otherpropertyvalue", Value.ToString());
        }

        private bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (attributes.ContainsKey(key))
            {
                return false;
            }

            attributes.Add(key, value);
            return true;
        }
    }
}
