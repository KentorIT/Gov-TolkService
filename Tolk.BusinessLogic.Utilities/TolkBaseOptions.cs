using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Utilities
{
    public class TolkBaseOptions
    {
        public int MonthsToApproveComplaints { get; set; }

        public Environment Env { get; set; } = new Environment { Name = string.Empty, Background = "background: rgba(255, 0, 0, 0.5)", Foreground = "color: #f1f1f1" };
        public TellusApi Tellus { get; set; }
        public SupportSettings Support { get; set; }

        public SmtpSettings Smtp { get; set; }

        public class TellusApi
        {
            public bool IsActivated { get; set; }
            public bool IsLanguagesCompetenceActivated { get; set; }
            public string Uri { get; set; }
            public string LanguagesUri { get; set; }
            public string LanguagesCompetenceInfoUri { get; set; }
            public string UnusedIsoCodes { get; set; }
            public IEnumerable<string> UnusedIsoCodesList { get
                {
                    return UnusedIsoCodes.Split(';');
                }
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

        public class SupportSettings
        {
            public string FirstLineEmail { get; set; }
            public string SecondLineEmail { get; set; }
            public string UserAccountEmail { get; set; }
            public string SupportPhone { get; set; }
        }

        public class Environment
        {
            public string Name { get; set; }
            public string Background { get; set; }
            public string Foreground { get; set; }
            public string DisplayName => string.IsNullOrWhiteSpace(Name) ? string.Empty : $"({Name})";
        }

        public class IsoCode
        {
            public string Value { get; set; }
        }
    }
}
