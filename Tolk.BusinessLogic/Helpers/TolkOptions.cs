﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;

namespace Tolk.BusinessLogic.Helpers
{
    public class TolkOptions : TolkBaseOptions
    {
        public string PublicOrigin { get; set; }

        public string AllowedFileExtensions { get; set; }

        public bool EnableTimeTravel { get; set; }

        public bool EnableOrderGroups { get; set; }
        public bool EnableMockInvoice { get; set; }

        public long CombinedMaxSizeAttachments { get; set; }

        public bool EnableRegisterUser { get; set; }
        public int HourToRunFrameworkAgreementValidation { get; set; }
        public int HourToRunDailyJobs { get; set; }
        public bool EnableOrderUpdate { get; set; }

        public bool RunEntityScheduler { get; set; } = true;
        public bool UseStoredProceduresForReports { get; set; } = true;

        public PeppolSettings Peppol { get; set; }

        public FlexibleOrderSettings FlexibleOrder { get; set; }

        public TolkConnectionStrings ConnectionStrings { get; set; }

        public IEnumerable<NotificationType> ExcludedNotificationTypesForCustomerAsEnums => ExcludedNotificationTypesForCustomer.Split(",").Select(i => (NotificationType)int.Parse(i));

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

        public TolkDbContext GetContext()
        {
            var ob = new DbContextOptionsBuilder<TolkDbContext>().UseSqlServer(ConnectionStrings.DBConnection);
            return new TolkDbContext(ob.Options);
        }

    }
}
