using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Tolk.BusinessLogic.Entities
{
    public class OutboundEmail
    {
        public OutboundEmail(
            string recipient,
            string subject,
            string body,
            DateTimeOffset createdAt)
        {
            Recipient = recipient;
            Subject = subject;
            Body = body;
            CreatedAt = createdAt;
        }

        public int OutboundEmailId { get; private set; }
        
        [Required]
        public string Recipient { get; private set; }

        [Required]
        public string Subject { get; private set; }

        [Required]
        public string Body { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public DateTimeOffset? DeliveredAt { get; set; }
    }
}
