using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Models.CustomerSpecificProperties
{
    public class CustomerSpecificPropertyModel
    {
        public int CustomerOrganisationId { get; set; }
        public string DisplayName { get; set; }
        public string DisplayDescription { get; set; }
        public string Placeholder { get; set; }
        public bool Required { get; set; }
        public bool RemoteValidation { get; set; }
        public string RegexPattern { get; set; }
        public string RegexErrorMessage { get; set; }
        public int? MaxLength { get; set; }
        public string Value { get; set; }
        public PropertyType PropertyToReplace { get; set; }
        public CustomerSpecificPropertyModel(CustomerSpecificProperty propEntity)
        {
            CustomerOrganisationId = propEntity.CustomerOrganisationId;
            DisplayName = propEntity.DisplayName;
            DisplayDescription = propEntity.DisplayDescription;
            Placeholder = propEntity.InputPlaceholder;
            Required = propEntity.Required;
            RemoteValidation = propEntity.RemoteValidation;
            RegexPattern = propEntity.RegexPattern;
            RegexErrorMessage = propEntity.RegexErrorMessage;
            MaxLength = propEntity.MaxLength;
            PropertyToReplace = propEntity.PropertyType;
        }
        public CustomerSpecificPropertyModel()
        {

        }
    }
}
