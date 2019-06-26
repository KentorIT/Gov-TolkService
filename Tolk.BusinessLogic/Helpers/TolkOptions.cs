using System;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Helpers
{
    public class TolkOptions: TolkBaseOptions
    {
        public string PublicOrigin { get; set; }

        public string AllowedFileExtensions { get; set; }

        public SmtpSettings Smtp { get; set; }

        public bool EnableTimeTravel { get; set; }

        public bool EnableOrderGroups { get; set; }

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

            if (string.IsNullOrEmpty(SupportEmail))
            {
                throw new InvalidOperationException($"Support e-mail config missing.");
            }
        }

        public class SmtpSettings
        {
            public string Host { get; set; }

            public int Port { get; set; }

            public string UserName { get; set; }

            public string Password { get; set; }

            public string FromAddress { get; set; }
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
