using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderInterpreterLocation
    {
        public InterpreterLocation InterpreterLocation { get; set; }

        public int Rank { get; set; }

        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        [MaxLength(100)]
        public string Street { get; set; }

        [MaxLength(100)]
        public string ZipCode { get; set; }

        [MaxLength(100)]
        public string City { get; set; }

        public OffSiteAssignmentType? OffSiteAssignmentType { get; set; }

        [MaxLength(255)]
        public string OffSiteContactInformation { get; set; }


    }
}
