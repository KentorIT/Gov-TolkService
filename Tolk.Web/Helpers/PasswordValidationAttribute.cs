using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

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
            string notValidLength = string.Empty;
            string Password = value == null ? String.Empty : value.ToString();

            if (string.IsNullOrEmpty(Password) || Password.Length < MinimumPasswordLength)
            {
                notValidLength = $"Lösenord ska vara minst { MinimumPasswordLength} tecken långt.";
                errors++;
            }
            if (MustContainLower)
            {
                Regex reg = new Regex("[a-z]");
                if (!reg.IsMatch(Password))
                {
                    missingChars.Add("liten bokstav");
                    errors++;
                }
            }
            if (MustContainUpper)
            {
                Regex reg = new Regex("[A-Z]");
                if (!reg.IsMatch(Password))
                {
                    missingChars.Add("stor bokstav");
                    errors++;
                }
            }
            if (MustContainNumbers)
            {
                Regex reg = new Regex("[0-9]");
                if (!reg.IsMatch(Password))
                {
                    missingChars.Add("siffra");
                    errors++;
                }
            }
            if (MustContainNonAlphanumeric)
            {
                Regex reg = new Regex("[^a-zA-Z\\d\\s]");
                if (!reg.IsMatch(Password))
                {
                    missingChars.Add("symbol/specialtecken");
                    errors++;
                }
            }
            if (errors == 0)
            {
                return ValidationResult.Success;
            }
            else
            {
                if (!string.IsNullOrEmpty(notValidLength))
                {
                    return new ValidationResult(notValidLength + (missingChars.Any() ? " Lösenordet saknade också " + string.Join(", ", missingChars) + "." : string.Empty ) + " Regler för lösenord finns under i-symbolen.");
                }
                else
                {
                    return new ValidationResult("Lösenordet saknade " + string.Join(", ", missingChars) + ". Regler för lösenord finns under i-symbolen.");
                }
            }
        }
    }
}
