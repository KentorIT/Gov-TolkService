using System.Collections.Generic;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Models
{
    public class StartViewModel
    {
        public IEnumerable<StartList> StartLists { get; set; }

        public string PageTitle { get; set; } = "Aktiva bokningar";

        public string Message { get; set; }
        public string ErrorMessage { get; set; }

        public IEnumerable<ConfirmationMessage> ConfirmationMessages { get; set; }

        public bool IsBroker { get; set; }

        public bool IsCustomer { get; set; }

        public IEnumerable<SystemMessage> SystemMessages { get; set; }

    }

}
