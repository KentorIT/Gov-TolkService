using System;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class UnitListItemModel
    {
        public int CustomerUnitId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public string CreatedBy { get; set; }

        public bool IsActive { get; set; }

        public string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(IsActive);

    }
}
