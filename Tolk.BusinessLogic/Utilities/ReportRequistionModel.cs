using System.Collections.Generic;
using System.Linq;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    public class ReportRequistionModel
    {
        public IEnumerable<ReportRequisitionHelperModel> Requisitions { get; set; }
        public IEnumerable<ReportPriceModel> RequisitionPrices { get; set; }
        public IEnumerable<ReportPriceModel> RequestPrices { get; set; }
        public IEnumerable<int> HasMealbreaks { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "Needed to get better ef code")]
        internal static ReportRequistionModel GetModelFromRequisitions(IQueryable<Requisition> requisitions, IQueryable<MealBreak> mealbreaks, IQueryable<RequisitionPriceRow> requisitionPricerows, IQueryable<RequestPriceRow> requestPricerows, bool isBroker)
        {
            var requisitionPrices = requisitionPricerows.ToList();
            var model = new ReportRequistionModel
            {
                Requisitions = requisitions.Select(r => new ReportRequisitionHelperModel
                {
                    RequisitionId = r.RequisitionId,
                    AllowExceedingTravelCost = r.Request.Order.AllowExceedingTravelCost,
                    OrderNumber = r.Request.Order.OrderNumber,
                    ReportDate = r.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    Language = r.Request.Order.Language.Name,
                    Region = r.Request.Order.Region.Name,
                    AssignmentType = r.Request.Order.AssignmentType,
                    AssignmentDate = $"{r.Request.Order.StartAt.ToString("yyyy-MM-dd HH:mm")}-{r.Request.Order.EndAt.ToString("HH:mm")}",
                    RequisitionStatus = r.Status,
                    ReferenceNumber = r.Request.Order.CustomerReferenceNumber ?? string.Empty,
                    Department = r.Request.Order.UnitName ?? string.Empty,
                    CustomerUnitName = r.Request.Order.CustomerUnitId.HasValue ? r.Request.Order.CustomerUnit.Name : string.Empty,
                    CustomerName = r.Request.Order.CustomerOrganisation.Name,
                    RequestId = r.RequestId,
                    BrokerName = r.Request.Ranking.Broker.Name,
                    InterpreterId = r.Request.Interpreter != null ? r.Request.Interpreter.OfficialInterpreterId ?? string.Empty : string.Empty,
                    InterpreterLocation = r.Request.InterpreterLocation,
                    CompetenceLevel = r.Request.CompetenceLevel,
                    CarCompensation = r.CarCompensation ?? 0,
                    PerDiem = r.PerDiem,
                    WaisteTime = r.TimeWasteNormalTime ?? 0,
                    WaisteTimeIWH = r.TimeWasteIWHTime ?? 0,
                    TaxCard = r.InterpretersTaxCard,
                    ReportPerson = r.Status == RequisitionStatus.AutomaticGeneratedFromCancelledOrder ? "Systemet" : !isBroker ? r.ProcessedUser != null ? r.ProcessedUser.FullName : string.Empty : r.CreatedByUser.FullName,
                }).ToList(),
                RequisitionPrices = requisitionPrices.GroupBy(r => r.RequisitionId).Select(rp => new ReportPriceModel
                {
                    RequisitionId = rp.Key,
                    Price = rp.Sum(p => p.TotalPrice),
                    Outlay = rp.FirstOrDefault(pr => pr.PriceRowType == PriceRowType.Outlay)?.Price ?? 0,
                }).ToList(),
                RequestPrices = requestPricerows?.ToList().GroupBy(r => r.RequestId).Select(rp => new ReportPriceModel
                {
                    RequestId = rp.Key,
                    Price = rp.Sum(p => p.TotalPrice)
                }).ToList(),
                HasMealbreaks = mealbreaks.Select(r => r.RequisitionId).ToList(),
            };
            return model;
        }
    }
}
