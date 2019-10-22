using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Models
{
    public class OrderGroupModel
    {
        public int? OrderGroupId { get; set; }


        [Display(Name = "OrderNumber")]
        public string OrderGroupNumber { get; set; }

        #region methods

        internal static OrderGroupModel GetModelFromOrderGroup(OrderGroup orderGroup)
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
