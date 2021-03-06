﻿using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class CustomerListItemModel
    {
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public string ParentName { get; set; }
        public string OrganisationNumber { get; set; }
        public PriceListType PriceListType { get; set; }
        public static string ColorClassName => CssClassHelper.GetColorClassNameForItemStatus(true);
    }
}
