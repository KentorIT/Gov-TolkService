using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;
using Tolk.Web.Services;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.Web.Models
{
    public class OrderGroupModel
    {
        public int? OrderGroupId { get; set; }


        [Display(Name = "OrderNumber")]
        public string OrderGroupNumber { get; set; }

        #region methods

        public static OrderGroupModel GetModelFromOrderGroup(OrderGroup orderGroup)
        {
            return new OrderGroupModel
            {
                OrderGroupId = orderGroup.OrderGroupId,
                OrderGroupNumber = orderGroup.OrderGroupNumber
            };
        }

        #endregion
    }
}
