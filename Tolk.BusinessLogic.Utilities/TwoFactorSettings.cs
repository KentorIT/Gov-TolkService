namespace Tolk.BusinessLogic.Utilities
{
    public class TwoFactorSettings
    {
        public bool Enabled { get; set; }
        public int ValidationCodeMinutesValidity { get; set; }
        public int LockoutMinutesValidity { get; set; }
        public int TwoFactorDaysValidity { get; set; }
        public int CodeLength { get; set; }
        public int MaxNumberOfTries { get; set; }

        public string Salt { get; set; }

        public string Description => $"Använd tvåfaktor: {Enabled.ToSwedishString()}\n\tAntal dagar mellan tvåfaktor: {TwoFactorDaysValidity.ToSwedishString()}\n\tAntal minuter valideringskod är valid: {ValidationCodeMinutesValidity.ToSwedishString()}\n\tAntal minuter lockout gäller: {LockoutMinutesValidity.ToSwedishString()}\n\tMax antal misslyckade försök innan lockout: {MaxNumberOfTries.ToSwedishString()}\n\tAntal siffror i kod: {CodeLength.ToSwedishString()}";
    }
}
