using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentDataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;

namespace Tolk.Web.Models
{
    public class OrderModel
    {
        [Display(Name = "Län")]
        [Required]
        public int RegionId { get; set; }

        [Display(Name = "Språk")]
        [Required]
        public int Language { get; set; }

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
        public int AssignentType { get; set; }

        [Display(Name = "Ert referensnummer", Description = "Extra fält för att koppla till ett ärendenummer i er verksamhet")]
        public string CustomerReferenceNumber { get; set; }

        [Display(Name = "Utbildningsnivå")]
        [Required]
        public int RequiredCompetenceLevel { get; set; }

        [Display(Name = "Accepterar mer än två timmar restidskostnad")]
        public bool AllowMoreThanTwoHoursTravelTime { get; set; }

        #region methods

        public Order Save(TolkDbContext dbContext, string createdBy, int customerOrganisationId)
        {
            Order order = new Order
            {
                //Hardcodes
                RequiredInterpreterLocation = 1,
                Status = 1,
                //end hardcodes
                CustomerOrganisationId = customerOrganisationId,
                CreatedBy = createdBy,
                CreatedDate = DateTime.Now,
                LanguageId = Language,
                AllowMoreThanTwoHoursTravelTime = AllowMoreThanTwoHoursTravelTime,
                AssignentType = AssignentType,
                RegionId = RegionId,
                CustomerReferenceNumber = CustomerReferenceNumber,
                StartDateTime = StartDateTime,
                EndDateTime = EndDateTime,
                Description = Description,
                UnitName = UnitName,
                Street = LocationStreet,
                ZipCode = LocationZipCode,
                City = LocationCity,
                RequiredCompetenceLevel = RequiredCompetenceLevel,
            };
            return Order.Save(dbContext, order);
        }
        #endregion
    }
}
