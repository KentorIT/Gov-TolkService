using System;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Models
{
    public class CombinedTimeRange : IModel
    {
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public TimeSpan? ExpectedLength { get; set; }
        public DateTimeOffset? RespondedStartAt { get; set; }
        public DateTimeOffset CalculatedStartAt => RespondedStartAt ?? StartAt;
        public TimeSpan CalculatedEndAt => (RespondedStartAt?.Add(ExpectedLength.Value) ?? EndAt).TimeOfDay;

        public bool IsAwaitingStartAt => ExpectedLength.HasValue && !RespondedStartAt.HasValue;

        public string AsSwedishString =>
            IsAwaitingStartAt ?
            $"<span class=\"startlist-subrow\">Flexibel:</span> {StartAt.ToSwedishString("yyyy-MM-dd")} ({StartAt.ToSwedishString("HH:mm")}-{EndAt.ToSwedishString("HH:mm")})<br /><span class=\"startlist-subrow\">Uppdragets längd:</span> {ExpectedLength.Value:%h} tim {(((ExpectedLength.Value.Minutes % 60) == 0) ? string.Empty : (ExpectedLength.Value.ToString("%m") + " min"))}" :
            $"{CalculatedStartAt.ToSwedishString("yyyy-MM-dd")} {CalculatedStartAt.ToSwedishString("HH:mm")}-{CalculatedEndAt.ToSwedishString("hh\\:mm")}<br />";

    }
}
