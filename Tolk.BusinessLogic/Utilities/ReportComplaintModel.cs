using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Entities;

namespace Tolk.BusinessLogic.Utilities
{
    public class ReportComplaintModel
    {
        public IEnumerable<ReportComplaintHelperModel> Complaints { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "Needed to get better ef code")]
        internal static ReportComplaintModel GetModelFromComplaints(IQueryable<Complaint> complaints,  bool isBroker)
        {
            var model = new ReportComplaintModel
            {
                Complaints = complaints.Select(r => new ReportComplaintHelperModel
                {
                    ComplaintId = r.ComplaintId,
                    AllowExceedingTravelCost = r.Request.Order.AllowExceedingTravelCost,
                    OrderNumber = r.Request.Order.OrderNumber,
                    ReportDate = r.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    Language = r.Request.Order.Language.Name,
                    Region = r.Request.Order.Region.Name,
                    AssignmentType = r.Request.Order.AssignmentType,
                    AssignmentDate = $"{r.Request.CalculatedStartAt:yyyy-MM-dd HH:mm}-{r.Request.CalculatedEndAt:HH:mm}",
                    ComplaintStatus = r.Status,
                    ComplaintType = r.ComplaintType,
                    ReportPerson = isBroker ? r.AnsweringUser != null ? r.AnsweringUser.FullName : string.Empty : r.CreatedByUser.FullName,
                    ReferenceNumber = r.Request.Order.CustomerReferenceNumber ?? string.Empty,
                    Department = r.Request.Order.UnitName ?? string.Empty,
                    CustomerUnitName = r.Request.Order.CustomerUnitId.HasValue ? r.Request.Order.CustomerUnit.Name : string.Empty,
                    CustomerName = r.Request.Order.CustomerOrganisation.Name,
                    RequestId = r.RequestId,
                    BrokerName = r.Request.Ranking.Broker.Name,
                    InterpreterId = r.Request.Interpreter != null ? r.Request.Interpreter.OfficialInterpreterId ?? string.Empty : string.Empty,
                    InterpreterLocation = r.Request.InterpreterLocation,
                    CompetenceLevel = r.Request.CompetenceLevel,
                    AgreementNumber = r.Request.Ranking.FrameworkAgreement.AgreementNumber
                }).ToList()
            };
            return model;
        }
    }
}
