using System;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    /// <summary>
    /// NOTE: This is connected to the view CustomerStartListRows, so it is not possible to change things here, without changing the view accordingly.
    /// </summary>
    /// 

    public class CustomerStartListRow : StartListRow
    {
        public int OrderId { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public OrderStatus? OrderGroupStatus { get; set; }
        public int CreatedBy { get; set; }
        public int? CustomerUnitId { get; set; }
        public int CustomerOrganisationId { get; set; }
        public bool CustomerUnitIsActive { get; set; }
        public int? OrderGroupId { get; set; }
        public int? ContactPersonId { get; set; }
    }
}
