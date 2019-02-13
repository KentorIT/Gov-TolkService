using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestView
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestViewId { get; set; }

        public int RequestId { get; set; }

        [ForeignKey(nameof(RequestId))]
        public Request Request { get; set; }

        public DateTimeOffset ViewedAt { get; set; }

        public int ViewedBy { get; set; }

        [ForeignKey(nameof(ViewedBy))]
        public AspNetUser ViewedByUser { get; set; }

        public int? ImpersonatingViewedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingViewedBy))]
        public AspNetUser ViewedByImpersonator { get; set; }
    
    }
}
