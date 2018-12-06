namespace Tolk.Api.Payloads.ApiPayloads
{
    public class RequirementAnswerModel
    {
        public int RequirementId { get; set; }

        public string Answer { get; set; }

        public bool CanMeetRequirement { get; set; }
    }
}


