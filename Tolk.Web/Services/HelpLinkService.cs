using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Configuration;
using System;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.Web.Services
{
    public class HelpLinkService
    {
        public string BaseAddress { get; set; }

        public string PageName { get; set; }

        public HelpLinkService(IConfiguration configuration)
        {
            BaseAddress = configuration != null ? configuration["UserDocumentation:BaseUrl"] : string.Empty;
        }

        public Uri GenerateUrl(string anchorpoint)
        {
            string link = BaseAddress;
            link += PageName;
            if (!string.IsNullOrEmpty(anchorpoint))
            {
                link += "#" + anchorpoint;
            }

            return link.AsUri();
        }

        public HtmlString Anchor(string anchorpoint) => new HtmlString($"<a href=\"{GenerateUrl(anchorpoint)}\" aria-label=\"Hjälp från manual\" target=\"_blank\"><span class=\"form-entry-help glyphicon glyphicon-question-sign\"></span></a>");

        public HtmlString HeaderAnchor(string anchorpoint = null) => new HtmlString($"<a href=\"{GenerateUrl(anchorpoint)}\" aria-label=\"Hjälp från manual\" target=\"_blank\" class=\"pull-right\"><span class=\"glyphicon glyphicon-question-sign\"></span></a>");

        public HtmlString ButtonHelpIcon(string pageName)
        {
            PageName = pageName;
            return new HtmlString($"<a href=\"{GenerateUrl(null)}\" aria-label=\"Hjälp från manual\" target=\"_blank\"><span class=\"glyphicon glyphicon-question-sign help-sign-medium\"></span></a>");
        }

        public string BrokerManual => $"{BaseAddress}formedling-anvandarmanual/";

        public string CustomerManual => $"{BaseAddress}myndighet-anvandarmanual/";

        public string SystemAdminManual => $"{BaseAddress}systemadmin-anvandarmanual/";

        public string ApplicationAdminManual => $"{BaseAddress}appadmin-anvandarmanual/";
    }
}

