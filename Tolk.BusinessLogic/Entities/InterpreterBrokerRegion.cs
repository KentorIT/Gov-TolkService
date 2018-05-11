using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tolk.BusinessLogic.Entities
{
    public class InterpreterBrokerRegion
    {
        public int BrokerRegionId { get; set; }

        public BrokerRegion BrokerRegion { get; set; }

        [Required]
        public string InterpreterId { get; set; }

        public AspNetUser Interpreter { get; set; }

    }
}
