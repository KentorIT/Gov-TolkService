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
    public class OrderGroup
    {
        #region constructors

        private OrderGroup() { }

        public OrderGroup(AspNetUser createdByUser, AspNetUser createdByImpersonator, DateTimeOffset createdAt, IEnumerable<Order> orders, bool requireSameInterpreter = true)
        {
            //Verify that all orders have the same customer, region and language
            Validate.Ensure(orders.GroupBy(o => o.CustomerOrganisationId).Count() == 1, "A group cannot have orders connected to several customers.");
            Validate.Ensure(orders.GroupBy(o => o.LanguageId).Count() == 1, "A group cannot have orders connected to several languages.");
            Validate.Ensure(orders.GroupBy(o => o.RegionId).Count() == 1, "A group cannot have orders connected to several regions.");
            Orders = orders.ToList();
            CreatedAt = createdAt;
            CreatedByUser = createdByUser;
            CreatedByImpersonator = createdByImpersonator;
            RequestGroups = new List<RequestGroup>();
            RequireSameInterpreter = requireSameInterpreter;
        }

        #endregion

        #region properties

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderGroupId { get; set; }

        [MaxLength(255)]
        [Required]
        public string OrderGroupNumber { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public int CreatedBy { get; set; }

        [ForeignKey(nameof(CreatedBy))]
        public AspNetUser CreatedByUser { get; set; }

        public int? ImpersonatingCreator { get; set; }

        public bool RequireSameInterpreter { get; set; }

        #endregion

        #region navigation properties

        [ForeignKey(nameof(ImpersonatingCreator))]
        public AspNetUser CreatedByImpersonator { get; set; }

        public List<Order> Orders { get; set; }

        public List<RequestGroup> RequestGroups { get; set; }

        public List<OrderGroupAttachment> Attachments { get; set; }

        #endregion

        #region methods and read only properties

        public void AwaitDeadlineFromCustomer()
        {
            foreach (Order order in Orders)
            {
                order.Status = OrderStatus.AwaitingDeadlineFromCustomer;
            }
            ActiveRequestGroup.Status = RequestStatus.AwaitingDeadlineFromCustomer;
            ActiveRequestGroup.IsTerminalRequest = true;
        }

        public RequestGroup ActiveRequestGroup => RequestGroups.Single(r => r.IsToBeProcessedByBroker);

        public int RegionId  => FirstOrder.RegionId;

        public Order FirstOrder => Orders?.OrderBy(o => o.StartAt).FirstOrDefault();

        public Region Region => FirstOrder?.Region;

        public AssignmentType AssignmentType  => FirstOrder.AssignentType; 

        public CustomerOrganisation CustomerOrganisation => FirstOrder.CustomerOrganisation; 

        public string LanguageName => FirstOrder?.OtherLanguage ?? FirstOrder?.Language?.Name; 

        public DateTimeOffset ClosestStartAt => FirstOrder?.StartAt ?? DateTimeOffset.MinValue; 

        public bool IsSingleOccasion => (Orders == null) || (Orders.Count <= 2 && Orders.Any(o => o.IsExtraInterpreterForOrderId != null));

        public AllowExceedingTravelCost? AllowExceedingTravelCost => FirstOrder.AllowExceedingTravelCost;

        public bool IsAuthorizedAsCreator(IEnumerable<int> customerUnits, int? customerOrganisationId, int userId, bool hasCorrectAdminRole = false)
        {
            return FirstOrder.IsAuthorizedAsCreator(customerUnits, customerOrganisationId, userId, hasCorrectAdminRole);
        }

        public bool IsAuthorizedAsCreatorOrContact(IEnumerable<int> customerUnits, int? customerOrganisationId, int userId, bool hasCorrectAdminRole = false)
        {
            return FirstOrder.IsAuthorizedAsCreatorOrContact(customerUnits, customerOrganisationId, userId, hasCorrectAdminRole);
        }

        public void SetStatus(OrderStatus status)
        {
            Orders.ForEach(o => o.Status = status);
        }

        public RequestGroup CreateRequestGroup(IEnumerable<Ranking> rankings, DateTimeOffset? newRequestExpiry, DateTimeOffset newRequestCreationTime, bool isTerminalRequest = false)
        {
            var brokersWithRequestGroups = RequestGroups.Select(r => r.Ranking.BrokerId);

            var ranking = rankings.Where(r => !brokersWithRequestGroups.Contains(r.BrokerId)).OrderBy(r => r.Rank).FirstOrDefault();
            if (ranking == null)
            {
                // Rejected by all brokers, close all orders
                SetStatus(OrderStatus.NoBrokerAcceptedOrder);
                return null;
            }

            var requestGroup = new RequestGroup(
                ranking,
                newRequestExpiry,
                newRequestCreationTime,
                Orders.Select(o => o.CreateRequest(ranking, newRequestExpiry, newRequestCreationTime, isTerminalRequest)).ToList(),
                isTerminalRequest
            );

            RequestGroups.Add(requestGroup);

            return requestGroup;
        }

        public RequestGroup CreatePartialRequestGroup(IEnumerable<Request> declinedRequests, IEnumerable<Ranking> rankings, DateTimeOffset? newRequestExpiry, DateTimeOffset newRequestCreationTime, bool isTerminalRequest = false)
        {
            var brokersWithRequestGroups = RequestGroups.Select(r => r.Ranking.BrokerId);

            var ranking = rankings.Where(r => !brokersWithRequestGroups.Contains(r.BrokerId)).OrderBy(r => r.Rank).FirstOrDefault();
            var orders = declinedRequests.GetRequestOrders();

            if (ranking == null)
            {
                // Rejected by all brokers, close all orders
                orders.ToList().ForEach(o => o.Status = OrderStatus.NoBrokerAcceptedOrder);
                return null;
            }
            var requestGroup = new RequestGroup(
                ranking,
                newRequestExpiry,
                newRequestCreationTime,
                orders.Select(o => o.CreateRequest(ranking, newRequestExpiry, newRequestCreationTime, isTerminalRequest)).ToList(),
                isTerminalRequest
            );

            RequestGroups.Add(requestGroup);

            return requestGroup;
        }

        #endregion
    }
}
