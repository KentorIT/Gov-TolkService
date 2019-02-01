using System;
using System.Collections.Generic;
using System.Text;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Xunit;

namespace Tolk.BusinessLogic.Tests.Entities
{
    public class ComplaintTests
    {
        [Theory]
        [InlineData(ComplaintStatus.DisputePendingTrial)]
        [InlineData(ComplaintStatus.TerminatedAsDisputeAccepted)]
        public void AnswerDispute_Valid(ComplaintStatus statusToSet)
        {
            var complaint = new Complaint { Status = ComplaintStatus.Disputed };
            var approveTime = DateTime.Parse("2019-01-31 14:06");
            var userId = 10;
            var impersonatorId = (int?)null;
            var message = "Answered dispute!";

            complaint.AnswerDispute(approveTime, userId, impersonatorId, message, statusToSet);

            Assert.Equal(statusToSet, complaint.Status);
            Assert.Equal(approveTime, complaint.AnswerDisputedAt);
            Assert.Equal(userId, complaint.AnswerDisputedBy);
            Assert.Equal(impersonatorId, complaint.ImpersonatingAnswerDisputedBy);
            Assert.Equal(message, complaint.AnswerDisputedMessage);
        }

        [Theory]
        // Setting status to confirmed
        [InlineData(ComplaintStatus.Confirmed, ComplaintStatus.Confirmed)]
        [InlineData(ComplaintStatus.Created, ComplaintStatus.Confirmed)]
        [InlineData(ComplaintStatus.Disputed, ComplaintStatus.Confirmed)]
        [InlineData(ComplaintStatus.DisputePendingTrial, ComplaintStatus.Confirmed)]
        [InlineData(ComplaintStatus.TerminatedAsDisputeAccepted, ComplaintStatus.Confirmed)]
        [InlineData(ComplaintStatus.TerminatedTrialConfirmedComplaint, ComplaintStatus.Confirmed)]
        [InlineData(ComplaintStatus.TerminatedTrialDeniedComplaint, ComplaintStatus.Confirmed)]
        // Setting status to created
        [InlineData(ComplaintStatus.Confirmed, ComplaintStatus.Created)]
        [InlineData(ComplaintStatus.Created, ComplaintStatus.Created)]
        [InlineData(ComplaintStatus.Disputed, ComplaintStatus.Created)]
        [InlineData(ComplaintStatus.DisputePendingTrial, ComplaintStatus.Created)]
        [InlineData(ComplaintStatus.TerminatedAsDisputeAccepted, ComplaintStatus.Created)]
        [InlineData(ComplaintStatus.TerminatedTrialConfirmedComplaint, ComplaintStatus.Created)]
        [InlineData(ComplaintStatus.TerminatedTrialDeniedComplaint, ComplaintStatus.Created)]
        // Setting status to disputed
        [InlineData(ComplaintStatus.Confirmed, ComplaintStatus.Disputed)]
        [InlineData(ComplaintStatus.Created, ComplaintStatus.Disputed)]
        [InlineData(ComplaintStatus.Disputed, ComplaintStatus.Disputed)]
        [InlineData(ComplaintStatus.DisputePendingTrial, ComplaintStatus.Disputed)]
        [InlineData(ComplaintStatus.TerminatedAsDisputeAccepted, ComplaintStatus.Disputed)]
        [InlineData(ComplaintStatus.TerminatedTrialConfirmedComplaint, ComplaintStatus.Disputed)]
        [InlineData(ComplaintStatus.TerminatedTrialDeniedComplaint, ComplaintStatus.Disputed)]
        // Setting status to terminated trial confirmed complaint
        [InlineData(ComplaintStatus.Confirmed, ComplaintStatus.TerminatedTrialConfirmedComplaint)]
        [InlineData(ComplaintStatus.Created, ComplaintStatus.TerminatedTrialConfirmedComplaint)]
        [InlineData(ComplaintStatus.Disputed, ComplaintStatus.TerminatedTrialConfirmedComplaint)]
        [InlineData(ComplaintStatus.DisputePendingTrial, ComplaintStatus.TerminatedTrialConfirmedComplaint)]
        [InlineData(ComplaintStatus.TerminatedAsDisputeAccepted, ComplaintStatus.TerminatedTrialConfirmedComplaint)]
        [InlineData(ComplaintStatus.TerminatedTrialConfirmedComplaint, ComplaintStatus.TerminatedTrialConfirmedComplaint)]
        [InlineData(ComplaintStatus.TerminatedTrialDeniedComplaint, ComplaintStatus.TerminatedTrialConfirmedComplaint)]
        // Setting status to terminated trial denied complaint
        [InlineData(ComplaintStatus.Confirmed, ComplaintStatus.TerminatedTrialDeniedComplaint)]
        [InlineData(ComplaintStatus.Created, ComplaintStatus.TerminatedTrialDeniedComplaint)]
        [InlineData(ComplaintStatus.Disputed, ComplaintStatus.TerminatedTrialDeniedComplaint)]
        [InlineData(ComplaintStatus.DisputePendingTrial, ComplaintStatus.TerminatedTrialDeniedComplaint)]
        [InlineData(ComplaintStatus.TerminatedAsDisputeAccepted, ComplaintStatus.TerminatedTrialDeniedComplaint)]
        [InlineData(ComplaintStatus.TerminatedTrialConfirmedComplaint, ComplaintStatus.TerminatedTrialDeniedComplaint)]
        [InlineData(ComplaintStatus.TerminatedTrialDeniedComplaint, ComplaintStatus.TerminatedTrialDeniedComplaint)]
        // statusToSet is TerminatedAsDisputeAccepted, but currentStatus is invalid
        [InlineData(ComplaintStatus.Confirmed, ComplaintStatus.TerminatedAsDisputeAccepted)]
        [InlineData(ComplaintStatus.Created, ComplaintStatus.TerminatedAsDisputeAccepted)]
        [InlineData(ComplaintStatus.DisputePendingTrial, ComplaintStatus.TerminatedAsDisputeAccepted)]
        [InlineData(ComplaintStatus.TerminatedAsDisputeAccepted, ComplaintStatus.TerminatedAsDisputeAccepted)]
        [InlineData(ComplaintStatus.TerminatedTrialConfirmedComplaint, ComplaintStatus.TerminatedAsDisputeAccepted)]
        [InlineData(ComplaintStatus.TerminatedTrialDeniedComplaint, ComplaintStatus.TerminatedAsDisputeAccepted)]
        // statusToSet is DisputePendingTrial, but currentStatus is invalid
        [InlineData(ComplaintStatus.Confirmed, ComplaintStatus.DisputePendingTrial)]
        [InlineData(ComplaintStatus.Created, ComplaintStatus.DisputePendingTrial)]
        [InlineData(ComplaintStatus.DisputePendingTrial, ComplaintStatus.DisputePendingTrial)]
        [InlineData(ComplaintStatus.TerminatedAsDisputeAccepted, ComplaintStatus.DisputePendingTrial)]
        [InlineData(ComplaintStatus.TerminatedTrialConfirmedComplaint, ComplaintStatus.DisputePendingTrial)]
        [InlineData(ComplaintStatus.TerminatedTrialDeniedComplaint, ComplaintStatus.DisputePendingTrial)]
        public void AnswerDispute_Invalid(ComplaintStatus currentStatus, ComplaintStatus statusToSet)
        {
            var complaint = new Complaint { Status = currentStatus };
            Assert.Throws<InvalidOperationException>(() => complaint.AnswerDispute(DateTime.Now, 10, null, "Test", statusToSet));
        }

