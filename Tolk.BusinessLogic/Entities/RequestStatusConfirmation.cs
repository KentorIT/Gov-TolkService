using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestStatusConfirmation
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestStatusConfirmationId { get; set; }

        public int RequestId { get; set; }

        [ForeignKey(nameof(RequestId))]
        public Request Request { get; set; }

        public RequestStatus RequestStatus { get; set; }

        public DateTimeOffset? ConfirmedAt { get; set; }

        public int? ConfirmedBy { get; set; }

        [ForeignKey(nameof(ConfirmedBy))]
        public AspNetUser ConfirmedByUser { get; set; }

        public int? ImpersonatingConfirmedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingConfirmedBy))]
        public AspNetUser ImpersonatingConfirmedByUser { get; set; }
    }
}
