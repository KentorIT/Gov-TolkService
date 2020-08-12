using Microsoft.AspNetCore.Authentication;
using System;

namespace Tolk.Web.Api.Authorization
{
    public static class CustomAuthExtensions
    {
        public static AuthenticationBuilder AddCustomAuth(this AuthenticationBuilder builder, Action<CustomAuthOptions> configureOptions)
        {
            return builder?.AddScheme<CustomAuthOptions, CustomAuthHandler>(CustomAuthHandler.SchemeName, "Custom Auth", configureOptions);
        }
    }
}

