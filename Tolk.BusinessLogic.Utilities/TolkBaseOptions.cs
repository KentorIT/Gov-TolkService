using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Utilities
{
    public class TolkBaseOptions
    {
        public TellusApi Tellus { get; set; }

        public class TellusApi
        {
            public bool IsActivated { get; set; }
            public string Uri { get; set; }
        }
    }
}
