using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class OrderChangeContactPersonModel
    {
            public int OrderId { get; set; }

            public int? ContactPersonId { get; set; }
    }
}
