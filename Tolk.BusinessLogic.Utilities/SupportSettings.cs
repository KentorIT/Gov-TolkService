namespace Tolk.BusinessLogic.Utilities
{
    public class SupportSettings
    {
        public string FirstLineEmail { get; set; }
        public string SecondLineEmail { get; set; }
        public string UserAccountEmail { get; set; }
        public string SupportPhone { get; set; }
        public bool ReportWebHookFailures { get; set; }
        public bool ReportPeppolMessageFailures { get; set; }
        public string Description => $"Rapportera webhook-fel: {(ReportWebHookFailures.ToSwedishString())}\nRapportera Peppol-fel: {ReportPeppolMessageFailures.ToSwedishString()}\nFirstline epost: {FirstLineEmail}\nSecondline epost: {SecondLineEmail}\nEpost för frågor om användarkonto: {UserAccountEmail}\nSupporttelefon: {SupportPhone}";
    }
}
