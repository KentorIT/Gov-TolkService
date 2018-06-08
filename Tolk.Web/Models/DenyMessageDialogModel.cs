using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class DenyMessageDialogModel
    {
        public string DialogHeader{ get; set; }

        public string Action { get; set; }

        public int ParentId { get; set; }

        [Display(Name = "Meddelande")]
        [DataType(DataType.MultilineText)]
        [Required]
        public string Message { get; set; }
    }
}
