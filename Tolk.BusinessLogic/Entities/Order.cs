﻿using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Tolk.BusinessLogic.Enums;
using Microsoft.Extensions.Internal;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.BusinessLogic.Entities
{
    public class Order
    {
        private OrderStatus _status;

        #region base information

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

        [Required]
        public int OrderNumber { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public int CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public AspNetUser CreatedByUser { get; set; }

        public OrderStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                if(value == OrderStatus.ResponseAccepted &&
                    (Status != OrderStatus.RequestResponded
                    || Requests.Count(r => r.Status == RequestStatus.Approved) != 1))
                {
                    throw new InvalidOperationException($"Order {OrderId} is in the wrong state to be set as accepted.");
                }

                _status = value;
            }
        }

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

        public int? ContactPersonId { get; set; }

        [ForeignKey(nameof(ContactPersonId))]
        public AspNetUser ContactPersonUser { get; set; }

        [MaxLength(100)]
        public string UnitName { get; set; }

        [MaxLength(100)]
        public string Street { get; set; }

        [MaxLength(100)]
        public string ZipCode { get; set; }

        [MaxLength(100)]
        public string City { get; set; }

        #endregion

        #region order information

        public int LanguageId { get; set; }

        [ForeignKey(nameof(LanguageId))]
        public Language Language { get; set; }

        public AssignmentType AssignentType { get; set; }

        public OffSiteAssignmentType? OffSiteAssignmentType { get; set; }

        [MaxLength(255)]
        public string OffSiteContactInformation { get; set; }

        public CompetenceAndSpecialistLevel RequiredCompetenceLevel { get; set; }

        public DateTimeOffset StartAt { get; set; }
         
        public DateTimeOffset EndAt { get; set; }

        public bool AllowMoreThanTwoHoursTravelTime { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public int? ImpersonatingCreator { get; set; }

        [ForeignKey(nameof(ImpersonatingCreator))]
        public AspNetUser CreatedByImpersonator { get; set; }

        #endregion

        #region navigation properties

        public string CompactAddress
        {
            get
            {
                return $"{UnitName}\n{Street}\n{ZipCode} {City}";
            }
        }

        public List<Request> Requests { get; set; }

        public List<OrderRequirement> Requirements { get; set; }

        public List<OrderInterpreterLocation> InterpreterLocations { get; set; }

        #endregion

        #region methods

        public Request CreateRequest(IQueryable<Ranking> rankings, DateTimeOffset newRequestExpiry)
        {
            // TODO Need to get/understand rules for how close to assignment a request can be allowed.
            if(newRequestExpiry.AddHours(1) > StartAt)
            {
                // For now, require response time to end at least one hour before start of assignment.
                return null;
            }

            if (Requests == null)
            {
                Requests = new List<Request>();
            }

            var brokersWithRequest = Requests.Select(r => r.Ranking.BrokerId);

            var ranking = rankings.Where(r => !brokersWithRequest.Contains(r.BrokerId))
                .OrderBy(r => r.RankingId).FirstOrDefault();

            if(ranking == null)
            {
                // TODO: Rejected by all brokers, what to do now?
                return null;
            }

            var request = new Request(ranking, newRequestExpiry);

            Requests.Add(request);

            return request;
        }

        public void DeliverRequisition()
        {
            if (Status != OrderStatus.ResponseAccepted && Status != OrderStatus.Delivered)
            {
                throw new InvalidOperationException($"Order {OrderId} is {Status}. Only Orders with Accepted request can be delivered");
            }

            Status = OrderStatus.Delivered;
        }

        #endregion
    }
}
