using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;


namespace Tolk.Web.Models
{
    public class AssignmentFilterModel
    {
        [Display(Name = "Uppdragets status")]
        public AssignmentStatus? Status { get; set; }
    }
}
