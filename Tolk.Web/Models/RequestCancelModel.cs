﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class RequestCancelModel
    {

        public int RequestId { get; set; }

        [DataType(DataType.MultilineText)]
        [Required]
        public string CancelMessage { get; set; }
    }
}
