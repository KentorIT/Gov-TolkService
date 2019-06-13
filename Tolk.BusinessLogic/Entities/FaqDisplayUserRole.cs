using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class FaqDisplayUserRole
    {

        public int FaqId { get; set; }

        [ForeignKey(nameof(FaqId))]
        public Faq Faq { get; set; }

        public DisplayUserRole DisplayUserRole { get; set; }

    }
}
