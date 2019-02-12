using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Configuration;

namespace Tolk.Web.Services
{
    public class HelpLinkService
    {
        public string BaseAddress { get; set; }
        public string PageName { get; set; }
        public HelpLinkService(IConfiguration configuration)
        {
            BaseAddress = configuration["UserDocumentation:BaseUrl"];
        }

        /*       public HtmlString HelpLink(string anchorpoint)
               {
                   string link = $"help-link=\"{anchorpoint}\"";
                   return new HtmlString(link);
               }
               */
        public string GenerateUrl(string anchorpoint)
        {
            string link = BaseAddress;
            link += PageName;
            if (!string.IsNullOrEmpty(anchorpoint))
            {
                link += "#" + anchorpoint;
            }

            return link;
        }

        public HtmlString Anchor(string anchorpoint)
        {
            return new HtmlString($"<a href=\"{GenerateUrl(anchorpoint)}\" target=\"_blank\"><span class=\"form-entry-help glyphicon glyphicon-question-sign\"></span></a>");
        }

        public HtmlString HeaderAnchor(string anchorpoint = null)
        {
            return new HtmlString($"<a href=\"{GenerateUrl(anchorpoint)}\" target=\"_blank\" class=\"pull-right\"><span class=\"glyphicon glyphicon-question-sign\"></span></a>");
        }
    }
}
