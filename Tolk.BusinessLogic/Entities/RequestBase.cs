﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestBase
    {
        public int RankingId { get; set; }

        public RequestStatus Status { get; set; }

        public Ranking Ranking { get; set; }

        /// <summary>
        /// The time (inclusive) when the request is expired. If null, expiry must be manually defined
        /// </summary>
        public DateTimeOffset? ExpiresAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        [MaxLength(1000)]
        public string BrokerMessage { get; set; }

        [MaxLength(1000)]
        public string DenyMessage { get; set; }

        public DateTimeOffset? RecievedAt { get; set; }

        public int? ReceivedBy { get; set; }

        [ForeignKey(nameof(ReceivedBy))]
        public AspNetUser ReceivedByUser { get; set; }

        public int? ImpersonatingReceivedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingReceivedBy))]
        public AspNetUser ReceivedByImpersonator { get; set; }

        public DateTimeOffset? AnswerDate { get; set; }

        public int? AnsweredBy { get; set; }

        [ForeignKey(nameof(AnsweredBy))]
        public AspNetUser AnsweringUser { get; set; }

        public int? ImpersonatingAnsweredBy { get; set; }

        [ForeignKey(nameof(ImpersonatingAnsweredBy))]
        public AspNetUser AnsweredByImpersonator { get; set; }

        public DateTimeOffset? AnswerProcessedAt { get; set; }

        public int? AnswerProcessedBy { get; set; }

        [ForeignKey(nameof(AnswerProcessedBy))]
        public AspNetUser ProcessingUser { get; private set; }

        public int? ImpersonatingAnswerProcessedBy { get; set; }

        [ForeignKey(nameof(ImpersonatingAnswerProcessedBy))]
        public AspNetUser AnswerProcessedByImpersonator { get; private set; }

        public DateTimeOffset? CancelledAt { get; set; }

        public int? CancelledBy { get; set; }

        [ForeignKey(nameof(CancelledBy))]
        public AspNetUser CancelledByUser { get; set; }

        public int? ImpersonatingCanceller { get; set; }

        [ForeignKey(nameof(ImpersonatingCanceller))]
        public AspNetUser CancelledByImpersonator { get; set; }

        [MaxLength(1000)]
        public string CancelMessage { get; set; }

        /// <summary>
        /// If true, this Request wil not be followed by another to the next broker.
        /// </summary>
        public bool IsTerminalRequest { get; set; }

        #region Status checks

        public bool IsAcceptedOrApproved
        {
            get => IsAccepted || Status == RequestStatus.Approved;
        }

        public bool IsAccepted
        {
            get => Status == RequestStatus.Accepted || Status == RequestStatus.AcceptedNewInterpreterAppointed;
        }

        public bool StatusNotToBeDisplayedForBroker
        {
            get => Status == RequestStatus.NoDeadlineFromCustomer || Status == RequestStatus.AwaitingDeadlineFromCustomer || Status == RequestStatus.InterpreterReplaced;
        }

        public bool CanDecline
        {
            get => IsToBeProcessedByBroker;
        }

        public bool CanApprove
        {
            get => IsAccepted;
        }

        public bool CanDeny
        {
            get => IsAccepted;
        }

        public bool IsToBeProcessedByBroker
        {
            get => Status == RequestStatus.Created || Status == RequestStatus.Received;
        }

        #endregion
    }
}