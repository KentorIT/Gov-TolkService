﻿using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderRequirementBase
    {
        public RequirementType RequirementType { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public bool IsRequired { get; set; }
    }
}
