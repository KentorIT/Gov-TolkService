using System;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Helpers
{
    public class TolkOptions: TolkBaseOptions
    {
        public string PublicOrigin { get; set; }

        public string AllowedFileExtensions { get; set; }

        public bool EnableTimeTravel { get; set; }

        public bool EnableOrderGroups { get; set; }

        public bool EnableDefaultSettings { get; set; }

        public bool RoundPriceDecimals { get; set; }

        public long CombinedMaxSizeAttachments { get; set; }

        public bool EnableRegisterUser { get; set; }

        public void Validate()
        {
            if (string.IsNullOrEmpty(PublicOrigin)
                || !Uri.TryCreate(PublicOrigin, UriKind.Absolute, out Uri url)
                    || url.Scheme != "https")
            {
                throw new InvalidOperationException($"Invalid configuration of PublicOrigin: {PublicOrigin}");
            }

            if (string.IsNullOrEmpty(Support.FirstLineEmail))
            {
                throw new InvalidOperationException($"First line support e-mail config missing.");
            }
        }

        public class SideBarBox
        {
            public SideBarBox() { }
            public SideBarBox(string title, string message)
            {
                Title = title;
                Message = message;
            }
            public string Title { get; set; }
            public string Message { get; set; }
        }
    }
}
