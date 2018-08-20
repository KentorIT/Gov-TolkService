using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Utilities
{
    public class DateRange
    {
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }

        public DateRange()
        {

        }

        public bool IsInRange(DateTimeOffset date)
        {
            return IsInRange(date, false);
        }

        public bool IsInRange(DateTimeOffset date, bool skipValidation)
        {
            if (!skipValidation && !IsValid())
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
            // Not a valid range if neither start nor end is set
            if (Start == null && End == null)
            {
                return false;
            }
            // If both start and end are set, start must be before end
            // If only one of them is set, it is valid
            return (Start != null && End != null) ? Start <= End : true;
        }
    }
}
