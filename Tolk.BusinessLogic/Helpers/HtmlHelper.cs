using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Helpers
{
    public static class HtmlHelper
    {
        public static string GetAnchorTag(string href)
        {
            return GetAnchorTag(href, href);
        }

        public static string GetAnchorTag(string href, string text)
        {
            return $"<a href=\"{href}\">{text}</a>";
        }

        public static string ToHtmlBreak(string text)
        {
            return text.Replace("\n", "<br />");
        }
    }
}
