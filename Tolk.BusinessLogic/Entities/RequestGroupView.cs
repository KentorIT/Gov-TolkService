using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestGroupView : ViewBase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestGroupViewId { get; set; }

        public int RequestGroupId { get; set; }

        [ForeignKey(nameof(RequestGroupId))]
        public RequestGroup RequestGroup { get; set; }
    }
}
