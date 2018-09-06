using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Tolk.BusinessLogic.Data;

namespace Tolk.BusinessLogic.Tests.TestHelpers
{
    public class TolkDbContextHelper
    {
        public static TolkDbContext CreateTolkDbContext(string databaseName = "empty")
        {
            var options = new DbContextOptionsBuilder<TolkDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            return new TolkDbContext(options);
        }
    }
}
