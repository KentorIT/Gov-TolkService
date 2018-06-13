using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Tolk.BusinessLogic.Entities
{
    public class InterpreterBroker
    {
        public int BrokerId { get; set; }

        [ForeignKey(nameof(BrokerId))]
        public Broker Broker { get; set; }

        public int InterpreterId { get; set; }

        [ForeignKey(nameof(InterpreterId))]
        public Interpreter Interpreter { get; set; }
    }
}
