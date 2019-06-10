using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class FaqDisplayUserRole
    {

        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        //public int FaqDisplayUserRoleId { get; set; }

        public int FaqId { get; set; }

        [ForeignKey(nameof(FaqId))]
        public Faq Faq { get; set; }

        public DisplayUserRole DisplayUserRole { get; set; }

    }
}
