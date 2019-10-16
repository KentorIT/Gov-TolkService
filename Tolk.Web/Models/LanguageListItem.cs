namespace Tolk.Web.Models
{
    public class LanguageListItem
    {
        public string Name { get; set; }

        public string ISO639Code { get; set; }

        public string TellusName { get; set; }

        public bool HasLegal { get; set; }

        public bool HasHealthcare { get; set; }

        public bool HasAuthorized { get; set; }

        public bool HasEducated { get; set; }

    }
}