        [Theory]
        [InlineData(ComplaintStatus.Confirmed)]
        [InlineData(ComplaintStatus.Disputed)]
        public void Answer_Valid(ComplaintStatus statusToSet)
        {
            var complaint = new Complaint { Status = ComplaintStatus.Created };
            var approveTime = DateTime.Parse("2019-01-31 14:06");
            var userId = 10;
            var impersonatorId = (int?)null;
            var message = "Answered dispute!";

            complaint.Answer(approveTime, userId, impersonatorId, message, statusToSet);

            Assert.Equal(statusToSet, complaint.Status);
            Assert.Equal(approveTime, complaint.AnsweredAt);
            Assert.Equal(userId, complaint.AnsweredBy);
            Assert.Equal(impersonatorId, complaint.ImpersonatingAnsweredBy);
            Assert.Equal(message, complaint.AnswerMessage);
        }

        [Theory]
        // Setting status to TerminatedAsDisputeAccepted
        [InlineData(ComplaintStatus.Confirmed, ComplaintStatus.TerminatedAsDisputeAccepted)]
        [InlineData(ComplaintStatus.Created, ComplaintStatus.TerminatedAsDisputeAccepted)]
        [InlineData(ComplaintStatus.Disputed, ComplaintStatus.TerminatedAsDisputeAccepted)]
        [InlineData(ComplaintStatus.DisputePendingTrial, ComplaintStatus.TerminatedAsDisputeAccepted)]
        [InlineData(ComplaintStatus.TerminatedAsDisputeAccepted, ComplaintStatus.TerminatedAsDisputeAccepted)]
        [InlineData(ComplaintStatus.TerminatedTrialConfirmedComplaint, ComplaintStatus.TerminatedAsDisputeAccepted)]
        [InlineData(ComplaintStatus.TerminatedTrialDeniedComplaint, ComplaintStatus.TerminatedAsDisputeAccepted)]
        // Setting status to created
        [InlineData(ComplaintStatus.Confirmed, ComplaintStatus.Created)]
        [InlineData(ComplaintStatus.Created, ComplaintStatus.Created)]
        [InlineData(ComplaintStatus.Disputed, ComplaintStatus.Created)]
        [InlineData(ComplaintStatus.DisputePendingTrial, ComplaintStatus.Created)]
        [InlineData(ComplaintStatus.TerminatedAsDisputeAccepted, ComplaintStatus.Created)]
        [InlineData(ComplaintStatus.TerminatedTrialConfirmedComplaint, ComplaintStatus.Created)]
        [InlineData(ComplaintStatus.TerminatedTrialDeniedComplaint, ComplaintStatus.Created)]
        // Setting status to DisputePendingTrial
        [InlineData(ComplaintStatus.Confirmed, ComplaintStatus.DisputePendingTrial)]
        [InlineData(ComplaintStatus.Created, ComplaintStatus.DisputePendingTrial)]
        [InlineData(ComplaintStatus.Disputed, ComplaintStatus.DisputePendingTrial)]
        [InlineData(ComplaintStatus.DisputePendingTrial, ComplaintStatus.DisputePendingTrial)]
        [InlineData(ComplaintStatus.TerminatedAsDisputeAccepted, ComplaintStatus.DisputePendingTrial)]
        [InlineData(ComplaintStatus.TerminatedTrialConfirmedComplaint, ComplaintStatus.DisputePendingTrial)]
        [InlineData(ComplaintStatus.TerminatedTrialDeniedComplaint, ComplaintStatus.DisputePendingTrial)]
        // Setting status to terminated trial confirmed complaint
        [InlineData(ComplaintStatus.Confirmed, ComplaintStatus.TerminatedTrialConfirmedComplaint)]
        [InlineData(ComplaintStatus.Created, ComplaintStatus.TerminatedTrialConfirmedComplaint)]
        [InlineData(ComplaintStatus.Disputed, ComplaintStatus.TerminatedTrialConfirmedComplaint)]
        [InlineData(ComplaintStatus.DisputePendingTrial, ComplaintStatus.TerminatedTrialConfirmedComplaint)]
        [InlineData(ComplaintStatus.TerminatedAsDisputeAccepted, ComplaintStatus.TerminatedTrialConfirmedComplaint)]
        [InlineData(ComplaintStatus.TerminatedTrialConfirmedComplaint, ComplaintStatus.TerminatedTrialConfirmedComplaint)]
        [InlineData(ComplaintStatus.TerminatedTrialDeniedComplaint, ComplaintStatus.TerminatedTrialConfirmedComplaint)]
        // Setting status to terminated trial denied complaint
        [InlineData(ComplaintStatus.Confirmed, ComplaintStatus.TerminatedTrialDeniedComplaint)]
        [InlineData(ComplaintStatus.Created, ComplaintStatus.TerminatedTrialDeniedComplaint)]
        [InlineData(ComplaintStatus.Disputed, ComplaintStatus.TerminatedTrialDeniedComplaint)]
        [InlineData(ComplaintStatus.DisputePendingTrial, ComplaintStatus.TerminatedTrialDeniedComplaint)]
        [InlineData(ComplaintStatus.TerminatedAsDisputeAccepted, ComplaintStatus.TerminatedTrialDeniedComplaint)]
        [InlineData(ComplaintStatus.TerminatedTrialConfirmedComplaint, ComplaintStatus.TerminatedTrialDeniedComplaint)]
        [InlineData(ComplaintStatus.TerminatedTrialDeniedComplaint, ComplaintStatus.TerminatedTrialDeniedComplaint)]
        // statusToSet is Confirmed, but currentStatus is invalid
        [InlineData(ComplaintStatus.Confirmed, ComplaintStatus.Confirmed)]
        [InlineData(ComplaintStatus.Disputed, ComplaintStatus.Confirmed)]
        [InlineData(ComplaintStatus.DisputePendingTrial, ComplaintStatus.Confirmed)]
        [InlineData(ComplaintStatus.TerminatedAsDisputeAccepted, ComplaintStatus.Confirmed)]
        [InlineData(ComplaintStatus.TerminatedTrialConfirmedComplaint, ComplaintStatus.Confirmed)]
        [InlineData(ComplaintStatus.TerminatedTrialDeniedComplaint, ComplaintStatus.Confirmed)]
        // statusToSet is Disputed, but currentStatus is invalid
        [InlineData(ComplaintStatus.Confirmed, ComplaintStatus.Disputed)]
        [InlineData(ComplaintStatus.Disputed, ComplaintStatus.Disputed)]
        [InlineData(ComplaintStatus.DisputePendingTrial, ComplaintStatus.Disputed)]
        [InlineData(ComplaintStatus.TerminatedAsDisputeAccepted, ComplaintStatus.Disputed)]
        [InlineData(ComplaintStatus.TerminatedTrialConfirmedComplaint, ComplaintStatus.Disputed)]
        [InlineData(ComplaintStatus.TerminatedTrialDeniedComplaint, ComplaintStatus.Disputed)]
        public void Answer_Invalid(ComplaintStatus currentStatus, ComplaintStatus statusToSet)
        {
            var complaint = new Complaint { Status = currentStatus };
            Assert.Throws<InvalidOperationException>(() => complaint.Answer(DateTime.Now, 10, null, "Test", statusToSet));
        }
    }
}
