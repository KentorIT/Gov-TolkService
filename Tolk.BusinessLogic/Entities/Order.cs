using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Entities
{
    public class Order
    {
        private OrderStatus _status;

        #region base information

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

        [Required]
        public string OrderNumber { get; set; }

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
                if (value == OrderStatus.ResponseAccepted &&
                    (!( Status == OrderStatus.RequestResponded || 
                        Status == OrderStatus.RequestRespondedNewInterpreter || 
                       (Status == OrderStatus.Requested && ReplacingOrderId.HasValue)) || 
                    Requests.Count(r => r.Status == RequestStatus.Approved) != 1))
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

        public int? ReplacingOrderId { get; set; }

        [ForeignKey(nameof(ReplacingOrderId))]
        [InverseProperty(nameof(ReplacedByOrder))]
        public Order ReplacingOrder { get; set; }

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

        public int? LanguageId { get; set; }

        [ForeignKey(nameof(LanguageId))]
        public Language Language { get; set; }

        [MaxLength(255)]
        public string OtherLanguage { get; set; }

        public AssignmentType AssignentType { get; set; }

        public OffSiteAssignmentType? OffSiteAssignmentType { get; set; }

        [MaxLength(255)]
        public string OffSiteContactInformation { get; set; }

        public bool SpecificCompetenceLevelRequired { get; set; }

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

        public List<OrderCompetenceRequirement> CompetenceRequirements { get; set; }

        public List<OrderInterpreterLocation> InterpreterLocations { get; set; }

        public List<OrderPriceRow> PriceRows { get; set; }

        [InverseProperty(nameof(ReplacingOrder))]
        public Order ReplacedByOrder { get; set; }

        #endregion

        #region methods

        public Request CreateRequest(IQueryable<Ranking> rankings, DateTimeOffset newRequestExpiry)
        {
            // TODO Need to get/understand rules for how close to assignment a request can be allowed.
            if (newRequestExpiry.AddHours(1) > StartAt)
            {
                // For now, require response time to end at least one hour before start of assignment.
                Status = OrderStatus.NoBrokerAcceptedOrder;
                return null;
            }

            var brokersWithRequest = Requests.Select(r => r.Ranking.BrokerId);

            var ranking = ReplacingOrderId.HasValue && brokersWithRequest.Any() ? null :
                rankings.Where(r => !brokersWithRequest.Contains(r.BrokerId)).OrderBy(r => r.RankingId).FirstOrDefault();

            if (ranking == null)
            {
                // Rejected by all brokers, close the order
                Status = OrderStatus.NoBrokerAcceptedOrder;
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

        public void MakeCopy(Order order, int? originalRequestId, int? replacementRequestId)
        {
            order.AllowMoreThanTwoHoursTravelTime = AllowMoreThanTwoHoursTravelTime;
            order.AssignentType = AssignentType;
            order.CustomerOrganisation = CustomerOrganisation;
            order.InterpreterLocations = InterpreterLocations.Select(i => new OrderInterpreterLocation
            {
                InterpreterLocation = i.InterpreterLocation,
                Rank = i.Rank
            }).ToList();
            order.Language = Language;
            order.OffSiteAssignmentType = OffSiteAssignmentType;
            order.OtherLanguage = OtherLanguage;
            order.Region = Region;
            order.RequiredCompetenceLevel = RequiredCompetenceLevel;
            order.Requirements = Requirements.Select(r => new OrderRequirement
            {
                Description = r.Description,
                IsRequired = r.IsRequired,
                RequirementType = r.RequirementType,
                RequirementAnswers = r.RequirementAnswers
                    .Where(a => a.RequestId == originalRequestId)
                    .Select(a => new OrderRequirementRequestAnswer
                    {
                        Answer = a.Answer,
                        CanSatisfyRequirement = a.CanSatisfyRequirement,
                        RequestId = replacementRequestId.Value
                    }).ToList(),
            }).ToList();
        }

        #endregion
    }
}
