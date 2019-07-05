using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Tolk.BusinessLogic.Entities
{
    public class Language
    {
        [Required]
        public int LanguageId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(3)]
        public string ISO_639_Code { get; set; }

        [MaxLength(100)]
        public string TellusName { get; set; }

        [Required]
        public bool Active { get; set; }

        public bool HasLegal { get; set; }

        public bool HasHealthcare { get; set; }

        public bool HasAuthorized { get; set; }

        public bool HasEducated { get; set; }

        public string Competences
        {
            get
            {
                StringBuilder sb = new StringBuilder(HasLegal ? "L" : string.Empty);
                sb.Append(HasHealthcare ? "H" : string.Empty);
                sb.Append(HasAuthorized ? "A" : string.Empty);
                sb.Append(HasEducated ? "E" : string.Empty);
                return sb.ToString().Length == 0 ? "0" : sb.ToString();
            }
        }
        public bool HasAllCompetences => HasLegal && HasHealthcare && HasAuthorized && HasEducated;


    }
}
