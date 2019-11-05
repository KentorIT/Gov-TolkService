using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestGroupAttachment
    {
        public int RequestGroupId { get; set; }

        [ForeignKey(nameof(RequestGroupId))]
        public RequestGroup RequestGroup { get; set; }

        public int AttachmentId { get; set; }

        [ForeignKey(nameof(AttachmentId))]
        public Attachment Attachment { get; set; }
    }
}
