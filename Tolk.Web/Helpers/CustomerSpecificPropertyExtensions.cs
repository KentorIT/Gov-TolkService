using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Models.CustomerSpecificProperties;

namespace Tolk.Web.Helpers
{
    public static class CustomerSpecificPropertyExtensions
    {
        public static CustomerSpecificProperty ToEntity(this CustomerSpecificPropertyModel propertyModel)
        {
            var propertyEntity = new CustomerSpecificProperty();
            propertyEntity.CustomerOrganisationId = propertyModel.CustomerOrganisationId;
            propertyEntity.PropertyType = propertyModel.PropertyToReplace;
            propertyEntity.RemoteValidation = propertyModel.RemoteValidation;
            propertyEntity.InputPlaceholder = propertyModel.Placeholder;
            propertyEntity.Required = propertyModel.Required;
            propertyEntity.MaxLength = propertyModel.MaxLength;
            propertyEntity.RegexPattern = propertyModel.RegexPattern;
            propertyEntity.RegexErrorMessage = propertyModel.RegexErrorMessage;
            propertyEntity.DisplayName = propertyModel.DisplayName;
            propertyEntity.DisplayDescription = propertyModel.DisplayDescription;

            return propertyEntity;
        }
    }
}
