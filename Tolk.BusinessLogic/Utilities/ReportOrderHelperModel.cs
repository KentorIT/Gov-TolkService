using Tolk.BusinessLogic.Enums;

namespace Tolk.BusinessLogic.Utilities
{
    public class ReportOrderHelperModel : ReportBaseHelperModel
    {
        public int OrderId { get; set; }

        public OrderStatus OrderStatus { get; set; }

        public RequestStatus RequestStatus { get; set; }

        public bool LanguageHasAuthorizedInterpreter { get; set; }

        public bool SpecificCompetenceLevelRequired { get; set; }


    }
}
