using System;
using System.Collections.Generic;
using System.Text;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Tests.Helpers
{
    public static class MockHelper
    {
        internal static Language[] MockLanguages()
        {
            return new[]
            {
                new Language { LanguageId = 0, Name = "English" },
                new Language { LanguageId = 1, Name = "German" },
                new Language { LanguageId = 2, Name = "French" },
                new Language { LanguageId = 3, Name = "Chinese" },
            };
        }
    }
}
