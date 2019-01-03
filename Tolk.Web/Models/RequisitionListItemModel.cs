using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class RequisitionListItemModel
    {
        public int OrderRequestId { get; set; }

        public RequisitionStatus Status { get; set; }

        public string OrderNumber { get; set; }

        public string Language { get; set; }

        [NoDisplayName]
        public virtual TimeRange OrderDateAndTime { get; set; }

        public string Controller { get; set; }

        public string ColorClassName { get => CssClassHelper.GetColorClassNameForRequisitionStatus(Status); }
    }
}
