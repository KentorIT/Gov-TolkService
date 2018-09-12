using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tolk.Web.Helpers
{
    public class PasswordValidationAttribute : ValidationAttribute
    {
        public int MinimumPasswordLength { get; set; }

        public bool MustContainLower { get; set; }

        public bool MustContainUpper { get; set; }

        public bool MustContainNumbers { get; set; }

        public bool MustContainNonAlphanumeric { get; set; }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            int errors = 0;
            List<string> missingChars = new List<string>();
            string Password = value == null ? String.Empty : value.ToString();

            if (string.IsNullOrEmpty(Password) || Password.Length < MinimumPasswordLength)
            {
                missingChars.Add($"{MinimumPasswordLength} bokstäver");
                errors++;
            }
            else
            {
                if (MustContainLower)
                {
                    Regex reg = new Regex("[a-z]");
                    if (!reg.IsMatch(Password))
                    {
                        missingChars.Add("1 liten bokstav");
                        errors++;
                    }
                }
                if (MustContainUpper)
                {
                    Regex reg = new Regex("[A-Z]");
                    if (!reg.IsMatch(Password))
                    {
                        missingChars.Add("1 stor bokstav");
                        errors++;
                    }
                }
                if (MustContainNumbers)
                {
                    Regex reg = new Regex("[0-9]");
                    if (!reg.IsMatch(Password))
                    {
                        missingChars.Add("1 siffra");
                        errors++;
                    }
                }
                if (MustContainNonAlphanumeric)
                {
                    Regex reg = new Regex("[^a-zA-Z\\d\\s]");
                    if (!reg.IsMatch(Password))
                    {
                        missingChars.Add("1 symbol");
                        errors++;
                    }
                }
            }
            if (errors == 0)
            {
                return ValidationResult.Success;
            }
            else
            {
                return new ValidationResult("Ditt lösenord måste innehålla minst " + string.Join(", ", missingChars));
            }
        }
    }
}
