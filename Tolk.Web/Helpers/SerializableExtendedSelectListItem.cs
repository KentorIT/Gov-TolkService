using System;

namespace Tolk.Web.Helpers
{
    [Serializable]
    public class SerializableExtendedSelectListItem
    {
        public string AdditionalDataAttribute { get; set; }
        public bool Disabled { get; set; }
        //TODO: Make this serializable too
        //public SelectListGroup Group { get; set; }
        public bool Selected { get; set; }
        public string Text { get; set; }
        public string Value { get; set; }

    }
}
