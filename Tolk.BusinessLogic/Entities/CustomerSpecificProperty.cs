using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{    
    public class CustomerSpecificProperty
    {
        [Required]
        public string DisplayName { get; set; }
        [Required]
        public string DisplayDescription { get; set; }
        [Required]
        public string InputPlaceholder { get; set; }
        public bool Required { get; set; }
        public bool RemoteValidation { get; set; }
        public string RegexPattern { get; set; }
        public string RegexErrorMessage { get; set; }
        public int? MaxLength { get; set; }
        public int CustomerOrganisationId { get; set; }
        public PropertyType PropertyType { get; set; }
        public bool Enabled { get; set; }
        #region foreign keys

        [ForeignKey(nameof(CustomerOrganisationId))]
        public CustomerOrganisation CustomerOrganisation { get; set; }
        #endregion

        public CustomerSpecificProperty()
        {                
        }

        private CustomerSpecificProperty(CustomerSpecificProperty toCopy)
        {
            DisplayName = toCopy.DisplayName;
            DisplayDescription = toCopy.DisplayDescription;
            InputPlaceholder = toCopy.InputPlaceholder;
            Required = toCopy.Required;
            RemoteValidation = toCopy.RemoteValidation;
            RegexPattern = toCopy.RegexPattern;
            RegexErrorMessage = toCopy.RegexErrorMessage;
            MaxLength = toCopy.MaxLength;
            CustomerOrganisationId = toCopy.CustomerOrganisationId;
            PropertyType = toCopy.PropertyType;
            Enabled = toCopy.Enabled;
        }

        public static CustomerSpecificProperty CopyCustomerSpecificProperty(CustomerSpecificProperty toCopy)
        {
            return new CustomerSpecificProperty(toCopy);
        }
    }
}
