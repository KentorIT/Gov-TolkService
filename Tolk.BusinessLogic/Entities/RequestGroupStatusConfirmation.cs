using System;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestGroupStatusConfirmation
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestGroupStatusConfirmationId { get; set; }

        public int RequestGroupId { get; set; }

        [ForeignKey(nameof(RequestGroupId))]
        public RequestGroup RequestGroup { get; set; }

        public RequestStatus RequestStatus { get; set; }

        public DateTimeOffset ConfirmedAt { get; set; }

        public int ConfirmedBy { get; set; }

        [ForeignKey(nameof(ConfirmedBy))]
        public AspNetUser ConfirmedByUser { get; set; }

        public int? ImpersonatingConfirmedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingConfirmedBy))]
        public AspNetUser ImpersonatingConfirmedByUser { get; set; }
    }
}
