using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Entities;

namespace Tolk.BusinessLogic.Utilities
{
    public class ReportOrderModel
    {
        public IEnumerable<ReportOrderHelperModel> OrderRequests { get; set; }
        public IEnumerable<ReportPriceModel> Prices { get; set; }
        public IEnumerable<ReportRequirementAnswerModel> RequirementAnswers { get; set; }
        public IEnumerable<ReportRequirementModel> Requirements { get; set; }
        public IEnumerable<ReportCompetenceModel> Competences { get; set; }
        public IEnumerable<ReportInterpreterLocationModel> InterpreterLocations { get; set; }
        public IEnumerable<int> HasComplaints { get; set; }
        public IEnumerable<int> HasRequisitions { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "Needed to get better ef code")]
        internal static ReportOrderModel GetModelFromOrders(IQueryable<Request> requestOrders, IQueryable<Requisition> requisitions, IQueryable<Complaint> complaints, IQueryable<OrderInterpreterLocation> interpreterLocations, IQueryable<OrderRequirement> orderRequirements, IQueryable<OrderRequirementRequestAnswer> orderRequirementAnswers, IQueryable<OrderCompetenceRequirement> competenceRequirements, IQueryable<RequestPriceRow> requestPricerows, bool isBroker, bool isDelivered)
        {
            var model = new ReportOrderModel
            {
                OrderRequests = requestOrders.Select(r => new ReportOrderHelperModel
                {
                    OrderId = r.OrderId,
                    AllowExceedingTravelCost = r.Order.AllowExceedingTravelCost,
                    OrderNumber = r.Order.OrderNumber,
                    ReportDate = isDelivered ? r.Order.StartAt.ToString("yyyy-MM-dd HH:mm") : isBroker ? r.CreatedAt.ToString("yyyy-MM-dd HH:mm") : r.Order.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    Language = r.Order.Language.Name,
                    Region = r.Order.Region.Name,
                    AssignmentType = r.Order.AssignmentType,
                    ReportPerson = isBroker ? r.AnsweringUser != null ? r.AnsweringUser.FullName : string.Empty : r.Order.CreatedByUser.FullName,
                    AssignmentDate = $"{r.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}-{r.Order.EndAt.ToString("HH:mm")}",
                    OrderStatus = r.Order.Status,
                    RequestStatus = r.Status,
                    ReferenceNumber = r.Order.CustomerReferenceNumber ?? string.Empty,
                    Department = r.Order.UnitName ?? string.Empty,
                    CustomerUnitName = r.Order.CustomerUnitId.HasValue ? r.Order.CustomerUnit.Name : string.Empty,
                    CustomerName = r.Order.CustomerOrganisation.Name,
                    LanguageHasAuthorizedInterpreter = r.Order.LanguageHasAuthorizedInterpreter,
                    SpecificCompetenceLevelRequired = r.Order.SpecificCompetenceLevelRequired,
                    RequestId = r.RequestId,
                    BrokerName = r.Ranking.Broker.Name,
                    InterpreterId = r.Interpreter != null ? r.Interpreter.OfficialInterpreterId ?? string.Empty : string.Empty,
                    InterpreterLocation = r.InterpreterLocation,
                    CompetenceLevel = r.CompetenceLevel
                }).ToList(),
                InterpreterLocations = interpreterLocations.Select(i => new ReportInterpreterLocationModel
                {
                    OrderId = i.OrderId,
                    InterpreterLocation = i.InterpreterLocation,
                    Rank = i.Rank
                }).ToList(),
                Requirements = orderRequirements.Select(or => new ReportRequirementModel
                {
                    OrderId = or.OrderId,
                    RequirementType = or.RequirementType,
                    IsRequired = or.IsRequired,
                    OrderRequirementId = or.OrderRequirementId,
                    Description = or.Description
                }).ToList(),
                RequirementAnswers = orderRequirementAnswers.Select(or => new ReportRequirementAnswerModel
                {
                    RequestId = or.RequestId,
                    CanSatisfyRequirement = or.CanSatisfyRequirement,
                    OrderRequirementId = or.OrderRequirementId
                }).ToList(),
                Competences = competenceRequirements.Select(oc => new ReportCompetenceModel
                {
                    CompetenceLevel = oc.CompetenceLevel,
                    Rank = oc.Rank,
                    OrderId = oc.OrderId
                }).ToList(),
                Prices = requestPricerows?.ToList().GroupBy(r => r.RequestId).Select(rp => new ReportPriceModel
                {
                    RequestId = rp.Key,
                    Price = (rp.Sum(p => p.TotalPrice))
                }).ToList(),
                HasRequisitions = requisitions.Select(r => r.RequestId).ToList(),
                HasComplaints = complaints.Select(r => r.RequestId).ToList()
            };
            return model;
        }
    }
}
