using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class OrderOccasionModel
    {
        public int? OrderOccasionId { get; set; }

        public DateTime OccasionStartDateTime { get; set; }

        public DateTime OccasionEndDateTime { get; set; }

        public bool ExtraInterpreter { get; set; }
    }
}
