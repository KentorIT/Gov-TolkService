using System.Collections.Generic;
using System.Linq;

namespace Tolk.BusinessLogic.Utilities
{
    public class UserInactivitySettings
    {
        public bool EnableAutomaticDeactivation { get; set; }
        public int InactivationThresholdMonths { get; set; } = 12;
        private List<int> _notifyDaysBeforeDeactivation;
        public List<int> NotifyDaysBeforeDeactivation {
            get => _notifyDaysBeforeDeactivation;
            set => _notifyDaysBeforeDeactivation = value.OrderByDescending(n => n).ToList();
        } 

        public string Description => $"Antal månader innan inaktivering: {InactivationThresholdMonths}\nDagar innan inaktivering som påminnelse skickas ut: {string.Join(", ",NotifyDaysBeforeDeactivation)}\n";
    }
}
