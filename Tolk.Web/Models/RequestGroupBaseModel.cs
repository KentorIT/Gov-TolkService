using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;


namespace Tolk.Web.Models
{
    public class RequestGroupBaseModel : IModel
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

        [Display(Name = "Orsak till avbokning")]
        [DataType(DataType.MultilineText)]
        public string CancelMessage { get; set; }

        [Display(Name = "Tillsätt tolk senast")]
        public DateTimeOffset? ExpiresAt { get; set; }

        [Display(Name = "Bekräfta senast")]
        public DateTimeOffset? LastAcceptAt { get; set; }

        public string ColorClassName => CssClassHelper.GetColorClassNameForRequestStatus(Status);

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


        [Display(Name = "Övrig information om uppdraget")]
        public string Description { get; set; }

        public string LanguageName { get; set; }

        public string Dialect { get; set; }

        public bool SpecificCompetenceLevelRequired { get; set; }

        public bool LanguageHasAuthorizedInterpreter { get; set; }

        public bool HasExtraInterpreter { get; set; }

        public bool OrderHasAllowExceedingTravelCost { get; set; }

        [Display(Name = "Accepterar restid eller resväg som överskrider gränsvärden")]
        public RadioButtonGroup AllowExceedingTravelCost { get; set; }

        [Display(Name = "Är tolkanvändare samma person som bokar")]
        public RadioButtonGroup CreatorIsInterpreterUser { get; set; }

        public CustomerInformationModel CustomerInformationModel { get; set; }


        [Display(Name = "Förmedlingens bokningsnummer", Description = "Här kan ni som förmedling ange ett eller flera egna bokningsnummer att koppla till den sammanhållna bokningen.")]
        public string BrokerReferenceNumber { get; set; }

        public OccasionListModel OccasionList { get; set; }

        [Display(Name = "Vill du ange en sista tid för att besvara tillsättning för myndighet", Description = "Ange om du vill sätta en tid för när myndigheten senast ska besvara tillsättningen. Om du anger en tid och myndigheten inte svarar inom angiven tid avslutas förfrågan.")]
        [ClientRequired]
        public RadioButtonGroup SetLatestAnswerTimeForCustomer { get; set; }

        [Display(Name = "Sista tid att besvara tillsättning", Description = "Här har förmedlingen möjlighet att ange en tid för när myndigheten senast ska besvara tillsättningen. Om myndigheten inte svarar inom angiven tid avslutas förfrågan.")]
        [ClientRequired]
        public DateTimeOffset? LatestAnswerTimeForCustomer { get; set; }

        public string TravelConditionHours { get; set; }
        public string TravelConditionKilometers { get; set; }

    }
}
