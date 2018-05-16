using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tolk.BusinessLogic.Entities
{
    public class InterpreterBrokerRegion
    {
        public int BrokerId { get; set; }

        public int RegionId { get; set; }

        [ForeignKey(nameof(BrokerId) + ", " + nameof(RegionId))]
        public BrokerRegion BrokerRegion { get; set; }

        public int InterpreterId { get; set; }

        [ForeignKey(nameof(InterpreterId))]
        public Interpreter Interpreter { get; set; }
    }
}
