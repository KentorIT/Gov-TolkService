﻿using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;


namespace Tolk.Web.Models
{
    public class EmailFilterModel
    {
        [Display(Name = "Sök på text i mottagare")]
        [Placeholder("Ange del av mottagare")]
        public string Receipent { get; set; }

        [Display(Name = "Sök på text i ämne")]
        [Placeholder("Ange del av ämne")]
        public string Subject { get; set; }

        [Display(Name = "Skickat")]
        public TrueFalse? IsSent{ get; set; }

        [Display(Name = "Skapat datum")]
        public DateRange DateCreated { get; set; }

        [NoDisplayName]
        public string FilterMessage { get; set; }

        public bool HasActiveFilters => IsSent.HasValue || !string.IsNullOrWhiteSpace(Receipent) || DateCreated?.Start != null || DateCreated?.End != null;

        internal IQueryable<EmailListItemModel> Apply(IQueryable<EmailListItemModel> emails)
        {
            emails = !string.IsNullOrWhiteSpace(Receipent) ? emails.Where(e => e.Recipient.Contains(Receipent)) : emails;
            emails = !string.IsNullOrWhiteSpace(Subject) ? emails.Where(e => e.Subject.Contains(Subject)) : emails;
            emails = IsSent.HasValue ? emails.Where(e => e.SentAt.HasValue == (IsSent == TrueFalse.Yes)) : emails;
            emails = DateCreated?.Start != null ? emails.Where(o => o.CreatedAt.Date >= DateCreated.Start) : emails;
            emails = DateCreated?.End != null ? emails.Where(o => o.CreatedAt.Date <= DateCreated.End) : emails;
            return emails;
        }
    }
}