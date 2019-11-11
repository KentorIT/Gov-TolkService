using System;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestStatusConfirmation : StatusConfirmationBase
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestStatusConfirmationId { get; set; }

        public int RequestId { get; set; }

        [ForeignKey(nameof(RequestId))]
        public Request Request { get; set; }

        public RequestStatus RequestStatus { get; set; }
    }
}
