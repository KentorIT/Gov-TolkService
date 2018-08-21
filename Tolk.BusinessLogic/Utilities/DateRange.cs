using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Utilities
{
    public class DateRange
    {
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }

        public bool HasValue
        {
            get
            {
                // Return false if both are null
                return !(Start == null && End == null);
            }
        }

        public DateRange()
        {

        }

        public bool IsInRange(DateTimeOffset date)
        {
            if (!IsValid())
            {
                return false;
            }

            // Check if date is in range
            if (Start != null && End != null)
            {
                return Start <= date && date <= End;
            }

            // Check if date is after start
            if (Start != null)
            {
                return Start <= date;
            }

            // Check if date is before end
            if (End != null)
            {
                return date <= End;
            }

            return false;
        }

        public bool IsValid()
        {
            // If both start and end are set, start must be before end
            // If only one of them is set, it is valid
            return (Start != null && End != null) ? Start <= End : true;
        }
    }
}
