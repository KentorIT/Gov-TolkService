using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;


namespace Tolk.Web.Models
{
    public class SystemMessageModel
    {
        public int SystemMessageId { get; set; }

        [Display(Name = "Nyhetstyp", Description = "Välj vilken typ av nyhet det är fråga om. Varningar visas i en gulaktig ton och infomration visas i en blåaktig ton. Varningar listas överst för användarna.")]
        [Required]
        public RadioButtonGroup SystemMessageType { get; set; }

        public SystemMessageType SystemMessageTypeValue { get; set; } = BusinessLogic.Enums.SystemMessageType.Information;

        [Required]
        [StringLength(255)]
        [Display(Name = "Nyhetens rubrik")]
        public string SystemMessageHeader { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Nyhetens innehåll", Description = "Den text som du vill ska visas för användarna.")]
        [StringLength(2000)]
        public string SystemMessageText { get; set; }

        [Required(ErrorMessage = "Ange datum")]
        [Display(Name = "Datum för när nyheten ska visas", Description = "Du kan förbereda nyheter som du vill ska visas längre fram i tiden och om du inte vill att nyheten ska visas så kan du sätta tillbaka datumet.")]
        public RequiredDateRange DisplayDate { get; set; }

        [Required]
        [Display(Name = "Visa nyheten för", Description = "Här bestämmer du vilka användare som ska få se nyheten.")]
        public SystemMessageUserTypeGroup DisplayedForUserTypeGroup { get; set; }

    }
}
