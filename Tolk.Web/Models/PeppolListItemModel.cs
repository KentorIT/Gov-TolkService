using System.ComponentModel.DataAnnotations;
using Tolk.Web.Attributes;

namespace Tolk.Web.Models
{
    public class PeppolListItemModel
    {
        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(OutboundPeppolMessageId), Visible = false)]
        public int OutboundPeppolMessageId { get; set; }
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
        [ColumnDefinitions(Index = 6, Name = nameof(CustomerName), Title = "Myndighet")]
        public string CustomerName { get; set; }
        [ColumnDefinitions(IsLeftCssClassName = true, Name = nameof(ListColor), Visible = false)]
        public string ListColor { get; set; }
    }
}
