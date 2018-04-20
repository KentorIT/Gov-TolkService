using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Models
{
    public class RequestViewModel
    {
        [Display(Name = "Plats")]
        public string LocationName { get; set; }

        [Display(Name = "Adress")]
        public string LocationAddress { get; set; }

        [Display(Name = "Ort")]
        public string LocationCity { get; set; }
    }
}
