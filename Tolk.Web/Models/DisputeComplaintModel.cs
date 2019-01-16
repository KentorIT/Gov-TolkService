﻿using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class DisputeComplaintModel
    {
        public int ComplaintId { get; set; }

        [StringLength(1000)]
        public string DisputeMessage { get; set; }
    }
}
