using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class TemporaryAttachmentGroup
    {
        public Guid TemporaryAttachmentGroupKey { get; set; }

        [Required]
        public DateTimeOffset CreatedAt { get; set; }

        public int AttachmentId { get; set; }

        [ForeignKey(nameof(AttachmentId))]
        public Attachment Attachment { get; set; }
    }
}

