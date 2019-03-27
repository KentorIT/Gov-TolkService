using Tolk.BusinessLogic.Enums;
using System.ComponentModel.DataAnnotations;

namespace Tolk.Web.Models
{
    public class ReportRowModel
    {
        [Display(Name = "Ordernummer")]
        public string OrderNumber { get; set; }

        public string ReportDate { get; set; }

        public string AssignmentType { get; set; }

        public string Region { get; set; }

        public string AssignmentDate { get; set; }

        public string Language { get; set; }

        public CompetenceAndSpecialistLevel InterpreterCompetenceLevel { get; set; }

        public string InterpreterId { get; set; }

        public string Status { get; set; }

        public bool HasRequisition { get; set; }

        public bool HasComplaint{ get; set; }


    }
}
