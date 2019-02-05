using System;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Api.Payloads.Responses
{
    public class ComplaintDetailsResponse : ResponseBase
    {
        public string OrderNumber { get; set; }

        public string Status { get; set; }

        public string ComplaintType { get; set; }

        public string Message { get; set; }

        public string AnswerMessage { get; set; }

        public string AnswerDisputedMessage { get; set; }

        public string TerminationMessage { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? AnsweredAt { get; set; }

        public DateTimeOffset? AnswerDisputedAt { get; set; }

        public DateTimeOffset? TerminatedAt { get; set; }


    }
}
