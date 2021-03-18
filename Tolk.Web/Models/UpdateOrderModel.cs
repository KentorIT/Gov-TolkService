using AutoMapper;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;
using Tolk.Web.Helpers;

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

        [Display(Name = "Gatuadress", Description = "Ange tydlig gatuadress så att tolken hittar. Mer information kan anges i fältet för övrig information om uppdraget.")]
        [ClientRequired]
        [SubItem]
        [StringLength(100)]
        public string LocationStreet { get; set; }

        [Display(Name = "Ort")]
        [SubItem]
        [StringLength(100)]
        public string LocationCity { get; set; }



        [Display(Name = "Kontaktinformation för tolktillfället", Description = "Distans per video: Ange vilket system som ska användas för videomötet. Mer information om anslutning etc. kan anges i fältet för övrig information om uppdraget. Distans per telefon: Ange vilket telefonnummer tolken ska ringa upp på eller om ni istället själva vill ringa upp tolken.")]
        [ClientRequired]
        [StringLength(255)]
        [SubItem]
        public string OffSiteContactInformation { get; set; }

        [Display(Name = "Rätt att granska rekvisition", Description = "Välj vid behov en annan person som skall ges rätt att granska rekvisition, t ex person som deltar vid tolktillfället. Denna uppgift kan du även komplettera eller ändra senare.")]
        public int? ContactPersonId { get; set; }


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
                IsCreatorInterpreterUser = order.CreatorIsInterpreterUser,
                ContactPersonId = order.ContactPersonId
            };
        }
    }
}
