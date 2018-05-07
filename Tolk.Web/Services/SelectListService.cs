using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Services
{
    public static class SelectListService
    {
        public static IEnumerable<SelectListItem> Regions { get; } =
            Region.Regions.Select(r => new SelectListItem
            {
                Value = r.Id.ToString(),
                Text = r.Name
            })
            .ToList().AsReadOnly();
    }
}
