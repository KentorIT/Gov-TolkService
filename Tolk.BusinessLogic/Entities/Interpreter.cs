using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Entities
{
    public class Interpreter
    {
        public int InterpreterId { get; set; }

        public List<InterpreterBrokerRegion> BrokerRegions { get; set; }

        public AspNetUser User { get; set; }
    }
}
