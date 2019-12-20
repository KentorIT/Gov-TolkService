namespace Tolk.BusinessLogic.Helpers
{
    public class TellusLanguageModel
    {
        public string Id { get; set; }
        public string Value { get; set; }
        public bool ExistsInSystemWithoutTellusConnection { get; set; }
        public bool InactiveInSystem { get; set; }

        public string Description =>
            !ExistsInSystemWithoutTellusConnection && !InactiveInSystem ? "Nej" :
                $"{(ExistsInSystemWithoutTellusConnection ? "Utan koppling" : string.Empty)} {(InactiveInSystem ? "inaktivt" : string.Empty)}";
    }
}
