using System;
using System.Collections.Generic;
using System.Text;

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

        public static string GetOrderGroupViewUrl(string origin, int orderGroupId)
        {
            return $"{origin}/OrderGroup/View/{orderGroupId}";
        }

        public static string GetOrderViewUrl(string origin, int orderId)
        {
            return $"{origin}/Order/View/{orderId}";
        }

        public static string GetOrderPrintUrl(string origin, int orderId)
        {
            return $"{origin}/Order/Print/{orderId}";
        }

        public static string GetRequestViewUrl(string origin, int requestId)
        {
            return $"{origin}/Request/View/{requestId}";
        }

        public static string GetRequestGroupViewUrl(string origin, int requestGroupId)
        {
            return $"{origin}/RequestGroup/View/{requestGroupId}";
        }

        public static string GetWebHookListUrl(string origin)
        {
            return $"{origin}/WebHook/List";
        }

        #endregion

        #region Anchors & Buttons

        public static string GetOrderViewAnchorTag(string origin, int orderId, string text, ViewTab tab = ViewTab.Default)
        {
            switch (tab)
            {
                case ViewTab.Default:
                default:
                    return $"{GetAnchorTag(GetOrderViewUrl(origin, orderId), text)}";
                case ViewTab.Requisition:
                    return $"{GetAnchorTag(GetOrderViewUrl(origin, orderId), text)}?tab=requisition";
                case ViewTab.Complaint:
                    return $"{GetAnchorTag(GetOrderViewUrl(origin, orderId), text)}?tab=complaint";
            }
        }

        public static string GetRequestViewAnchorTag(string origin, int requestId, string text, ViewTab tab = ViewTab.Default)
        {
            switch (tab)
            {
                case ViewTab.Default:
                default:
                    return $"{GetAnchorTag(GetRequestViewUrl(origin, requestId), text)}";
                case ViewTab.Requisition:
                    return $"{GetAnchorTag(GetRequestViewUrl(origin, requestId), text)}?tab=requisition";
                case ViewTab.Complaint:
                    return $"{GetAnchorTag(GetRequestViewUrl(origin, requestId), text)}?tab=complaint";
            }
        }

        public static string GetAnchorTag(string href)
        {
            return GetAnchorTag(href, href);
        }

        public static string GetAnchorTag(string href, string text)
        {
            return $"<a href=\"{href}\">{text}</a>";
        }

        public static string GetAnchorTag(string href, string text, string classes)
        {
            return $"<a href=\"{href}\" class=\"{classes}\">{text}</a>";
        }

        public static string GetButtonDefaultLargeTag(string href, string text)
        {
            return GetAnchorTag(href, text, "btn btn-default btn-large");
        }

        #endregion

        public static string ToHtmlBreak(string text)
        {
            return text.Replace("\n", "<br />");
        }
    }
}
