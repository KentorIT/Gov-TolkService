namespace Tolk.Web.Models
{
    public class UserMoveValidationModel
    {
        public int NoOfOpenOrders { get; set; }
        public bool OrganisationHasCentralOrderHandler { get; set; }
        public bool IsSoleUnitAdmin { get; set; }

        public bool WarnOpenOrders => NoOfOpenOrders > 0 && OrganisationHasCentralOrderHandler;
        public bool StopOpenOrders => NoOfOpenOrders > 0 && !OrganisationHasCentralOrderHandler;
        public bool DenyMove => IsSoleUnitAdmin || StopOpenOrders;
    }
}
