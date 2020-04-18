using System;
using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class EventLogEntryModel
    {
        // Weight to order events that happen simultaneously
        // Larger weight is ordered after smaller weight
        public int Weight { get; set; } = 100;

        public DateTimeOffset Timestamp { get; set; }

        public string EventDetails { get; set; }

        public string Actor { get; set; }
        public string ActorContactInfo { get; set; }

        public string Organization { get; set; }

        internal class EventLogEntryComparer : IEqualityComparer<EventLogEntryModel>
        {
            //if all properties are same the EventLogEntries are equal
            public bool Equals(EventLogEntryModel x, EventLogEntryModel y)
            {
                //Check whether the compared objects reference the same data
                if (ReferenceEquals(x, y)) return true;

                //Check whether any of the compared objects is null
                if (x is null || y is null)
                    return false;

                //Check whether properties are equal
                return x.Organization == y.Organization && x.Actor == y.Actor && x.ActorContactInfo == y.ActorContactInfo && x.EventDetails == y.EventDetails && x.Weight == y.Weight && x.Timestamp == y.Timestamp;
            }

            // If Equals() returns true for a pair of objects then GetHashCode() must return the same value for these objects.
            public int GetHashCode(EventLogEntryModel eventLogEntry)
            {
                if (eventLogEntry is null) return 0;
                int hashActor = eventLogEntry.Actor == null ? 0 : eventLogEntry.Actor.GetHashCode(StringComparison.Ordinal);
                int hashEventDetails = eventLogEntry.EventDetails == null ? 0 : eventLogEntry.EventDetails.GetHashCode(StringComparison.Ordinal);
                int hashContactInfo = eventLogEntry.ActorContactInfo == null ? 0 : eventLogEntry.ActorContactInfo.GetHashCode(StringComparison.Ordinal);
                int hashOrganization = eventLogEntry.Organization == null ? 0 : eventLogEntry.Organization.GetHashCode(StringComparison.Ordinal);
                int hashWeight = eventLogEntry.Weight.GetHashCode();
                int hashTimeStamp = eventLogEntry.Timestamp.GetHashCode();

                //Calculate the hash code for eventLogEntry
                return hashActor ^ hashEventDetails ^ hashContactInfo ^ hashOrganization ^ hashWeight ^ hashTimeStamp;
            }
        }



    }
}
