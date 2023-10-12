using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Models.CustomerSpecificProperties
{
    public class CustomerSpecificPropertyModel
    {
        [Required]
        public int CustomerOrganisationId { get; set; }
        [Required(ErrorMessage = "Nytt fältnamn måste anges")]
        [Display(Name = "Nytt fältnamn")]
        public string DisplayName { get; set; }
        [Display(Name = "Beskrivning")]
        public string DisplayDescription { get; set; }
        [Display(Name = "Placeholder")]
        public string Placeholder { get; set; }
        [Display(Name = "Krav")]
        public bool Required { get; set; }
        [Display(Name = "Servervalidering")]
        public bool RemoteValidation { get; set; }        
        [Required(ErrorMessage = "Regexmönster måste anges")]
        [Display(Name = "Regexmönster")]
        public string RegexPattern { get; set; }
        [Display(Name = "Felmeddelande")]
        public string RegexErrorMessage { get; set; }
        [Display(Name = "Maxlängd")]
        public int? MaxLength { get; set; }
        public string Value { get; set; }
        [Required]
        [Display(Name = "Originalfält")]
        public PropertyType PropertyToReplace { get; set; }
        [Display(Name = "Aktivt")]
        public bool Enabled { get; set; }        
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
            Enabled = propEntity.Enabled;
        }
        public CustomerSpecificPropertyModel()
        {

        }
    }
}
