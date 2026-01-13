namespace Tolk.BusinessLogic.Models.TwoFactor
{
    public enum TwoFactorState
    {
        NeedTwoFactorEmail,
        Awaiting,
        Confirmed,
        LockedOut
    }
}