﻿using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    public class ReportRow
    {
        public string OrderNumber { get; set; }

        public string ReportDate { get; set; }

        public string AssignmentType { get; set; }

        public string Region { get; set; }

        public string AssignmentDate { get; set; }

        public string Language { get; set; }

        public CompetenceAndSpecialistLevel InterpreterCompetenceLevel { get; set; }
        public string InterpreterCompetenceLevelAsString { get; set; }

        public string InterpreterLocation { get; set; }

        public string InterpreterId { get; set; }

        public string Status { get; set; }

        public string BrokerName { get; set; }

        public bool HasRequisition { get; set; }

        public bool HasComplaint { get; set; }

        public string ReportPersonToDisplay { get; set; }

        public string CustomerName { get; set; }

        public decimal Price { get; set; }

        public string ReferenceNumber { get; set; }

        public string Department { get; set; }

        public string CustomerUnitName { get; set; }

        public string AllowExceedingTravelCost { get; set; }

        public string OrderCreatorEmail { get; set; }

        public string InvoiceReference { get; set; }
        public string AgreementNumber { get; set; }

        public string FlexiblOrderAsString { get; set; }

    }
}
