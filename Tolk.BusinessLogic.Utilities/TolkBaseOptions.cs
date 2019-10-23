namespace Tolk.BusinessLogic.Utilities
{
    public class TolkBaseOptions
    {
        public int MonthsToApproveComplaints { get; set; }
        public Environment Env { get; set; } = new Environment { Name = string.Empty, Background = "background: rgba(255, 0, 0, 0.5)", Foreground = "color: #f1f1f1" };
        public TellusApi Tellus { get; set; }
        public SupportSettings Support { get; set; }
        public SmtpSettings Smtp { get; set; }
        public StatusCheckerSettings StatusChecker { get; set; }
    }
}
