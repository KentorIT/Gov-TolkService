using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Authorization;
using Tolk.Web.Helpers;

namespace Tolk.Web.Services
{
    public class HelpLinkService
    {
        public string BaseAddress { get; set; }

        public string PageName { get; set; }

        private readonly IHttpContextAccessor _httpContextAccessor;

        public HelpLinkService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            BaseAddress = configuration != null ? configuration["PublicOrigin"] + "/docs/" : string.Empty;
        }


        public Uri GenerateUrl(string anchorpoint)
        {
            return ManualForUserGroup.AsUri();
        }

        private string ManualForUserGroup
        {
            get
            {
                var currentUser = _httpContextAccessor.HttpContext.User;
                var customerId = currentUser.TryGetCustomerOrganisationId();
                var brokerId = currentUser.TryGetBrokerId();

                if (currentUser.IsInRole(Roles.ApplicationAdministrator))
                {
                    return ApplicationAdminManual;
                }
                else if (currentUser.IsInRole(Roles.SystemAdministrator))
                {
                    return SystemAdminManual;
                }
                else if (brokerId.HasValue)
                {
                    return BrokerManual;
                }
                else if (customerId.HasValue)
                    return CustomerManual;
                else
                    return BaseAddress;
            }
        }

        public static HtmlString Anchor(string anchorpoint) => new(string.Empty);

        public HtmlString HeaderAnchor(string anchorpoint = null) => new($"<a href=\"{GenerateUrl(anchorpoint)}\" aria-label=\"Hjälp från manual\" target=\"_blank\" class=\"pull-right\"><span class=\"glyphicon glyphicon-question-sign\"></span></a>");

        public static HtmlString ButtonHelpIcon(string pageName) => new(string.Empty);
  

        public string BrokerManual => $"{BaseAddress}Formedling_anvandarmanual_8294712.pdf";

        public string CustomerManual => $"{BaseAddress}Myndighet_anvandarmanual_8237894.pdf";

        public string SystemAdminManual => $"{BaseAddress}Systemadmin_anvandarmanual_8625683.pdf";

        public string ApplicationAdminManual => $"{BaseAddress}Appadmin_anvandarmanual_83767231.pdf";
    }
}

