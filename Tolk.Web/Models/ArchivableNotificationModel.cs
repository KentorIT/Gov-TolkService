using DocumentFormat.OpenXml.Presentation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Models.Notification;

namespace Tolk.Web.Models
{
    public class ArchivableNotificationsModel : IModel
    {
        [Display(Name = "Förmedling")]
        [Required]
        public int? BrokerId { get; set; }

        [Display(Name = "Arkivera meddelanden äldre än")]
        [Required]
        public DateTime? ArchiveToDate { get; set; }

        public IEnumerable<NotificationDisplayModel> AllNotifications { get; set; }

        public bool IsApplicationAdministrator { get; set; }

    }
    public class ArchiveNotificationsModel
    {
        public int BrokerId { get; set; }

        public DateTime ArchiveToDate { get; set; }

        public IEnumerable<NotificationType> SelectedTypes { get; set; }

    }
}
