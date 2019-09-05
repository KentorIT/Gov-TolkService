using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.BusinessLogic.Validation;

namespace Tolk.BusinessLogic.Entities
{
    public class Order
    {
        private Order() { }

        public Order(Order order)
            :this (order.CreatedByUser, order.CreatedByImpersonator, order.CustomerOrganisation, order.CreatedAt)
        {
            AllowExceedingTravelCost = order.AllowExceedingTravelCost;
            AssignentType = order.AssignentType;
            Language = order.Language;
            OtherLanguage = order.OtherLanguage;
            Region = order.Region;
            CustomerUnitId = order.CustomerUnitId;
            LanguageHasAuthorizedInterpreter = order.LanguageHasAuthorizedInterpreter;
            SpecificCompetenceLevelRequired = order.SpecificCompetenceLevelRequired;
            Group = order.Group;
            CompetenceRequirements = order.CompetenceRequirements.Select(r => new OrderCompetenceRequirement
            {
                CompetenceLevel = r.CompetenceLevel,
                Rank = r.Rank
            }).ToList();
            InterpreterLocations = order.InterpreterLocations.Select(l => new OrderInterpreterLocation
            {
                City = l.City,
                InterpreterLocation = l.InterpreterLocation,
                OffSiteContactInformation = l.OffSiteContactInformation,
                Rank = l.Rank,
                Street = l.Street
            }).ToList();
        }
        public Order(AspNetUser createdByUser, AspNetUser createdByImpersonator, CustomerOrganisation customerOrganisation, DateTimeOffset createdAt)
        {
            CreatedByUser = createdByUser;
            CreatedAt = createdAt;
            CustomerOrganisation = customerOrganisation;
            CreatedByImpersonator = createdByImpersonator;
            Status = OrderStatus.Requested;
            Requirements = new List<OrderRequirement>();
            InterpreterLocations = new List<OrderInterpreterLocation>();
            PriceRows = new List<OrderPriceRow>();
            Requests = new List<Request>();
            CompetenceRequirements = new List<OrderCompetenceRequirement>();
        }

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

        public int? OrderGroupId { get; set; }

        [ForeignKey(nameof(OrderGroupId))]
        public OrderGroup Group { get; set; }

