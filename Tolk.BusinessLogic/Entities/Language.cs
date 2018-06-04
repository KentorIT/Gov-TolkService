using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tolk.BusinessLogic.Entities
{
    public class Language
    {
        public int LanguageId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
    }
}
