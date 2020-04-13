using System.ComponentModel.DataAnnotations;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;
using AutoMapper;
using System;
using System.Collections.Generic;

namespace Tolk.Web.Models
{
    [AutoMap(typeof(OrderModel))]
    public class UpdateOrderModel : OrderBaseModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; }

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

        [Display(Name = "Rätt att granska rekvisition", Description = "Välj vid behov en annan person som skall ges rätt att granska rekvisition, t ex person som deltar vid tolktillfället. Denna uppgift kan du även komplettera eller ändra senare.")]
        public int? ContactPersonId { get; set; }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Used in razor view")]
        public List<FileModel> Files { get; set; }

        public Guid? FileGroupKey { get; set; }

        public long? CombinedMaxSizeAttachments { get; set; }

        [Display(Name = "Uppdragstyp")]
        public AssignmentType AssignmentType { get; set; }

        [Display(Name = "Datum och tid", Description = "Datum och tid för tolkuppdraget")]
        public virtual TimeRange TimeRange { get; set; }


        internal bool IsOrderUpdated(Order order, OrderInterpreterLocation selectedInterpreterLocation)
        {
            var offSitePhoneOrVideo = selectedInterpreterLocation.InterpreterLocation == InterpreterLocation.OffSitePhone || selectedInterpreterLocation.InterpreterLocation == InterpreterLocation.OffSitePhone;

            return
                !(order.InvoiceReference == InvoiceReference &&
                order.CustomerReferenceNumber == CustomerReferenceNumber &&
                order.UnitName == UnitName &&
                order.Description == Description &&
                ((offSitePhoneOrVideo && selectedInterpreterLocation.OffSiteContactInformation == OffSiteContactInformation) ||
                (!offSitePhoneOrVideo && selectedInterpreterLocation.Street == LocationStreet)));

        }

        internal static UpdateOrderModel GetModelFromOrder(Order order)
        {
            return new UpdateOrderModel
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                AssignmentType = order.AssignmentType,
                RegionName = order.Region.Name,
                LanguageName = order.OtherLanguage ?? order.Language?.Name,
                TimeRange = new TimeRange
                {
                    StartDateTime = order.StartAt,
                    EndDateTime = order.EndAt
                },
                Description = order.Description,
                UnitName = order.UnitName,
                InvoiceReference = order.InvoiceReference,
                CustomerUnitName = order.CustomerUnit?.Name,
                CustomerReferenceNumber = order.CustomerReferenceNumber,
            };
        }
    }
}
