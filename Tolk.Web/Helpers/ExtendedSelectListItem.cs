using System.ComponentModel;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Tolk.Web.Helpers
{
    public class ExtendedSelectListItem : SelectListItem
    {
        /// <summary>
        /// Additional data attribute to render
        /// </summary>
        [DefaultValue(null)]
        public string AdditionalDataAttribute { get; set; } = null;

    }
}
