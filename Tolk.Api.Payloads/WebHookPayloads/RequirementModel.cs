namespace Tolk.Api.Payloads.WebHookPayloads
{
    public class RequirementModel
    {
        public int RequirementId { get; set; }

        public string Description { get; set; }

        public string RequirementType { get; set; }

        public bool IsRequired { get; set; }
    }
}


