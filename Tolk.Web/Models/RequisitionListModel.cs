﻿using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class RequisitionListModel
    {
        public string Action { get; set; }

        public RequisitionFilterModel FilterModel { get; set; }

        public IEnumerable<RequisitionListItemModel> Items { get; set; }
    }
}
