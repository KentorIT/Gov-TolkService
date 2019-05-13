﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Utilities
{
    public class TolkBaseOptions
    {
        public int MonthsToApproveComplaints { get; set; }

        public Environment Env { get; set; } = new Environment { Name = string.Empty, Background = "background: rgba(255, 0, 0, 0.5)", Foreground = "color: #f1f1f1" };
        public TellusApi Tellus { get; set; }
        public string SupportEmail { get; set; }

        public class TellusApi
        {
            public bool IsActivated { get; set; }
            public string Uri { get; set; }
            public string LanguagesUri { get; set; }
            public string UnusedIsoCodes { get; set; }
            public IEnumerable<string> UnusedIsoCodesList { get
                {
                    return UnusedIsoCodes.Split(';');
                }
            }
        }

        public class Environment
        {
            public string Name { get; set; }
            public string Background { get; set; }
            public string Foreground { get; set; }
        }
        public class IsoCode
        {
            public string Value { get; set; }
        }
    }
}
