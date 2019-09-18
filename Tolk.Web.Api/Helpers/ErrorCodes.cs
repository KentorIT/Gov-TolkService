using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Tolk.Api.Payloads.Responses;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Api.Helpers
{
    public static class ErrorCodes
    {
        public const string UNAUTHORIZED = nameof(UNAUTHORIZED);
        public const string ORDER_NOT_FOUND = nameof(ORDER_NOT_FOUND);
        public const string COMPLAINT_NOT_FOUND = nameof(COMPLAINT_NOT_FOUND);
        public const string COMPLAINT_NOT_IN_CORRECT_STATE = nameof(COMPLAINT_NOT_IN_CORRECT_STATE);
        public const string REQUEST_NOT_FOUND = nameof(REQUEST_NOT_FOUND);
        public const string INTERPRETER_NOT_FOUND = nameof(INTERPRETER_NOT_FOUND);
        public const string INTERPRETER_OFFICIALID_ALREADY_SAVED = nameof(INTERPRETER_OFFICIALID_ALREADY_SAVED);
        public const string ATTACHMENT_NOT_FOUND = nameof(ATTACHMENT_NOT_FOUND);
        public const string REQUEST_NOT_IN_CORRECT_STATE = nameof(REQUEST_NOT_IN_CORRECT_STATE);
        public const string REQUEST_NOT_CORRECTLY_ANSWERED = nameof(REQUEST_NOT_CORRECTLY_ANSWERED);
        public const string REQUISITION_NOT_FOUND = nameof(REQUISITION_NOT_FOUND);
        public const string REQUISITION_NOT_IN_CORRECT_STATE = nameof(REQUISITION_NOT_IN_CORRECT_STATE);
    }
}
