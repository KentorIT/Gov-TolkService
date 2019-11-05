using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class RequestGroupBaseModel
    {
        public int RequestGroupId { get; set; }

        [Display(Name = "Status på förfrågan")]
        public RequestStatus Status { get; set; }

        public int BrokerId { get; set; }

        [Display(Name = "Sammanhållet BokningsID")]
        public string OrderGroupNumber { get; set; }

        public string ViewedByUser { get; set; } = string.Empty;

        [Display(Name = "Bokning skapad")]
        public DateTimeOffset CreatedAt { get; set; }
    }
}
