using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Services;

namespace Tolk.Web.Services
{
    public class LoginLinkTokenProvider
    {
        public class ValidationResult
        {
            public ValidationResult(bool expired, int userId)
            {
                Expired = expired;
                UserId = userId;
            }

            public bool Expired { get; }

            public int UserId { get; }
        }

        private const string _purpose = "loginlink";
        private readonly Encoding Encoding = new UTF8Encoding(false, true);

        private readonly IDataProtector _dataProtector;
        private readonly ISwedishClock _clock;

        public LoginLinkTokenProvider(
            IDataProtectionProvider dataProtectionProvider,
            ISwedishClock clock)
        {
            _dataProtector = dataProtectionProvider.CreateProtector(_purpose);
            _clock = clock;
        }


        public async Task<string> GenerateAsync(UserManager<AspNetUser> manager, AspNetUser user)
        {
            var ms = new MemoryStream();
            using (var writer = new BinaryWriter(ms, Encoding))
            {
                var creationTime = _clock.SwedenNow;
                writer.Write(creationTime.UtcTicks);
                writer.Write(user.Id);
                var stamp = await manager.GetSecurityStampAsync(user);
                writer.Write(stamp);
            }

            var protectedBytes = _dataProtector.Protect(ms.ToArray());

            return Convert.ToBase64String(protectedBytes);
        }

        public async Task<ValidationResult> ValidateAsync(string token, UserManager<AspNetUser> manager)
        {
            try
            {
                var unproctedData = _dataProtector.Unprotect(Convert.FromBase64String(token));
                var ms = new MemoryStream(unproctedData);
                using (var reader = new BinaryReader(ms))
                {
                    var creationTime = new DateTimeOffset(reader.ReadInt64(), TimeSpan.Zero);
                    var expired = creationTime.AddMinutes(15) < _clock.SwedenNow;
                    var userId = reader.ReadInt32();
                    var stamp = reader.ReadString();

                    var user = await manager.FindByIdAsync(userId.ToString());

                    if(stamp != await manager.GetSecurityStampAsync(user))
                    {
                        return null;
                    }

                    return new ValidationResult(expired, userId);
                }
            }catch
            {
                // Exceptions here means token format was wrong, just ignore and return null.
            }
            return null;
        }
    }
}
