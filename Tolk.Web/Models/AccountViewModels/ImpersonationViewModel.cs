using Tolk.Web.Helpers;

namespace Tolk.Web.Models.AccountViewModels
{
    public class ImpersonationViewModel : IModel
    {
        [NoDisplayName]
        public string UserId { get; set; }
    }
}
