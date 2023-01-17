namespace Tolk.BusinessLogic.Models
{
    public class EmailTextsModel
    {
        readonly static string NoReply = "Detta e-postmeddelande går inte att svara på.";
        readonly static string HandledBy = $"Detta ärende hanteras i {Constants.SystemName}.";

        public string SenderPrepend { get; set; }

        public string Subject { get; set; }

        public string BodyPlain { get; set; }

        public string BodyHtml { get; set; }

        public string FrameworkAgreementNumber { get; set; }

        public bool IsBrokerEmail { get; set; }
        public bool AddContractInfo { get; set; }

        public string ContractInfo => $"Avrop från ramavtal för tolkförmedlingstjänster {FrameworkAgreementNumber}";

        public string FormattedSubject => SenderPrepend + Subject;
        public string FormattedBodyPlain => $"{BodyPlain}\n\n{NoReply}" + (IsBrokerEmail ? $"\n\n{HandledBy}" : string.Empty) + (AddContractInfo ? $"\n\n{ContractInfo}" : string.Empty);
        public string FormattedBodyHtml => $"{BodyHtml}<br/><br/>{NoReply}" + (IsBrokerEmail ? $"<br/><br/>{HandledBy}" : string.Empty) + (AddContractInfo ? $"<br/><br/>{ContractInfo}" : string.Empty);

    }
}
