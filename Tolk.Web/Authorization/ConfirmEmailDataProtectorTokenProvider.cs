using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tolk.Web.Authorization
{
    public class ConfirmEmailDataProtectorTokenProvider<TUser> : DataProtectorTokenProvider<TUser> where TUser : class
    {
        public ConfirmEmailDataProtectorTokenProvider(
                    IDataProtectionProvider dataProtectionProvider,
                    IOptions<ConfirmEmailDataProtectionTokenProviderOptions> options,
                    ILogger<ConfirmEmailDataProtectorTokenProvider<TUser>> logger)
                    : base(dataProtectionProvider, options, logger)
        {
        }
    }

    public class ConfirmEmailDataProtectionTokenProviderOptions : DataProtectionTokenProviderOptions { }
}
