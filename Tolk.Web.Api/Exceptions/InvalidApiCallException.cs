using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Api.Exceptions
{
    public class InvalidApiCallException : Exception
    {
        public InvalidApiCallException() { }
        public InvalidApiCallException(string errorCode)
        {
            ErrorCode = errorCode;
        }

        public InvalidApiCallException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public string ErrorCode { get; set; }
    }
}
