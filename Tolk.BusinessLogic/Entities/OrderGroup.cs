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
    public class OrderGroup : OrderBase
    {
        #region constructors

        private OrderGroup() { }

        public OrderGroup(AspNetUser createdByUser, AspNetUser createdByImpersonator, CustomerOrganisation customerOrganisation, DateTimeOffset createdAt, IEnumerable<Order> orders, bool requireSameInterpreter = true)
        {
            //Verify that all orders have the same customer, region and language
            Validate.Ensure(orders.GroupBy(o => o.CustomerOrganisationId).Count() == 1, "A group cannot have orders connected to several customers.");
            Validate.Ensure(orders.GroupBy(o => o.LanguageId).Count() == 1, "A group cannot have orders connected to several languages.");
            Validate.Ensure(orders.GroupBy(o => o.RegionId).Count() == 1, "A group cannot have orders connected to several regions.");
            Orders = orders.ToList();
            CustomerOrganisation = customerOrganisation;
            CustomerOrganisationId = customerOrganisation.CustomerOrganisationId;
            CreatedAt = createdAt;
            CreatedByUser = createdByUser;
            CreatedByImpersonator = createdByImpersonator;
            RequestGroups = new List<RequestGroup>();
            RequireSameInterpreter = requireSameInterpreter;
            Status = OrderStatus.Requested;
            Requirements = new List<OrderGroupRequirement>();
            InterpreterLocations = new List<OrderGroupInterpreterLocation>();
            CompetenceRequirements = new List<OrderGroupCompetenceRequirement>();
        }

        #endregion

        #region properties

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderGroupId { get; set; }

        [MaxLength(255)]
        [Required]
        public string OrderGroupNumber { get; set; }

        public bool RequireSameInterpreter { get; set; }

        #endregion

        #region navigation properties

        public List<Order> Orders { get; set; }

        public List<RequestGroup> RequestGroups { get; set; }

        public List<OrderGroupAttachment> Attachments { get; set; }

        public List<OrderGroupRequirement> Requirements { get; set; }

        public List<OrderGroupCompetenceRequirement> CompetenceRequirements { get; set; }

        public List<OrderGroupInterpreterLocation> InterpreterLocations { get; set; }

        #endregion

        #region methods and read only properties

        public void AwaitDeadlineFromCustomer()
        {
            SetStatus(OrderStatus.AwaitingDeadlineFromCustomer);
            ActiveRequestGroup.Status = RequestStatus.AwaitingDeadlineFromCustomer;
            ActiveRequestGroup.IsTerminalRequest = true;
        }

        public RequestGroup ActiveRequestGroup => RequestGroups.Single(r => r.IsToBeProcessedByBroker);

        public Order FirstOrder => Orders?.OrderBy(o => o.StartAt).FirstOrDefault();

        public string LanguageName => FirstOrder?.OtherLanguage ?? FirstOrder?.Language?.Name;

        public DateTimeOffset ClosestStartAt => FirstOrder?.StartAt ?? DateTimeOffset.MinValue; 

        public bool IsSingleOccasion => (Orders == null) || (Orders.Count <= 2 && Orders.Any(o => o.IsExtraInterpreterForOrderId != null));

        public void SetStatus(OrderStatus status, bool updateOrders = true)
        {
            Status = status;
            if (updateOrders)
            {
                Orders.ForEach(o => o.Status = status);
            }
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
                SetStatus(OrderStatus.NoBrokerAcceptedOrder, false);
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
