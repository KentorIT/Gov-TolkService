using System;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Attributes;

namespace Tolk.Web.Models
{
    public class WebHookListItemModel
    {
        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(OutboundWebHookCallId), Visible = false)]
        public int OutboundWebHookCallId { get; set; }
        [ColumnDefinitions(Index = 1, Name = nameof(CreatedAt), SortOnWebServer = false, Title = "Skapad")]
        public string CreatedAt { get; set; }
        [ColumnDefinitions(Index = 2, Name = nameof(DeliveredAt), SortOnWebServer = false, Title = "Skickad")]
        public string DeliveredAt { get; set; }
        [ColumnDefinitions(Index = 3, Name = nameof(NotificationType), Title = "Notifieringstyp")]
        public string NotificationType { get; set; }
        [Display(Name = "Misslyckade försök")]
        [ColumnDefinitions(Index = 4, Name = nameof(FailedTries), Title = "Misslyckade försök")]
        public int FailedTries { get; set; }
        [ColumnDefinitions(Index = 5, Name = nameof(HasBeenResent), Title = "Är omskickad")]
        public string HasBeenResent { get; set; }
        [ColumnDefinitions(Index = 6, Name = nameof(BrokerName), Title = "Förmedling")]
        public string BrokerName { get; set; }
        [ColumnDefinitions(IsLeftCssClassName = true, Name = nameof(ListColor), Visible = false)]
        public string ListColor { get; set; }
    }
}
