using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

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
    }
}
