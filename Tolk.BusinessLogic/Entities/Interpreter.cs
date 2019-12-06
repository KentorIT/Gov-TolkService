using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class Interpreter
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InterpreterId { get; set; }

        public bool IsProtected { get; set; }

        public List<InterpreterBroker> Brokers { get; set; } = new List<InterpreterBroker>();

        public AspNetUser User { get; set; }
    }
}
