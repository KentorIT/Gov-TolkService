using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Validation;

namespace Tolk.BusinessLogic.Entities
{
    public class OrderGroup
    {
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

        [ForeignKey(nameof(ImpersonatingCreator))]
        public AspNetUser CreatedByImpersonator { get; set; }

        public bool RequireSameInterpreter { get; set; }

        public List<Order> Orders { get; set; }

        public List<RequestGroup> RequestGroups { get; set; }

        public void AwaitDeadlineFromCustomer()
        {
            foreach (Order order in Orders)
            {
                order.Status = OrderStatus.AwaitingDeadlineFromCustomer;
            }
            ActiveRequestGroup.Status = RequestStatus.AwaitingDeadlineFromCustomer;
            ActiveRequestGroup.IsTerminalRequest = true;
        }

        public RequestGroup ActiveRequestGroup
        {
            get => RequestGroups.Single(r => r.IsToBeProcessedByBroker);
        }
        public int RegionId { get => FirstOrder.RegionId; }
        public Order FirstOrder { get => Orders?.OrderBy(o => o.StartAt).FirstOrDefault(); }

        public Region Region { get => FirstOrder?.Region; }

        public AssignmentType AssignmentType { get => FirstOrder.AssignentType; }

        public CustomerOrganisation CustomerOrganisation { get => FirstOrder?.CustomerOrganisation; }

        public string LanguageName { get => FirstOrder?.OtherLanguage ?? FirstOrder?.Language?.Name; }

        public DateTimeOffset ClosestStartAt { get => FirstOrder?.StartAt ?? DateTimeOffset.MinValue; }

        public bool IsSingleOccasion
        {
            get => (Orders == null) || (Orders.Count() <= 2 && Orders.Any(o => o.IsExtraInterpreterForOrderId != null));
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
    }
}
