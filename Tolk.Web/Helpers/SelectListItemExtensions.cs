using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace Tolk.Web.Helpers
{
    public static class SelectListItemExtensions
    {
        public static IEnumerable<ExtendedSelectListItem> GetExtendedSelectListItems(this IEnumerable<SerializableExtendedSelectListItem> items)
        {
            return items.Select(i => new ExtendedSelectListItem
            {
                Value = i.Value,
                Text = i.Text,
                Disabled = i.Disabled,
                Selected = i.Selected,
                AdditionalDataAttribute = i.AdditionalDataAttribute,
            });
        }
        public static IEnumerable<SelectListItem> GetSelectListItems(this IEnumerable<SerializableExtendedSelectListItem> items)
        {
            return items.Select(i => new SelectListItem
            {
                Value = i.Value,
                Text = i.Text,
                Disabled = i.Disabled,
                Selected = i.Selected,
            });
        }
    }
}
