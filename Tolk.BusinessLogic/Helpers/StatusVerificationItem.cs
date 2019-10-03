﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;

namespace Tolk.BusinessLogic.Helpers
{
    public class StatusVerificationItem
    {
        public bool Success { get; set; }
        public string Test { get; set; }
    }
}
