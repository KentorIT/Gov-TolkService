using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;


namespace Tolk.Web.Models
{
    public class RequestGroupBaseModel
    {

        public int RequestGroupId { get; set; }

        [Display(Name = "Status på förfrågan")]
        public RequestStatus Status { get; set; }

        public int BrokerId { get; set; }

        [Display(Name = "Sammanhållet BokningsID")]
        public string OrderGroupNumber { get; set; }

        public string ViewedByUser { get; set; } = string.Empty;

        [Display(Name = "Bokning skapad")]
        public DateTimeOffset CreatedAt { get; set; }

        public int OrderGroupId { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<FileModel> Files { get; set; }

        public AttachmentListModel AttachmentListModel { get; set; }

        public Guid? FileGroupKey { get; set; }

        public long? CombinedMaxSizeAttachments { get; set; }

        [Display(Name = "Orsak till avböjande")]
        [DataType(DataType.MultilineText)]
        [ClientRequired]
        [StringLength(1000)]
        [Placeholder("Beskriv anledning tydligt.")]
        public string DenyMessage { get; set; }

        [Display(Name = "Anledning till att bokningen avbokas")]
        [DataType(DataType.MultilineText)]
        public string CancelMessage { get; set; }

        [Display(Name = "Svara senast")]
        public DateTimeOffset ExpiresAt { get; set; }

        public string ColorClassName { get => CssClassHelper.GetColorClassNameForRequestStatus(Status); }

        //INTERPRETER REPLACEMENT
        public InterpreterAnswerModel InterpreterAnswerModel { get; set; }
        public InterpreterAnswerModel ExtraInterpreterAnswerModel { get; set; }

        [ClientRequired]
        [Display(Name = "Inställelsesätt")]
        public InterpreterLocation? InterpreterLocation { get; set; }

        [Display(Name = "Uppdragstyp")]
        public AssignmentType AssignmentType { get; set; }

        [Display(Name = "Språk och dialekt")]
        [DataType(DataType.MultilineText)]
        public string LanguageAndDialect => $"{LanguageName}\n{Dialect}";

        [Display(Name = "Län")]
        public string RegionName { get; set; }

        [Display(Name = "Myndighet")]
        public string CustomerName { get; set; }

        [Display(Name = "Skapad av")]
        [DataType(DataType.MultilineText)]
        public string CreatedBy { get; set; }

        [Display(Name = "Myndighetens enhet")]
        public string CustomerUnitName { get; set; }

        [Display(Name = "Myndighetens avdelning")]
        public string UnitName { get; set; }

        [Display(Name = "Myndighetens organisationsnummer")]
        public string CustomerOrganisationNumber { get; set; }

        [Display(Name = "Myndighetens ärendenummer")]
        public string CustomerReferenceNumber { get; set; }

        [Display(Name = "Övrig information om uppdraget")]
        public string Description { get; set; }

        public string LanguageName { get; set; }

        public string Dialect { get; set; }

        public bool SpecificCompetenceLevelRequired { get; set; }

        public bool LanguageHasAuthorizedInterpreter { get; set; }

        public bool HasExtraInterpreter { get; set; }

        public bool AllowExceedingTravelCost { get; set; }


    }
}
