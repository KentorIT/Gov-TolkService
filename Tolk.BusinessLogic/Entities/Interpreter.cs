using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Entities
{
    public class Interpreter
    {
        public int InterpreterId { get; set; }

        public List<InterpreterBroker> Brokers { get; set; } = new List<InterpreterBroker>();

        public AspNetUser User { get; set; }
    }
}
