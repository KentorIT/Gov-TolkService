using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class RequisitionAttachment
    {
        public int RequisitionId { get; set; }

        [ForeignKey(nameof(RequisitionId))]
        public Requisition Requisition { get; set; }

        public int AttachmentId { get; set; }

        [ForeignKey(nameof(AttachmentId))]
        public Attachment Attachment { get; set; }
    }
}

