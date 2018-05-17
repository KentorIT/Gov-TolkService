using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class OrderModel
    {
        public int? OrderId { get; set; }

        [Display(Name = "Region")]
        [Required]
        public int RegionId { get; set; }

        [Display(Name = "Språk")]
        [Required]
        public int LanguageId { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Beskrivning")]
        [Required]
        public string Description { get; set; }

        [Display(Name = "Enhet/avdelning")]
        [Required]
        public string UnitName { get; set; }

        [Display(Name = "Adress")]
        [Required]
        public string LocationStreet { get; set; }

        [Display(Name = "Postnummer")]
        [Required]
        public string LocationZipCode { get; set; }

        [Display(Name = "Ort")]
        [Required]
        public string LocationCity { get; set; }

        [Display(Name = "Startdatum och tid")]
        public DateTimeOffset StartDateTime { get; set; }

        [Display(Name = "Slutdatum och tid")]
        public DateTimeOffset EndDateTime { get; set; }

        [Display(Name = "Typ av tolkuppdrag")]
        [Required]
        public AssignmentType AssignmentType { get; set; }

        [Display(Name = "Ert referensnummer", Description = "Extra fält för att koppla till ett ärendenummer i er verksamhet")]
        public string CustomerReferenceNumber { get; set; }

        [Display(Name = "Utbildningsnivå")]
        [Required]
        public CompetenceAndSpecialistLevel RequiredCompetenceLevel { get; set; }

        [Display(Name = "Accepterar mer än två timmar restidskostnad")]
        public bool AllowMoreThanTwoHoursTravelTime { get; set; }

        #region details

        [Display(Name = "Status")]
        public OrderStatus Status { get; set; }

        [Display(Name = "AvropsID")]
        public string OrderNumber { get; set; }

        [Display(Name = "Region")]
        public string RegionName { get; set; }

        [Display(Name = "Språk")]
        public string LanguageName { get; set; }

        [Display(Name = "Skapad")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Skapad av")]
        public string CreatedBy { get; set; }

        [Display(Name = "Kund")]
        public string CustomerName { get; set; }

        [Display(Name = "Förmedling")]
        public string BrokerName { get; set; }

        [Display(Name = "Beräknat pris inklusive förmedlingsavgift och ev. OB")]
        public decimal CalculatedPrice { get; set; }
        //Request information

        #endregion

        #region methods

        public Order UpdateOrder(Order order)
        {
            order.LanguageId = LanguageId;
            order.AllowMoreThanTwoHoursTravelTime = AllowMoreThanTwoHoursTravelTime;
            order.AssignentType = AssignmentType;
            order.RegionId = RegionId;
            order.CustomerReferenceNumber = CustomerReferenceNumber;
            order.StartDateTime = StartDateTime;
            order.EndDateTime = EndDateTime;
            order.Description = Description;
            order.UnitName = UnitName;
            order.Street = LocationStreet;
            order.ZipCode = LocationZipCode;
            order.City = LocationCity;
            order.RequiredCompetenceLevel = RequiredCompetenceLevel;
            return order;
        }

        public static OrderModel GetModelFromOrder(Order order)
        {
            return new OrderModel
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber.ToString(),
                CreatedBy = order.CreatedByUser?.NormalizedEmail,
                CreatedDate = order.CreatedDate,
                CustomerName = order.CustomerOrganisation?.Name,
                LanguageName = order.Language?.Name,
                RegionName = order.Region?.Name,
                LanguageId = order.LanguageId,
                AllowMoreThanTwoHoursTravelTime = order.AllowMoreThanTwoHoursTravelTime,
                AssignmentType = order.AssignentType,
                RegionId = order.RegionId,
                CustomerReferenceNumber = order.CustomerReferenceNumber,
                StartDateTime = order.StartDateTime,
                EndDateTime = order.EndDateTime,
                Description = order.Description,
                UnitName = order.UnitName,
                LocationStreet = order.Street,
                LocationZipCode = order.ZipCode,
                LocationCity = order.City,
                RequiredCompetenceLevel = order.RequiredCompetenceLevel,
                Status = order.Status
            };

        }

        #endregion
    }
}
