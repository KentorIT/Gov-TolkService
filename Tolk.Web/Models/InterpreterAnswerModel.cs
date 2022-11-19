using System.ComponentModel.DataAnnotations;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class InterpreterAnswerModel : InterpreterAcceptModel, IModel
    {
        [Required]
        [Display(Name = "Tolk", Description = "I de fall tillsatt tolk har skyddad identitet skall inte tolkens namn eller kontaktuppgifter finnas i bekräftelsen. Använd i dessa fall valet ”Tolk med skyddade personuppgifter”. Överlämna tolkens uppgifter på annat sätt i enlighet med era säkerhetsrutiner")]
        public int? InterpreterId { get; set; }

        [ClientRequired]
        [EmailAddress]
        [RegularExpression(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*@((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$", ErrorMessage = "Felaktig e-postadress")]
        [Display(Name = "Tolkens e-postadress")]
        [StringLength(255)]
        public string NewInterpreterEmail { get; set; }

        [ClientRequired]
        [Display(Name = "Tolkens förnamn")]
        [StringLength(255)]
        public string NewInterpreterFirstName { get; set; }

        [ClientRequired]
        [Display(Name = "Tolkens efternamn")]
        [StringLength(255)]
        public string NewInterpreterLastName { get; set; }

        [Display(Name = "Kammarkollegiets tolknummer")]
        public string NewInterpreterOfficialInterpreterId { get; set; }

        [ClientRequired]
        [Display(Name = "Tolkens telefonnummer")]
        [StringLength(255)]
        public string NewInterpreterPhoneNumber { get; set; }

        [Range(0, 999999, ErrorMessage = "Kontrollera värdet för resekostnad")]
        [RegularExpression(@"^[^.]*$", ErrorMessage = "Värdet får inte innehålla punkttecken, ersätt med kommatecken")] // validate.js regex allows dots, despite not explicitly allowing them
        [ClientRequired(ErrorMessage = "Ange resekostnad (endast siffror, ange 0 om det inte finns någon kostnad)")]
        [Display(Name = "Bedömd resekostnad")]
        [DataType(DataType.Currency)]
        [Placeholder("Ange i SEK")]
        public decimal? ExpectedTravelCosts { get; set; }

        [Display(Name = "Kommentar till bedömd resekostnad", Description = "Här kan du kommentera den bedömda resekostnaden som angivits genom att skriva in t ex antal km för bilersättning, eventuella biljettkostnader m.m.")]
        [Placeholder("Ange t ex bedömt antal km, biljettkostnad m.m.")]
        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        public string ExpectedTravelCostInfo { get; set; }

        [Display(Name = "Kommentar till tacka nej", Description = "Beskriv varför det inte är möjligt att tillsätta den extra tolken")]
        [Placeholder("Ange en anledning till varför ni inte kan tillsätta denna tolk")]
        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        [ClientRequired]
        public string DeclineMessage { get; set; }
    }
}
