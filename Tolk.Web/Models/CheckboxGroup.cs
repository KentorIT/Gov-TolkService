using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Tolk.Web.Models
{
    public class CheckboxGroup
    {
        public IEnumerable<SelectListItem> SelectedItems { get; set; }
    }
}
