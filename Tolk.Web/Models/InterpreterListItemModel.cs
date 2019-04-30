using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class InterpreterListItemModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public bool IsActive { get; set; }

        public string OfficialInterpreterId { get; set; }

        public string ColorClassName { get => CssClassHelper.GetColorClassNameForActiveStatus(IsActive); }
    }
}
