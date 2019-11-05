using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestView : ViewBase
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestViewId { get; set; }

        public int RequestId { get; set; }

        [ForeignKey(nameof(RequestId))]
        public Request Request { get; set; }    
    }
}
