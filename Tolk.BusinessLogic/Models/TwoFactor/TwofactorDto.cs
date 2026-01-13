using System;

namespace Tolk.BusinessLogic.Models.TwoFactor
{
    public class TwoFactorDto
    {
        public DateTimeOffset? TwoFactorExpiresAt { get; set; }
        public string ValidationCode { get; set; }
        public DateTimeOffset? ValidationCodeExpiresAt { get; set; }
        public DateTimeOffset? LockoutExpiresAt { get; set; }

        public int NumberOfAttempts { get; set; }

        internal TwoFactorState NeedTwoFactor(DateTimeOffset now)
        {
            if (LockoutExpiresAt.HasValue && LockoutExpiresAt.Value > now)
            {
                return TwoFactorState.LockedOut;
            }
            else if (TwoFactorExpiresAt.HasValue && TwoFactorExpiresAt.Value > now)
            {
                return TwoFactorState.Confirmed;
            }
            else if (TwoFactorExpiresAt.HasValue && TwoFactorExpiresAt.Value < now && !ValidationCodeExpiresAt.HasValue ||
                ValidationCodeExpiresAt.HasValue && ValidationCodeExpiresAt.Value < now)
            {
                return TwoFactorState.NeedTwoFactorEmail;
            }
            else if (ValidationCode != null && ValidationCodeExpiresAt.HasValue && ValidationCodeExpiresAt.Value > now)
            {
                return TwoFactorState.Awaiting;
            }
            return TwoFactorState.NeedTwoFactorEmail;
        }
    }
}