using System;
using System.Collections.Generic;
using Tolk.Api.Payloads.Responses;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Api.Helpers
{
    public class TolkApiOptions : TolkBaseOptions
    {
        public Uri TolkWebBaseUrl { get; set; }
        public static List<ErrorResponse> BrokerApiErrorResponses =>
                new List<ErrorResponse>
                {
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.OrderNotFound, ErrorMessage = "The provided order number could not be found on a request connected to your organisation." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.RequestNotFound, ErrorMessage = "The provided order number has no request in the correct state for the call." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.InterpreterNotFound, ErrorMessage = "The provided interpreter was not found." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.InterpreterFaultyIntention, ErrorMessage = "Use Update for changes, and Create for adding a new interpreter." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.InterpreterAnswerNotValid, ErrorMessage = "The provided interpreter answer was not valid." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.InterpreterAnswerMainInterpereterDeclined, ErrorMessage = "When answering an order group request, the main interpreter must be provided. If the entire group request is to be declined, use DeclineGroup." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.InterpreterOfficialIdAlreadySaved, ErrorMessage = "The official interpreterId for the provided new interpreter was already saved." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.RequestNotInCorrectState, ErrorMessage = "The request or the underlying order was not in a correct state." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.RequestNotCorrectlyAnswered, ErrorMessage = "The request or the underlying order was not correctly answered" },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.OrderGroupNotFound, ErrorMessage = "The provided order group number could not be found on a request connected to your organisation." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.RequestGroupNotFound, ErrorMessage = "The provided order group number has no request in the correct state for the call." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.RequestGroupNotInCorrectState, ErrorMessage = "The request group or the underlying order group was not in a correct state." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.RequestIsPartOfAGroup, ErrorMessage = "The provided order is part of an order group. I cannot be accepted, acknowledged or declined separately" },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.AllRequirementsMustBeAnsweredOnAccept, ErrorMessage = "All requirement on a request must be accepted on accept." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.AcceptIsNotAllowedOnTheRequest, ErrorMessage = "The request is not allowed for initial accept, it must be fully answered directly." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.AcceptIsNotAllowedOnTheRequestGroup, ErrorMessage = "The request group is not allowed for initial accept, it must be fully answered directly." },
               };
        
        public static List<ErrorResponse> CustomerApiErrorResponses =>
                new List<ErrorResponse>
                {
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.CallingUserMissing, ErrorMessage = "You must provide a calling user for this call." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.OrderNotFound, ErrorMessage = "The provided order number could not be found, connected to your organisation." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.OrderGroupNotFound, ErrorMessage = "The provided order group number could not be found, connected to your organisation." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.OrderNotValid, ErrorMessage = "The provided order cannot be created." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.OrderNotInCorrectState, ErrorMessage = "The order was not in a correct state." },
               };

        public static List<ErrorResponse> CommonErrorResponses =>
                new List<ErrorResponse>
                {
                    new ErrorResponse { StatusCode = 500, ErrorCode = ErrorCodes.UnspecifiedProblem, ErrorMessage = "The api server encountered a problem. Please contact the support if the problem persists." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.Unauthorized, ErrorMessage = "The api user could not be authorized." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.ComplaintNotFound, ErrorMessage = "The provided order has no registered complaint." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.ComplaintNotInCorrectState, ErrorMessage = "The complaint was not in a correct state." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.AttachmentNotFound, ErrorMessage = "The file could not be found." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.RequisitionNotFound, ErrorMessage = "The provided order has no registered requisition." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.RequisitionNotInCorrectState, ErrorMessage = "The requisition was not in a correct state." },
                    new ErrorResponse { StatusCode = 400, ErrorCode = ErrorCodes.IncomingPayloadIsMissing, ErrorMessage = "The incoming payload seems to be missing. Please contact the support if the problem persists." },
               };
    }
}
