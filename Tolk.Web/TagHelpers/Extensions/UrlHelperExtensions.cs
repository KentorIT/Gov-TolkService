using Tolk.Web.Controllers;

namespace Microsoft.AspNetCore.Mvc
{
    public static class UrlHelperExtensions
    {
        public static string ResetPasswordCallbackLink(this IUrlHelper urlHelper, string userId, string code)
        {
            return urlHelper.Action(
                action: nameof(AccountController.ResetPassword),
                controller: "Account",
                values: new { userId, code },
                protocol: "https");
        }

        public static string ChangeEmailCallbackLink(this IUrlHelper urlHelper, string userId, string code)
        {
            return urlHelper.Action(
                action: nameof(AccountController.ChangeEmailCallback),
                controller: "Account",
                values: new { userId, code },
                protocol: "https");
        }
    }
}
