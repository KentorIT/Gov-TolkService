using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Services
{
    public interface ISwedishClock
    {
        DateTimeOffset SwedenNow { get; }
    }
}
