using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class StatusConfirmationBase
    {
        public DateTimeOffset ConfirmedAt { get; set; }

        public int ConfirmedBy { get; set; }

        [ForeignKey(nameof(ConfirmedBy))]
        public AspNetUser ConfirmedByUser { get; set; }

        public int? ImpersonatingConfirmedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingConfirmedBy))]
        public AspNetUser ImpersonatingConfirmedByUser { get; set; }
    }
}
