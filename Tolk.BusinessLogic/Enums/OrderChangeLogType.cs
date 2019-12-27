using System.ComponentModel;

namespace Tolk.BusinessLogic.Enums
{
    public enum OrderChangeLogType
    {
        [Description("Bifogad fil")]
        Attachment = 1,
        [Description("Kontaktperson")]
        ContactPerson = 2,
        [Description("Övrigt")]
        Other = 3,
    }
}
