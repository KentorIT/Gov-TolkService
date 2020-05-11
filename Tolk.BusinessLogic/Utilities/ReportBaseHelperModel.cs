using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    public class ReportBaseHelperModel
    {

        public string OrderNumber { get; set; }

        public string ReportDate { get; set; }

        public AssignmentType AssignmentType { get; set; }

        public string AssignmentDate { get; set; }

        public string ReferenceNumber { get; set; }

        public string Department { get; set; }

        public AllowExceedingTravelCost? AllowExceedingTravelCost { get; set; }

        public string Region { get; set; }

        public string Language { get; set; }

        public string CustomerName { get; set; }

        public string CustomerUnitName { get; set; }

        public int? InterpreterLocation { get; set; }

        public string InterpreterId { get; set; }

        public string BrokerName { get; set; }

        public int? CompetenceLevel { get; set; }

        public string ReportPerson { get; set; }
        
        public int RequestId { get; set; }

    }
}
