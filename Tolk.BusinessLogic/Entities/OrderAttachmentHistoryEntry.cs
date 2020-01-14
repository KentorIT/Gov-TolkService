using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderAttachmentHistoryEntry
    {

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderAttachmentHistoryEntryId { get; set; }

        public int OrderChangeLogEntryId { get; set; }

        public int AttachmentId { get; set; }

        [ForeignKey(nameof(OrderChangeLogEntryId))]
        public OrderChangeLogEntry OrderChangeLogEntry { get; set; }

        [ForeignKey(nameof(AttachmentId))]
        public Attachment Attachment { get; set; }

        /// <summary>
        /// This is used when an attachment for the ordergoup should be removed for the individual order
        /// </summary>
        public bool OrderGroupAttachmentRemoved { get; set; }

    }
}
