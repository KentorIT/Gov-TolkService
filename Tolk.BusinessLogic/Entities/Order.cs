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
    public class Order : OrderBase
    {
        private Order() { }

        public Order(Order order)
            : this(order?.CreatedByUser ?? throw new ArgumentNullException(nameof(order)), order.CreatedByImpersonator, order.CustomerOrganisation, order.CreatedAt)
        {
            AllowExceedingTravelCost = order.AllowExceedingTravelCost;
            AssignmentType = order.AssignmentType;
            Language = order.Language;
            OtherLanguage = order.OtherLanguage;
            Region = order.Region;
            InvoiceReference = order.InvoiceReference;
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
            CustomerOrganisation = customerOrganisation ?? throw new ArgumentNullException(nameof(customerOrganisation));
            CustomerOrganisationId = customerOrganisation.CustomerOrganisationId;
            CreatedByImpersonator = createdByImpersonator;
            Status = OrderStatus.Requested;
            Requirements = new List<OrderRequirement>();
            InterpreterLocations = new List<OrderInterpreterLocation>();
            PriceRows = new List<OrderPriceRow>();
            Requests = new List<Request>();
            CompetenceRequirements = new List<OrderCompetenceRequirement>();
        }

        #region base information

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

        [Required]
        public string OrderNumber { get; set; }

        public int? OrderGroupId { get; set; }

        [ForeignKey(nameof(OrderGroupId))]
        public OrderGroup Group { get; set; }

        public override OrderStatus Status
        {
            get
            {
                return base.Status;
            }
            set
            {
                if (value == OrderStatus.ResponseAccepted &&
                //NEED TO ADD A CHECK IF REQUESTED, AND THE ALLOW CHECK IS FALSE
                    (!((base.Status == OrderStatus.Requested && !Requests.OrderBy(r => r.RequestId).Last().RequiresAccept) ||
                        base.Status == OrderStatus.RequestResponded ||
                        base.Status == OrderStatus.RequestRespondedNewInterpreter ||
                       (base.Status == OrderStatus.Requested && ReplacingOrderId.HasValue)) ||
                    Requests.Count(r => r.Status == RequestStatus.Approved) != 1))
                {
                    throw new InvalidOperationException($"Betällning {OrderNumber} Kan inte sättas till {OrderStatus.ResponseAccepted.GetDescription()}.");
                }

                base.Status = value;
            }
        }

        public int? ReplacingOrderId { get; set; }

        [ForeignKey(nameof(ReplacingOrderId))]
        [InverseProperty(nameof(ReplacedByOrder))]
        public Order ReplacingOrder { get; set; }

        public List<OrderAttachment> Attachments { get; set; }

        public List<OrderChangeLogEntry> OrderChangeLogEntry { get; set; }
        
        #endregion

        #region customer information

        [MaxLength(100)]
        public string CustomerReferenceNumber { get; set; }

        [MaxLength(100)]
        public string InvoiceReference { get; set; }

        public int? ContactPersonId { get; set; }

        [ForeignKey(nameof(ContactPersonId))]
        public AspNetUser ContactPersonUser { get; set; }

        [MaxLength(100)]
        public string UnitName { get; set; }

        #endregion

        #region order information

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

        [MaxLength(1000)]
        public string Description { get; set; }

        public int? IsExtraInterpreterForOrderId { get; set; }

        [ForeignKey(nameof(IsExtraInterpreterForOrderId))]
        [InverseProperty(nameof(ExtraInterpreterOrder))]
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

        [InverseProperty(nameof(IsExtraInterpreterForOrder))]
        public Order ExtraInterpreterOrder { get; set; }

        public CompetenceLevel PriceCalculatedFromCompetenceLevel
        {
            get
            {
                CompetenceAndSpecialistLevel level = CompetenceAndSpecialistLevel.CourtSpecialist;
                if (CompetenceRequirements != null && CompetenceRequirements.Any())
                {
                    if (CompetenceRequirements.Count == 1)
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
            Ranking ranking = GetNextRanking(rankings, newRequestCreationTime);
            if (ranking == null)
            {
                // Rejected by all brokers, close the order
                Status = OrderStatus.NoBrokerAcceptedOrder;
                return null;
            }

            return CreateRequest(ranking, newRequestExpiry, newRequestCreationTime, isTerminalRequest);
        }

        internal Request CreateRequest(Ranking ranking, DateTimeOffset? newRequestExpiry, DateTimeOffset newRequestCreationTime, bool isTerminalRequest = false)
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

        public void ChangeContactPerson(DateTimeOffset changedAt, int userId, int? impersonatingUserId, AspNetUser newContactPerson)
        {
            if (newContactPerson != null && newContactPerson.CustomerOrganisationId != CustomerOrganisationId)
            {
                throw new InvalidOperationException($"Cannot assign User {newContactPerson.Id} as contact person on Order {OrderId}, since this user belongs to CustomerOrganization {newContactPerson.CustomerOrganisationId}");
            }
            if (Status == OrderStatus.CancelledByCreator || Status == OrderStatus.CancelledByBroker || Status == OrderStatus.NoBrokerAcceptedOrder || Status == OrderStatus.ResponseNotAnsweredByCreator)
            {
                throw new InvalidOperationException($"Order {OrderId} is {Status}. Can't change contact person for orders with this status.");
            }
            OrderChangeLogEntry.Add(new OrderChangeLogEntry
            {
                LoggedAt = changedAt,
                UpdatedByUserId = userId,
                UpdatedByImpersonatorId = impersonatingUserId,
                OrderChangeLogType = OrderChangeLogType.ContactPerson,
                OrderContactPersonHistory = new OrderContactPersonHistory { PreviousContactPersonId = ContactPersonId }
            });
            ContactPersonUser = newContactPerson;
        }

        public void ChangeAttachments(DateTimeOffset changedAt, int userId, int? impersonatingUserId, IEnumerable<int> updatedAttachments)
        {
            if (Status != OrderStatus.ResponseAccepted)
            {
                throw new InvalidOperationException($"Bokningen {OrderId} har fel status {Status} för att kunna uppdateras.");
            }
            //before changing the attachments
            var orderAttachmentHistoryEntries = Attachments.Select(a => new OrderAttachmentHistoryEntry { AttachmentId = a.AttachmentId }).ToList();

            List<int> ordergroupAttachmentIdsToRemove = Enumerable.Empty<int>().ToList();

            IEnumerable<int> orderAttachmentIds = Attachments.Select(a => a.AttachmentId);
            IEnumerable<int> orderGroupAttachmentIds = OrderGroupId.HasValue ? Group.Attachments.Where(oa => !oa.Attachment.OrderAttachmentHistoryEntries.Any(h => h.OrderGroupAttachmentRemoved && h.OrderChangeLogEntry.OrderId == OrderId)).Select(ag => ag.AttachmentId) : Enumerable.Empty<int>();
            IEnumerable<int> oldOrderAttachmentIdsToCompare = orderAttachmentIds.Union(orderGroupAttachmentIds);

            //all attachments removed
            if (oldOrderAttachmentIdsToCompare.Any() && !updatedAttachments.Any())
            {
                Attachments.Clear();
                if (orderGroupAttachmentIds.Any())
                {
                    ordergroupAttachmentIdsToRemove.AddRange(orderGroupAttachmentIds);
                }
            }
            else
            {
                //add those that should be removed from group
                ordergroupAttachmentIdsToRemove.AddRange(orderGroupAttachmentIds.Except(updatedAttachments));
                //new added attachments
                Attachments.AddRange(updatedAttachments.Except(oldOrderAttachmentIdsToCompare).Select(a => new OrderAttachment { AttachmentId = a }).ToList());
                //removed from order
                foreach (int i in orderAttachmentIds.Except(updatedAttachments).ToList())
                {
                    var attachment = Attachments.Single(a => a.AttachmentId == i);
                    Attachments.Remove(attachment);
                }
            }

            orderAttachmentHistoryEntries.AddRange(ordergroupAttachmentIdsToRemove.Select(a => new OrderAttachmentHistoryEntry { AttachmentId = a, OrderGroupAttachmentRemoved = true }));
            OrderChangeLogEntry.Add(new OrderChangeLogEntry
            {
                LoggedAt = changedAt,
                UpdatedByUserId = userId,
                UpdatedByImpersonatorId = impersonatingUserId,
                OrderChangeLogType = OrderChangeLogType.Attachment,
                OrderAttachmentHistoryEntries = orderAttachmentHistoryEntries, 
                BrokerId = Requests.OrderBy(r => r.RequestId).Last().Ranking.BrokerId
            });
        }

        private Ranking GetNextRanking(IQueryable<Ranking> rankings, DateTimeOffset newRequestCreationTime)
        {
            var brokersWithRequest = Requests.Select(r => r.Ranking.BrokerId);

            var ranking = ReplacingOrderId.HasValue && brokersWithRequest.Any() ? null :
                rankings.Where(r => !brokersWithRequest.Contains(r.BrokerId)).OrderBy(r => r.Rank).FirstOrDefault();
            if (ranking != null)
            {
                var quarantine = ranking.Quarantines.FirstOrDefault(q => q.CustomerOrganisationId == CustomerOrganisationId && q.ActiveFrom <= newRequestCreationTime && q.ActiveTo >= newRequestCreationTime);
                if (quarantine != null)
                {
                    //Create a quarantined request, and get next...
                    CreateQuarantinedRequest(ranking, newRequestCreationTime, quarantine);
                    ranking = GetNextRanking(rankings, newRequestCreationTime);
                }
            }
            return ranking;
        }

        public void Update(DateTimeOffset changedAt, int userId, int? impersonatingUserId, string description, string locationStreet, string offSiteContactInformation, string invoiceReference, string customerReferenceNumber, string unitName, InterpreterLocation selectedInterpreterlocation)
        {
            if (Status != OrderStatus.ResponseAccepted)
            {
                throw new InvalidOperationException($"Bokningen {OrderId} har fel status {Status} för att kunna uppdateras.");
            }
            var orderHistoryEntries = new List<OrderHistoryEntry>
            {
                new OrderHistoryEntry { ChangeOrderType = ChangeOrderType.Description, Value = Description },
                new OrderHistoryEntry { ChangeOrderType = string.IsNullOrEmpty(locationStreet) ? ChangeOrderType.OffSiteContactInformation : ChangeOrderType.LocationStreet, Value = string.IsNullOrEmpty(locationStreet) ? InterpreterLocations.Where(i => i.InterpreterLocation == selectedInterpreterlocation).Single().OffSiteContactInformation : InterpreterLocations.Where(i => i.InterpreterLocation == selectedInterpreterlocation).Single().Street },
                new OrderHistoryEntry { ChangeOrderType = ChangeOrderType.InvoiceReference, Value = InvoiceReference },
                new OrderHistoryEntry { ChangeOrderType = ChangeOrderType.CustomerReferenceNumber, Value = CustomerReferenceNumber },
                new OrderHistoryEntry { ChangeOrderType = ChangeOrderType.CustomerDepartment, Value = UnitName }
            };
            OrderChangeLogEntry.Add(new OrderChangeLogEntry
            {
                LoggedAt = changedAt,
                UpdatedByUserId = userId,
                UpdatedByImpersonatorId = impersonatingUserId,
                OrderChangeLogType = OrderChangeLogType.Other,
                OrderHistories = orderHistoryEntries,
                BrokerId = Requests.OrderBy(r => r.RequestId).Last().Ranking.BrokerId
            });
            Description = description;
            InvoiceReference = invoiceReference;
            UnitName = unitName;
            CustomerReferenceNumber = customerReferenceNumber;
            InterpreterLocations.Where(i => i.InterpreterLocation == selectedInterpreterlocation).Single().OffSiteContactInformation = offSiteContactInformation;
            InterpreterLocations.Where(i => i.InterpreterLocation == selectedInterpreterlocation).Single().Street = locationStreet;
        }

        public void ConfirmNoAnswer(DateTimeOffset confirmedAt, int userId, int? impersonatorId)
        {
            if (Status != OrderStatus.NoBrokerAcceptedOrder)
            {
                throw new InvalidOperationException($"Bokning med boknings-id {OrderNumber} var inte avböjd/obesvarad av samtliga förmedlingar.");
            }
            if (OrderStatusConfirmations.Any(o => o.OrderStatus == Status))
            {
                throw new InvalidOperationException($"Bokning med boknings-id {OrderNumber} har redan bekräftats som obesvarad.");
            }
            OrderStatusConfirmations.Add(new OrderStatusConfirmation { ConfirmedBy = userId, ImpersonatingConfirmedBy = impersonatorId, OrderStatus = Status, ConfirmedAt = confirmedAt });
        }

        internal Request CreateQuarantinedRequest(Ranking ranking, DateTimeOffset creationTime, Quarantine quarantine)
        {
            var request = new Request(ranking, creationTime, quarantine);
            Requests.Add(request);
            return request;
        }

        internal override bool UserIsContact(int userId) => ContactPersonId == userId;

        #endregion
    }
}
