using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Tolk.Web.Models
{
    public class CheckboxGroup
    {
        public HashSet<SelectListItem> SelectedItems { get; set; }
    }
}
