
namespace Tolk.Web.Api.Helpers
{
    public static class ErrorCodes
    {
        public const string UnspecifiedProblem = nameof(UnspecifiedProblem);
        public const string Unauthorized = nameof(Unauthorized);
        public const string OrderNotFound = nameof(OrderNotFound);
        public const string ComplaintNotFound = nameof(ComplaintNotFound);
        public const string ComplaintNotInCorrectState = nameof(ComplaintNotInCorrectState);
        public const string RequestNotFound = nameof(RequestNotFound);
        public const string InterpreterNotFound = nameof(InterpreterNotFound);
        public const string InterpreterFaultyIntention = nameof(InterpreterFaultyIntention);
        public const string InterpreterAnswerNotValid = nameof(InterpreterAnswerNotValid);
        public const string InterpreterAnswerMainInterpereterDeclined = nameof(InterpreterAnswerMainInterpereterDeclined);
        public const string InterpreterOfficialIdAlreadySaved = nameof(InterpreterOfficialIdAlreadySaved);
        public const string AttachmentNotFound = nameof(AttachmentNotFound);
        public const string RequestNotInCorrectState = nameof(RequestNotInCorrectState);
        public const string RequestNotCorrectlyAnswered = nameof(RequestNotCorrectlyAnswered);
        public const string RequisitionNotFound = nameof(RequisitionNotFound);
        public const string RequisitionNotInCorrectState = nameof(RequisitionNotInCorrectState);
        public const string OrderGroupNotFound = nameof(OrderGroupNotFound);
        public const string RequestGroupNotFound = nameof(RequestGroupNotFound);
        public const string RequestGroupNotInCorrectState = nameof(RequestGroupNotInCorrectState);
        public const string IncomingPayloadIsMissing = nameof(IncomingPayloadIsMissing);
        public const string RequestIsPartOfAGroup = nameof(RequestIsPartOfAGroup);
        public const string CallingUserMissing = nameof(CallingUserMissing);
        public const string OrderNotValid = nameof(OrderNotValid);
        public const string OrderNotInCorrectState = nameof(OrderNotInCorrectState);
        public const string AllRequirementsMustBeAnsweredOnAccept = nameof(AllRequirementsMustBeAnsweredOnAccept);
    }
}
