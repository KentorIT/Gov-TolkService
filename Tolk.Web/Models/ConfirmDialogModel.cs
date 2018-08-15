﻿using Microsoft.AspNetCore.Mvc.Rendering;
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
    public class ConfirmDialogModel
    {
        public string DialogHeader{ get; set; }

        public string Action { get; set; }

        public string BtnTextConfirm { get; set; }

        public string BtnTextDeny { get; set; }

        [Display(Name = "Meddelande")]
        public string Message { get; set; }

        public ConfirmDialogModel()
        {
            // Default values
            BtnTextConfirm = "Ja";
            BtnTextDeny = "Nej";
        }
    }
}
