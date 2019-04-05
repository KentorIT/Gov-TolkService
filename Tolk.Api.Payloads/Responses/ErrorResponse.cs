using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Api.Payloads.Responses
{
    public class ErrorResponse: ResponseBase
    {
        public string ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        /// <summary>
        /// This always should return false...
        /// </summary>
        public override bool Success { get => false; }

        public  ErrorResponse Copy()
        {
            return new ErrorResponse
            {
                ErrorCode = ErrorCode,
                ErrorMessage = ErrorMessage,
                StatusCode = StatusCode
            };
        }
    }
}
