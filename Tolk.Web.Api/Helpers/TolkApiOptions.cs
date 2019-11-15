using System.Collections.Generic;
using System;
using Tolk.Api.Payloads.Responses;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Api.Helpers
{
    public class TolkApiOptions : TolkBaseOptions
    {
        public Uri TolkWebBaseUrl { get; set; }
        public static List<ErrorResponse> ErrorResponses => 
                new List<ErrorResponse>
                {
                    new ErrorResponse { StatusCode = 500, ErrorCode = ErrorCodes.UnspecifiedProblem, ErrorMessage = "The api server encountered a problem. Please contact the support if the problem persists." },
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.Unauthorized, ErrorMessage = "The api user could not be authorized." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.OrderNotFound, ErrorMessage = "The provided order number could not be found on a request connected to your organisation." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.ComplaintNotFound, ErrorMessage = "The provided order has no registered complaint." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.ComplaintNotInCorrectState, ErrorMessage = "The complaint was not in a correct state." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.RequestNotFound, ErrorMessage = "The provided order number has no request in the correct state for the call." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.InterpreterNotFound, ErrorMessage = "The provided interpreter was not found." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.InterpreterFaultyIntention, ErrorMessage = "Use Update for changes, and Create for adding a new interpreter." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.InterpreterAnswerNotValid, ErrorMessage = "The provided interpreter answer was not valid." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.InterpreterAnswerMainInterpereterDeclined, ErrorMessage = "When answering an order group request, the main interpreter must be provided. If the entire group request is to be declined, use DeclineGroup." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.InterpreterOfficialIdAlreadySaved, ErrorMessage = "The official interpreterId for the provided new interpreter was already saved." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.AttachmentNotFound, ErrorMessage = "The file coould not be found." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.RequestNotInCorrectState, ErrorMessage = "The request or the underlying order was not in a correct state." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.RequestNotCorrectlyAnswered, ErrorMessage = "The request or the underlying order was not correctly answered" },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.RequisitionNotFound, ErrorMessage = "The provided order has no registered requisition." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.RequisitionNotInCorrectState, ErrorMessage = "The requisition was not in a correct state." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.OrderGroupNotFound, ErrorMessage = "The provided order group number could not be found on a request connected to your organisation." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.RequestGroupNotFound, ErrorMessage = "The provided order group number has no request in the correct state for the call." },
                    new ErrorResponse { StatusCode = 500, ErrorCode = ErrorCodes.IncomingPayloadIsMissing, ErrorMessage = "The incoming payload seems to be missing. Please contact the support if the problem persists." },
               };
    }
}
