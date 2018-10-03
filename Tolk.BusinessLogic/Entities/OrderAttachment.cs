using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderAttachment
    {
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; }

        public int AttachmentId { get; set; }

        [ForeignKey(nameof(AttachmentId))]
        public Attachment Attachment { get; set; }
    }
}
