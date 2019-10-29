using Microsoft.AspNetCore.Rewrite;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Tolk.Web.Authorization
{
    public class RedirectHostRule : IRule
    {
        public int StatusCode { get; } = (int)HttpStatusCode.MovedPermanently;
        public bool ExcludeLocalhost { get; set; } = true;
        public string InternalHost { get; set; }
        public string PublicOriginPath { get; set; }

        public void ApplyRule(RewriteContext context)
        {
            var request = context?.HttpContext.Request;
            var host = request.Host;
            if (ExcludeLocalhost && string.Equals(host.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = RuleResult.ContinueRules;
                return;
            }

            if (string.Equals(host.Host, InternalHost, StringComparison.OrdinalIgnoreCase))
            {
                var response = context.HttpContext.Response;
                response.StatusCode = StatusCode;
                response.Headers[HeaderNames.Location] =
                    $"{PublicOriginPath}{request.PathBase}{request.Path}{request.QueryString}";
                context.Result = RuleResult.EndResponse; // Do not continue processing the request     
                return;
            }
            context.Result = RuleResult.ContinueRules;
            return;
        }
    }
}