        public OrderStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (value == OrderStatus.ResponseAccepted &&
                //NEED TO ADD A CHECK IF REQUESTED, AND THE ALLOW CHECK IS FALSE
                    (!((Status == OrderStatus.Requested && AllowExceedingTravelCost != Enums.AllowExceedingTravelCost.YesShouldBeApproved) ||
                        Status == OrderStatus.RequestResponded ||
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

        public int? CustomerUnitId { get; set; }

        [ForeignKey(nameof(CustomerUnitId))]
        public CustomerUnit CustomerUnit { get; set; }

        public int RegionId { get; set; }

        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; }

        public int? ReplacingOrderId { get; set; }

        [ForeignKey(nameof(ReplacingOrderId))]
        [InverseProperty(nameof(ReplacedByOrder))]
        public Order ReplacingOrder { get; set; }

        public List<OrderAttachment> Attachments { get; set; }

        public List<OrderContactPersonHistory> OrderContactPersonHistory { get; set; }

        #endregion

        #region customer information

        [MaxLength(100)]
        public string CustomerReferenceNumber { get; set; }

        public int? ContactPersonId { get; set; }

        [ForeignKey(nameof(ContactPersonId))]
        public AspNetUser ContactPersonUser { get; set; }

        [MaxLength(100)]
        public string UnitName { get; set; }

        #endregion

        #region order information

        public int? LanguageId { get; set; }

        [ForeignKey(nameof(LanguageId))]
        public Language Language { get; set; }

        [MaxLength(255)]
        public string OtherLanguage { get; set; }

        public bool LanguageHasAuthorizedInterpreter { get; set; }

        public AssignmentType AssignentType { get; set; }

        public bool SpecificCompetenceLevelRequired { get; set; }

        public DateTimeOffset StartAt { get; set; }

        private DateTimeOffset _endAt;

        public DateTimeOffset EndAt
        {
            get
            {
                return _endAt;
            }
            set
            {
                Validate.Ensure(value > StartAt, $"{nameof(EndAt)} cannot occur before {nameof(StartAt)}");
                _endAt = value;
            }
        }

        public AllowExceedingTravelCost? AllowExceedingTravelCost { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public int? ImpersonatingCreator { get; set; }

        [ForeignKey(nameof(ImpersonatingCreator))]
        public AspNetUser CreatedByImpersonator { get; set; }

        public int? IsExtraInterpreterForOrderId { get; set; }

        [ForeignKey(nameof(IsExtraInterpreterForOrderId))]
        public Order IsExtraInterpreterForOrder { get; set; }

        #endregion

        #region navigation properties

        public List<Request> Requests { get; set; }

        public List<OrderRequirement> Requirements { get; set; }

        public List<OrderCompetenceRequirement> CompetenceRequirements { get; set; }

        public List<OrderInterpreterLocation> InterpreterLocations { get; set; }

        public List<OrderPriceRow> PriceRows { get; set; }

        [InverseProperty(nameof(ReplacingOrder))]
        public Order ReplacedByOrder { get; set; }

        public CompetenceLevel PriceCalculatedFromCompetenceLevel
        {
            get
            {
                CompetenceAndSpecialistLevel level = CompetenceAndSpecialistLevel.CourtSpecialist;
                if (CompetenceRequirements != null && CompetenceRequirements.Any())
                {
                    if (CompetenceRequirements.Count() == 1)
                    {
                        level = CompetenceRequirements.Single().CompetenceLevel;
                    }
                    // Otherwise, base estimation on the highest (and most expensive) competence level
                    level = CompetenceRequirements.OrderByDescending(r => (int)r.CompetenceLevel).First().CompetenceLevel;
                }
                return EnumHelper.Parent<CompetenceAndSpecialistLevel, CompetenceLevel>(level);
            }
        }

        public List<OrderStatusConfirmation> OrderStatusConfirmations { get; set; }

        public Request ActiveRequest
        {
            get
            {
                return Requests.SingleOrDefault(r =>
                        r.Status == RequestStatus.Created ||
                        r.Status == RequestStatus.Received ||
                        r.Status == RequestStatus.Accepted ||
                        r.Status == RequestStatus.Approved ||
                        r.Status == RequestStatus.AcceptedNewInterpreterAppointed);
            }
        }

        #endregion

        #region methods

        public Request CreateRequest(IQueryable<Ranking> rankings, DateTimeOffset? newRequestExpiry, DateTimeOffset newRequestCreationTime, bool isTerminalRequest = false)
        {
            var brokersWithRequest = Requests.Select(r => r.Ranking.BrokerId);

            var ranking = ReplacingOrderId.HasValue && brokersWithRequest.Any() ? null :
                rankings.Where(r => !brokersWithRequest.Contains(r.BrokerId)).OrderBy(r => r.Rank).FirstOrDefault();

            if (ranking == null)
            {
                // Rejected by all brokers, close the order
                Status = OrderStatus.NoBrokerAcceptedOrder;
                return null;
            }

            return CreateRequest(ranking, newRequestExpiry, newRequestCreationTime, isTerminalRequest);
        }

        public Request CreateRequest(Ranking ranking, DateTimeOffset? newRequestExpiry, DateTimeOffset newRequestCreationTime, bool isTerminalRequest = false)
        {
            var request = new Request(ranking, newRequestExpiry, newRequestCreationTime, isTerminalRequest);

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

        public void ChangeContactPerson(DateTimeOffset changedAt, int userId, int? impersonatingUserId, AspNetUser contactPerson)
        {
            if (contactPerson != null && contactPerson.CustomerOrganisationId != CustomerOrganisationId)
            {
                throw new InvalidOperationException($"Cannot assign User {contactPerson.Id} as contact person on Order {OrderId}, since this user belongs to CustomerOrganization {contactPerson.CustomerOrganisationId}");
            }
            if (Status == OrderStatus.CancelledByCreator || Status == OrderStatus.CancelledByBroker || Status == OrderStatus.NoBrokerAcceptedOrder || Status == OrderStatus.ResponseNotAnsweredByCreator)
            {
                throw new InvalidOperationException($"Order {OrderId} is {Status}. Can't change contact person for orders with this status.");
            }
            OrderContactPersonHistory.Add(new OrderContactPersonHistory
            {
                ChangedAt = changedAt,
                PreviousContactPersonId = ContactPersonId,
                ChangedBy = userId,
                ImpersonatingChangeUserId = impersonatingUserId,
                OrderId = OrderId
            });
            ContactPersonUser = contactPerson;
        }

        public bool IsAuthorizedAsCreator(IEnumerable<int> customerUnits, int? customerOrganisationId, int userId, bool hasCorrectAdminRole = false)
        {
            return HasCorrectAdminRoleForCustomer(customerOrganisationId, hasCorrectAdminRole) 
                || CreatedByUserWithoutUnit(customerOrganisationId, userId) 
                || CreatedByUsersUnit(customerUnits);
        }

        public bool IsAuthorizedAsCreatorOrContact(IEnumerable<int> customerUnits, int? customerOrganisationId, int userId, bool hasCorrectAdminRole = false)
        {
            return IsAuthorizedAsCreator(customerUnits, customerOrganisationId, userId, hasCorrectAdminRole) 
                || UserIsContact(userId);
        }

        private bool HasCorrectAdminRoleForCustomer(int? customerOrganisationId, bool hasCorrectAdminRole = false)
        {
            return hasCorrectAdminRole && CustomerOrganisationId == customerOrganisationId;
        }

        private bool CreatedByUserWithoutUnit(int? customerOrganisationId, int userId)
        {
            return CustomerOrganisationId == customerOrganisationId && CustomerUnitId == null && CreatedBy == userId;
        }

        private bool UserIsContact(int userId)
        {
            return ContactPersonId == userId;
        }

        private bool CreatedByUsersUnit(IEnumerable<int> customerUnits)
        {
            return CustomerUnitId != null && customerUnits.Contains(CustomerUnitId.Value);
        }

        #endregion
    }
}
