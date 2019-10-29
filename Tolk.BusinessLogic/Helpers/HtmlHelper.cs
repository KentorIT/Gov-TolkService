using System;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Helpers
{
    public static class HtmlHelper
    {
        public enum ViewTab
        {
            Default = 0,
            Requisition = 1,
            Complaint = 2,
        }

        #region URLs

        public static Uri GetOrderGroupViewUrl(Uri origin, int orderGroupId)
        {
            return origin.BuildUri($"OrderGroup/View/{orderGroupId}");
        }

        public static Uri GetOrderViewUrl(Uri origin, int orderId, string query = null)
        {
            return origin.BuildUri($"Order/View/{orderId}", query);
        }

        public static Uri GetOrderPrintUrl(Uri origin, int orderId)
        {
            return origin.BuildUri($"Order/Print/{orderId}");
        }

        public static Uri GetRequestViewUrl(Uri origin, int requestId, string query = null)
        {
            return origin.BuildUri($"Request/View/{requestId}", query);
        }

        public static Uri GetRequestGroupViewUrl(Uri origin, int requestGroupId)
        {
            return origin.BuildUri($"RequestGroup/View/{requestGroupId}");
        }

        public static Uri GetWebHookListUrl(Uri origin)
        {
            return origin.BuildUri("WebHook/List");
        }

        #endregion

        #region Anchors & Buttons

        private static string GetAnchorTag(Uri href, string text, string classes)
        {
            return $"<a href=\"{href}\" class=\"{classes}\">{text}</a>";
        }

        public static string GetButtonDefaultLargeTag(Uri href, string text)
        {
            return GetAnchorTag(href, text, "btn btn-default btn-large");
        }

        #endregion

        internal static string ToHtmlBreak(string text)
        {
            return text?.Replace("\n", "<br />");
        }

    }
}
