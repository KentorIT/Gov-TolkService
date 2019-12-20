using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class VerificationResultModel
    {
        public VerificationResult Value { get; set; }

        public string DisplayName { get; set; }

        public string Description { get; set; }

        public static string GetCssColorFromResultCode(int value)
        {
            return value >= 400 ? "red"
                : value >= 300 ? "yellow"
                : value >= 200 ? "red"
                : "blue";
        }
    }
}
