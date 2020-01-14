using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;
using AutoMapper;

namespace Tolk.Web.Models
{
    [AutoMap(typeof(OrderModel))]
    public class UpdateOrderModel : OrderModel
    {

        [Display(Name = "Inställelsesätt (enligt svar)")]
        [Required]
        public InterpreterLocation SelectedInterpreterLocation { get; set; }

        [Display(Name = "Gatuadress")]
        [ClientRequired]
        [SubItem]
        [StringLength(100)]
        public string LocationStreet { get; set; }

        [Display(Name = "Ort")]
        [SubItem]
        [StringLength(100)]
        public string LocationCity { get; set; }

        [Display(Name = "Kontaktinformation för tolktillfället", Description = "Ex. telefonnummer eller namn relevant för tillfället")]
        [ClientRequired]
        [StringLength(255)]
        [SubItem]
        public string OffSiteContactInformation { get; set; }

        internal bool IsOrderUpdated(Order order)
        {
            var interpreterlocation = order.InterpreterLocations.Where(i => i.InterpreterLocation == SelectedInterpreterLocation).Single();
            var offSitePhoneOrVideo = interpreterlocation.InterpreterLocation == InterpreterLocation.OffSitePhone || interpreterlocation.InterpreterLocation == InterpreterLocation.OffSitePhone;

            return
                !(order.InvoiceReference == InvoiceReference &&
                order.CustomerReferenceNumber == CustomerReferenceNumber &&
                order.UnitName == UnitName &&
                order.Description == Description &&
                ((offSitePhoneOrVideo && interpreterlocation.OffSiteContactInformation == OffSiteContactInformation) ||
                (!offSitePhoneOrVideo && interpreterlocation.Street == LocationStreet)));

        }
    }
}
