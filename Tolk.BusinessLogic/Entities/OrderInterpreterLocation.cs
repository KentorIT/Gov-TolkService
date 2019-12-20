using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderInterpreterLocation : OrderInterpreterLocationBase
    {
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        [MaxLength(100)]
        public string Street { get; set; }

        [MaxLength(100)]
        public string City { get; set; }

        [MaxLength(255)]
        public string OffSiteContactInformation { get; set; }

        public string FullAddress { get => $"{Street}\n {City}"; }

    }
}
