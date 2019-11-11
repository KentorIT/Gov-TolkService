using System;
using System.ComponentModel.DataAnnotations.Schema;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestGroupStatusConfirmation : StatusConfirmationBase
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestGroupStatusConfirmationId { get; set; }

        public int RequestGroupId { get; set; }

        [ForeignKey(nameof(RequestGroupId))]
        public RequestGroup RequestGroup { get; set; }

        public RequestStatus RequestStatus { get; set; }
    }
}
