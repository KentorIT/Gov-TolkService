using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Tolk.Api.Payloads.Responses;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Api.Helpers
{
    public class TolkApiOptions : TolkBaseOptions
    {
        public string TolkWebBaseUrl { get; set; }
        public List<ErrorResponse> ErrorResponses => 
                new List<ErrorResponse>
                {
                    new ErrorResponse { StatusCode = 403, ErrorCode = ErrorCodes.UNAUTHORIZED, ErrorMessage = "The api user could not be authorized." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.ORDER_NOT_FOUND, ErrorMessage = "The provided order number could not be found on a request connected to your organsation." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.COMPLAINT_NOT_FOUND, ErrorMessage = "The provided order has no registered complaint." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.COMPLAINT_NOT_IN_CORRECT_STATE, ErrorMessage = "The complaint was not in a correct state." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.REQUEST_NOT_FOUND, ErrorMessage = "The provided order number has no request in the correct state for the call." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.INTERPRETER_NOT_FOUND, ErrorMessage = "The provided interpreter was not found." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.INTERPRETER_OFFICIALID_ALREADY_SAVED, ErrorMessage = "The official interpreterId for the provided new interpreter was already saved." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.ATTACHMENT_NOT_FOUND, ErrorMessage = "The file coould not be found." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.REQUEST_NOT_IN_CORRECT_STATE, ErrorMessage = "The request or the underlying order was not in a correct state." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.REQUEST_NOT_CORRECTLY_ANSWERED, ErrorMessage = "The request or the underlying order was not correctly answered" },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.REQUISITION_NOT_FOUND, ErrorMessage = "The provided order has no registered requisition." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.REQUISITION_NOT_IN_CORRECT_STATE, ErrorMessage = "The requisition was not in a correct state." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.ORDER_GROUP_NOT_FOUND, ErrorMessage = "The provided order group number could not be found on a request connected to your organsation." },
                    new ErrorResponse { StatusCode = 401, ErrorCode = ErrorCodes.REQUEST_GROUP_NOT_FOUND, ErrorMessage = "The provided order group number has no request in the correct state for the call." },
               };
    }
}
