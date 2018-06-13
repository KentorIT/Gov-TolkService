using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Helpers
{
    public class TolkOptions
    {
        public string PublicOrigin { get; set; }

        public string SupportEmail { get; set; }

        public void Validate()
        {
            if (string.IsNullOrEmpty(PublicOrigin)
                || !Uri.TryCreate(PublicOrigin, UriKind.Absolute, out Uri url)
                    || url.Scheme != "https")
            {
                throw new InvalidOperationException($"Invalid configuration of PublicOrigin: {PublicOrigin}");
            }

            if(string.IsNullOrEmpty(SupportEmail))
            {
                throw new InvalidOperationException($"Support e-mail config missing.");
            }
        }
    }
}
