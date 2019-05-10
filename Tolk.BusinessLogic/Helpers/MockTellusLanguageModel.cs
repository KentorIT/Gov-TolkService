namespace Tolk.BusinessLogic.Helpers
{
    public class MockTellusLanguageModel : TellusLanguageModel
    {
        public bool AllwaysAdd { get; set; } = false;
        public bool AddOnTest { get; set; } = false;
        public bool RemoveOnTest { get; set; } = false;
    }
}
