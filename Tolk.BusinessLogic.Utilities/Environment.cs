namespace Tolk.BusinessLogic.Utilities
{
    public class Environment
    {
        public string Name { get; set; }
        public string Background { get; set; }
        public string Foreground { get; set; }
        public string DisplayName => string.IsNullOrWhiteSpace(Name) ? string.Empty : $"({Name})";
        public string Description => $"Namn: {Name}\nBakgrundsfärg: {Background}\nTextfärg: {Foreground}";
    }
}
