using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Api.Helpers
{
    public class TolkApiOptions : TolkBaseOptions
    {
        public string TolkWebBaseUrl { get; set; }
    }
}
