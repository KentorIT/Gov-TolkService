﻿using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Tolk.BusinessLogic.Entities
{
    public class Order
    {
        #region base information

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

        [Required]
        public int OrderNumber { get; set; }

        public DateTime CreatedDate { get; set; }

        //FK to AspNetUser
        public string CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public AspNetUser CreatedByUser { get; set; }

        //TODO: Make Enum and fk
        public int Status { get; set; }

        //FK to CustomerOrganisation
        public int CustomerOrganisationId { get; set; }

        [ForeignKey(nameof(CustomerOrganisationId))]
        public CustomerOrganisation CustomerOrganisation { get; set; }

        public int RegionId { get; set; }

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; }

        #endregion

        #region customer information

        [MaxLength(100)]
        public string CustomerReferenceNumber { get; set; }

        [MaxLength(255)]
        public string OtherContactPerson { get; set; }
        [MaxLength(50)]
        public string OtherContactPhone { get; set; }
        [MaxLength(255)]
        public string OtherContactEmail { get; set; }

        [MaxLength(100)]
        public string UnitName { get; set; }

        [MaxLength(100)]
        public string Street { get; set; }

        [MaxLength(100)]
        public string ZipCode { get; set; }

        [MaxLength(100)]
        public string City { get; set; }

        [MaxLength(255)]
        public string OtherAddressInformation { get; set; }

        #endregion

        #region order information

        public int LanguageId { get; set; }

        [ForeignKey(nameof(LanguageId))]
        public Language Language { get; set; }

        //TODO: Make Enum and fk
        public int AssignentType { get; set; }

        //TODO: Make Enum and fk
        public int RequiredInterpreterLocation { get; set; }
        public int? RequestedInterpreterLocation { get; set; }

        //TODO: Make Enum and fk
        public int RequiredCompetenceLevel { get; set; }
        //Same as above
        public int? RequestedCompetenceLevel { get; set; }

        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset EndDateTime { get; set; }

        public bool AllowMoreThanTwoHoursTravelTime { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public string ImpersonatingCreator { get; set; }

        [ForeignKey(nameof(ImpersonatingCreator))]
        public AspNetUser CreatedByImpersonator { get; set; }

        #endregion

        public List<Request> Requests { get; set; }

        public List<OrderRequirement> Requirements { get; set; }

        public static Request CreateRequest(TolkDbContext dbContext, Order order, int rank = 1)
        {
            //Get Ranking from Region
            var now = DateTime.Now;
            var ranking = dbContext.Regions
                .Include(r => r.BrokerRegions)
                .ThenInclude(br => br.Ranking)
                .Single(r => r.RegionId == order.RegionId)
                .BrokerRegions
                .Single(br => br.Ranking.Rank == rank && br.Ranking.StartDate <= now && br.Ranking.EndDate >= now).Ranking;
            var request = new Request
            {
                RankingId = ranking.RankingId,
                Order = order,
            };
            dbContext.Requests.Add(request);
            return request;
        }

        /* remaining fields
(Antal tolkar (eventuellt, avvakta))
Särskilda kontaktuppgifter (om uppdraget avser Distanstolkning i anvisad lokal eller Distanstolkning (video el. telefon)
        */
    }
}
