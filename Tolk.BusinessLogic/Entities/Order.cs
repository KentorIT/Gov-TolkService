using System;
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
        #region base information

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

        [Required]
        public int OrderNumber { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        //FK to AspNetUser
        public int CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public AspNetUser CreatedByUser { get; set; }


        private OrderStatus _status;

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

        public AssignmentType AssignentType { get; set; }

        //TODO: Make Enum and fk
        public CompetenceAndSpecialistLevel RequiredCompetenceLevel { get; set; }
        //Same as above
        //TODO: Fix this, temporarily reverted to an int due to EF Core issue with nullable enum.
        public int? RequestedCompetenceLevel { get; set; }

        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset EndDateTime { get; set; }

        public bool AllowMoreThanTwoHoursTravelTime { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public int? ImpersonatingCreator { get; set; }

        [ForeignKey(nameof(ImpersonatingCreator))]
        public AspNetUser CreatedByImpersonator { get; set; }

        #endregion


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

        public Request CreateRequest(IQueryable<Ranking> rankings, DateTimeOffset newRequestExpiry)
        {
            // TODO Need to get/understand rules for how close to assignment a request can be allowed.
            if(newRequestExpiry.AddHours(1) > StartDateTime)
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
    }
}
