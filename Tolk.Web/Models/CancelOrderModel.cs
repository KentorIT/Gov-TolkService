namespace Tolk.Web.Models
{
    public class CancelOrderModel
    {
        public int OrderId { get; set; }
        public string CancelMessage { get; set; }
        public bool AddReplacementOrder { get; set; } = false;
    }
}
