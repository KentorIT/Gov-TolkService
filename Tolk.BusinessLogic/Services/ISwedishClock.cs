using System;

namespace Tolk.BusinessLogic.Services
{
    public interface ISwedishClock
    {
        DateTimeOffset SwedenNow { get; }
    }
}
